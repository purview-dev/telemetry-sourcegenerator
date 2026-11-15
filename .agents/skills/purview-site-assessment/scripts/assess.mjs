#!/usr/bin/env node
// Assess a repository's readiness for purview.dev.
//
//   node assess.mjs [repository-root] [--docs <path>] [--catalogue <entry.yml>] [--json]
//
// Reads files only (plus `git log`, when git is available). No network, no
// dependencies, Node 18+. Exit code 1 when an error-level finding exists.
//
// `--catalogue` is a local copy of this project's purview.dev catalogue entry
// (purview-dev/purview-dev: src/src/data/projects/<id>.yml). With it, the
// docs path, root page, deep links and catalogue redirects are checked too.
//
// The rules mirror what purview.dev's docs sync does (purview-dev/purview-dev,
// src/src/lib/docs/). Each finding names a rule id the SKILL.md explains.

import { execFileSync } from 'node:child_process';
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { basename, dirname, join, posix, relative, resolve, sep } from 'node:path';

// ---------------------------------------------------------------------------
// Arguments
// ---------------------------------------------------------------------------

const args = process.argv.slice(2);
const json = args.includes('--json');
const option = (name) => {
  const index = args.indexOf(name);
  return index >= 0 ? args[index + 1] : undefined;
};
const VALUE_OPTIONS = new Set(['--docs', '--catalogue']);
const root = resolve(
  args.find((arg, index) => !arg.startsWith('--') && !VALUE_OPTIONS.has(args[index - 1])) ?? '.',
);
const cataloguePath = option('--catalogue');

/**
 * The few catalogue fields the checks need, read line by line from the entry's
 * fixed layout (no YAML dependency): docs source/path/rootPage, docs.redirects
 * and every use case's docsPage.
 */
function readCatalogueEntry(path) {
  const lines = readFileSync(path, 'utf8').replace(/\r\n?/g, '\n').split('\n');
  const entry = { source: undefined, path: undefined, rootPage: undefined, redirects: {}, docsPages: [] };
  let section = null;
  let sectionIndent = 0;
  for (const line of lines) {
    const indent = line.length - line.trimStart().length;
    const text = line.trim().replace(/\s+#.*$/, '');
    if (text === '' || text.startsWith('#')) continue;
    if (indent === 0) {
      section = text === 'docs:' ? 'docs' : null;
      continue;
    }
    if (section === 'docs' && text === 'redirects:') {
      section = 'redirects';
      sectionIndent = indent;
      continue;
    }
    if (section === 'redirects') {
      if (indent <= sectionIndent) {
        section = 'docs';
      } else {
        const pair = /^["']?([^"':]+)["']?\s*:\s*["']?([^"']+?)["']?$/.exec(text);
        if (pair) entry.redirects[pair[1].trim()] = pair[2].trim();
        continue;
      }
    }
    if (section === 'docs') {
      const field = /^(source|path|rootPage):\s*["']?([^"']+?)["']?$/.exec(text);
      if (field && indent === 2) entry[field[1]] = field[2];
    }
    const deepLink = /^docsPage:\s*["']?([^"'\s]+)["']?$/.exec(text);
    if (deepLink) entry.docsPages.push(deepLink[1]);
  }
  return entry;
}

const catalogue = cataloguePath ? readCatalogueEntry(resolve(cataloguePath)) : null;
const docsOverride = option('--docs') ?? (catalogue?.source === 'github-path' ? catalogue.path : undefined);

/** @type {{ id: string; severity: 'error' | 'warning' | 'info'; file?: string; line?: number; message: string; fix: string }[]} */
const findings = [];
const report = (id, severity, file, line, message, fix) =>
  findings.push({ id, severity, file, line, message, fix });

// ---------------------------------------------------------------------------
// File helpers
// ---------------------------------------------------------------------------

const toPosix = (path) => path.split(sep).join('/');
const repoPath = (absolute) => toPosix(relative(root, absolute));
const read = (absolute) => readFileSync(absolute, 'utf8').replace(/\r\n?/g, '\n');
const IGNORED_DIRS = new Set(['.git', 'node_modules', 'bin', 'obj', 'dist', '.vs', '.idea', 'artifacts']);

function walk(directory, predicate, found = []) {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const absolute = join(directory, entry.name);
    if (entry.isDirectory()) {
      if (!IGNORED_DIRS.has(entry.name)) {
        walk(absolute, predicate, found);
      }
    } else if (predicate(absolute)) {
      found.push(absolute);
    }
  }
  return found;
}

// ---------------------------------------------------------------------------
// Slugs — the same reduction purview.dev applies (src/src/lib/docs/links.ts)
// ---------------------------------------------------------------------------

const reduceSegment = (segment) =>
  segment
    .replace(/[^a-zA-Z0-9-]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .toLowerCase();

function docSlug(relativePath) {
  const segments = relativePath.split('/').filter((segment) => segment !== '' && segment !== '.');
  return segments
    .map((segment, index) => {
      if (index < segments.length - 1) {
        return reduceSegment(segment);
      }
      const name = segment.replace(/\.(md|mdx)$/i, '');
      return segments.length > 1 && /^readme$/i.test(name) ? 'index' : reduceSegment(name);
    })
    .join('/');
}

const SAFE_SEGMENT = /^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/;
const isIndexName = (name) => /^(home|index|readme)\.md$/i.test(name);

// ---------------------------------------------------------------------------
// Markdown helpers
// ---------------------------------------------------------------------------

/** Lines with fenced code blanked (fences reported separately), so headings and links are read from prose only. */
function scanFences(text) {
  const lines = text.split('\n');
  const fences = [];
  const prose = [...lines];
  let open = null;
  lines.forEach((line, index) => {
    const match = /^ {0,3}(`{3,}|~{3,})(.*)$/.exec(line);
    if (!open && match) {
      open = { start: index, run: match[1], info: match[2].trim(), body: [] };
      prose[index] = '';
      return;
    }
    if (open) {
      if (match && match[1][0] === open.run[0] && match[1].length >= open.run.length && match[2].trim() === '') {
        fences.push({ ...open, end: index });
        open = null;
      } else {
        open.body.push(line);
      }
      prose[index] = '';
    }
  });
  if (open) {
    fences.push({ ...open, end: lines.length - 1, unclosed: true });
  }
  return { lines, prose, fences };
}

const stripInlineCode = (line) => line.replace(/`+[^`]*`+/g, '');

const MERMAID_START = /^\s*(flowchart|graph|sequenceDiagram|classDiagram|stateDiagram(-v2)?|erDiagram|gantt|journey|mindmap|timeline|C4Context|C4Container|C4Component|gitGraph|pie|quadrantChart|block-beta|architecture-beta)\b/;

/**
 * What an unlabelled or `text` fence holds: `mermaid` source (the old
 * "Mermaid goes in a text block" convention), a folder `tree` (a listing, not a
 * diagram), an ASCII-art `diagram`, or null for ordinary text.
 */
function classifyTextFence(fence) {
  const language = fence.info.split(/\s+/)[0].toLowerCase();
  if (!['', 'text', 'txt', 'plaintext', 'ascii', 'none'].includes(language)) {
    return null;
  }
  const body = fence.body.filter((line) => line.trim() !== '');
  if (body.length === 0) {
    return null;
  }
  if (MERMAID_START.test(body[0])) {
    return 'mermaid';
  }
  const treeLines = body.filter((line) => /[├└]──|^\s*│|^\s*[\w.<>-]+\/\s*$/.test(line)).length;
  if (treeLines >= Math.ceil(body.length / 2)) {
    return 'tree';
  }
  if (body.length < 3) {
    return null;
  }
  const boxDrawing = body.some((line) => /[┌┐┘┬┴┼═║╔╗╚╝▶▼◀▲]/.test(line));
  const arrows = body.filter((line) => /(-{1,}>|<-{1,}|={1,}>|\+-{2,}|-{3,}\+|→|↓)/.test(line)).length;
  const connectors = body.filter(
    (line) => /^\s*[|^v+]\s*($|\s{2,})|\s{2,}[|^v+]\s*$/.test(line) || /\s[|^]\s{3,}[|^]/.test(line),
  ).length;
  return boxDrawing || (arrows >= 1 && connectors >= 1) || arrows >= 3 ? 'diagram' : null;
}

// ---------------------------------------------------------------------------
// Diagram pairs — mirrors src/src/lib/docs/diagrams.ts
// ---------------------------------------------------------------------------

const DIAGRAM_OPEN = /^ {0,3}<!--\s*purview:likec4\b(.*?)-->\s*$/;
const DIAGRAM_CLOSE = /^ {0,3}<!--\s*\/purview:likec4\s*-->\s*$/;

function parseOptions(source) {
  const options = {};
  for (const match of source.matchAll(/([A-Za-z][\w-]*)\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s"']+))/g)) {
    options[match[1]] = match[2] ?? match[3] ?? match[4] ?? '';
  }
  return options;
}

function declaredViews(source) {
  return new Set(
    [...source.matchAll(/(?:^|[\s{;])(?:dynamic\s+|deployment\s+)?view\s+([A-Za-z_][\w-]*)/g)].map((m) => m[1]),
  );
}

function checkDiagramBlocks(file, scan) {
  const page = repoPath(file);
  const inFence = new Set();
  for (const fence of scan.fences) {
    for (let index = fence.start; index <= fence.end; index++) inFence.add(index);
  }
  const blocks = [];
  let open = null;
  scan.lines.forEach((line, index) => {
    if (inFence.has(index)) return;
    const opening = DIAGRAM_OPEN.exec(line);
    if (opening) {
      if (open) {
        report('DIAG-004', 'error', page, open.line, 'A purview:likec4 block is not closed before the next one opens.', 'Add `<!-- /purview:likec4 -->` after the Mermaid fence.');
      }
      open = { line: index + 1, start: index, options: parseOptions(opening[1]) };
    } else if (DIAGRAM_CLOSE.test(line)) {
      if (!open) {
        report('DIAG-004', 'error', page, index + 1, 'A closing purview:likec4 marker has no opening marker.', 'Remove it, or add the opening marker with file="…" and view="…".');
      } else {
        blocks.push({ ...open, end: index });
        open = null;
      }
    }
  });
  if (open) {
    report('DIAG-004', 'error', page, open.line, 'A purview:likec4 block is never closed.', 'Add `<!-- /purview:likec4 -->` after the Mermaid fence.');
  }

  for (const block of blocks) {
    const inner = scan.fences.filter((fence) => fence.start > block.start && fence.end < block.end);
    if (!inner.some((fence) => fence.info.split(/\s+/)[0] === 'mermaid')) {
      report('DIAG-006', 'warning', page, block.line, 'The diagram block has no ```mermaid fence, so GitHub readers see no diagram.', 'Put the Mermaid rendering of the view between the markers.');
    }
    const file = block.options.file;
    if (!file) {
      report('DIAG-004', 'error', page, block.line, 'The purview:likec4 marker has no file="…"; the site will show the Mermaid instead.', 'Add file="./diagrams/<name>.c4" (relative to the page).');
      continue;
    }
    const target = file.startsWith('/') ? posix.normalize(file.slice(1)) : posix.normalize(posix.join(posix.dirname(page), file));
    if (target.startsWith('..')) {
      report('DIAG-004', 'error', page, block.line, `file="${file}" leaves the repository.`, 'Keep the model inside the repository.');
      continue;
    }
    const absolute = join(root, target);
    if (!existsSync(absolute)) {
      report('DIAG-004', 'error', page, block.line, `file="${file}" does not exist (${target}); the site will show the Mermaid instead.`, 'Create the model or fix the path (it is relative to the page).');
      continue;
    }
    const model = read(absolute);
    const views = declaredViews(model);
    if (block.options.view && !views.has(block.options.view)) {
      report('DIAG-004', 'error', page, block.line, `${target} declares no view "${block.options.view}" (declared: ${[...views].join(', ') || 'none'}).`, 'Name an existing view, or add it to the model.');
    } else if (!block.options.view) {
      report('DIAG-007', 'info', page, block.line, 'The diagram block does not name a view, so the first view in the model is shown.', 'Add view="<id>" so a new view added first does not change this page.');
    }
    if (/^\s*import\s/m.test(model) || !/\bspecification\s*\{/.test(model)) {
      report('DIAG-005', 'warning', target, 1, 'The model is not self-contained (it imports, or has no specification block). The site compiles each file on its own.', 'Give the file its own `specification`, `model` and `views`, and remove imports.');
    }
  }
  return blocks;
}

// ---------------------------------------------------------------------------
// `_redirects.yml` — a tolerant reader for the simple shape the site documents
// ---------------------------------------------------------------------------

function parseRedirects(text) {
  const pairs = [];
  const errors = [];
  text.split('\n').forEach((raw, index) => {
    const line = raw.replace(/\s+#.*$/, '').replace(/^#.*$/, '');
    if (line.trim() === '' || /^redirects:\s*$/.test(line.trim())) return;
    const match = /^\s*(?:"([^"]+)"|'([^']+)'|([^:#\s][^:#]*?))\s*:\s*(?:"([^"]*)"|'([^']*)'|(.*?))\s*$/.exec(line);
    if (!match) {
      errors.push({ line: index + 1, message: `cannot read "${raw.trim()}" as \`previous: current\`` });
      return;
    }
    const from = match[1] ?? match[2] ?? match[3];
    const to = match[4] ?? match[5] ?? match[6];
    if (!to) {
      errors.push({ line: index + 1, message: `"${from}" has no destination` });
      return;
    }
    pairs.push({ line: index + 1, from, to });
  });
  return { pairs, errors };
}

const redirectSlug = (value) => {
  const trimmed = value.trim().replace(/^\/+|\/+$/g, '');
  return docSlug(/\.(md|mdx)$/i.test(trimmed) ? trimmed : `${trimmed}.md`);
};

// ---------------------------------------------------------------------------
// Repository-level checks
// ---------------------------------------------------------------------------

if (!existsSync(root) || !statSync(root).isDirectory()) {
  console.error(`Not a directory: ${root}`);
  process.exit(2);
}

const readmePath = ['README.md', 'readme.md', 'Readme.md'].map((name) => join(root, name)).find(existsSync);
if (!readmePath) {
  report('REPO-001', 'error', undefined, undefined, 'No README.md at the repository root.', 'Add one: what the project is, install, a minimal example, a link into the docs.');
}
for (const site of ['mkdocs.yml', 'mkdocs.yaml', 'docs/mkdocs.yml', 'docs/wiki/mkdocs.yml']) {
  if (existsSync(join(root, site))) {
    report('DOCS-012', 'info', site, undefined, `${site} is a site configuration purview.dev ignores.`, 'Delete it; the site reads the Markdown directly. Rewrite any Material-only markup it enabled as plain Markdown.');
  }
}

// ---------------------------------------------------------------------------
// Documentation
// ---------------------------------------------------------------------------

const docsCandidates = docsOverride ? [docsOverride] : ['docs/wiki', 'docs'];
const docsRootRelative = docsCandidates.find((candidate) => {
  const absolute = join(root, candidate);
  return existsSync(absolute) && statSync(absolute).isDirectory() && walk(absolute, (path) => path.endsWith('.md')).length > 0;
});

if (docsOverride && !docsRootRelative) {
  report('CAT-001', 'error', docsOverride, undefined, `The docs folder "${docsOverride}" has no Markdown${catalogue ? " (the catalogue's docs.path)" : ''}; the site publishes nothing.`, 'Create the folder, or correct docs.path.');
}

/** Every Markdown file the checks read: the docs tree and the README. */
const markdownFiles = [];
let pageSlugs = new Set();

if (!docsRootRelative) {
  const readme = readmePath ? read(readmePath) : '';
  const sections = (readme.match(/^## /gm) ?? []).length;
  const lines = readme.split('\n').length;
  if (sections >= 8 || lines >= 300) {
    report('DOCS-001', 'warning', 'README.md', undefined, `No docs folder, and the README has ${sections} sections over ${lines} lines — it is a documentation tree in one file.`, 'Move the reference material into docs/wiki/ topic folders (see purview-site-docs) and keep the README as the front door.');
  } else {
    report('DOCS-001', 'info', undefined, undefined, 'No docs folder; purview.dev would publish README.md as the only page (docs.source: readme).', 'Fine for a small project. Create docs/wiki/ once the README outgrows one page.');
  }
  if (readmePath) markdownFiles.push(readmePath);
} else {
  const docsRoot = join(root, docsRootRelative);
  const docFiles = walk(docsRoot, (path) => path.endsWith('.md'));
  const relativeToDocs = (absolute) => toPosix(relative(docsRoot, absolute));
  const pages = docFiles.filter((path) => !/^_(Sidebar|Footer)\.md$/i.test(basename(path)));
  markdownFiles.push(...pages);
  if (readmePath && !pages.includes(readmePath)) markdownFiles.push(readmePath);

  // Root page: the catalogue's rootPage when it names one, else the conventional index.
  const rootFiles = pages.filter((path) => dirname(path) === docsRoot);
  const configuredRoot = catalogue?.rootPage
    ? pages.find((path) => relativeToDocs(path).toLowerCase() === catalogue.rootPage.toLowerCase())
    : undefined;
  if (catalogue?.rootPage && !configuredRoot) {
    report('CAT-002', 'error', `${docsRootRelative}/${catalogue.rootPage}`, undefined, `The catalogue's docs.rootPage "${catalogue.rootPage}" does not exist; the site's sync fails.`, 'Restore the page, or ask the site maintainers to change docs.rootPage in the same release.');
  }
  const index = configuredRoot ?? rootFiles.find((path) => isIndexName(basename(path)));
  if (!index) {
    report('DOCS-002', 'warning', docsRootRelative, undefined, 'No Home.md, index.md or README.md at the docs root.', 'Add Home.md as the hub, or have the catalogue entry name docs.rootPage.');
  }

  // Shape: flat corpus, depth.
  const folders = new Set(pages.map((path) => dirname(relativeToDocs(path))).filter((folder) => folder !== '.'));
  if (folders.size === 0 && pages.length >= 7) {
    report('DOCS-003', 'warning', docsRootRelative, undefined, `${pages.length} pages in one flat folder.`, 'Group them into lower-case topic folders (guide/, configuration/, reference/); keep only the hub, getting-started and catalogue deep-linked pages at the root; add each move to _redirects.yml.');
  } else if (rootFiles.length >= 10 && folders.size > 0) {
    report('DOCS-003', 'info', docsRootRelative, undefined, `${rootFiles.length} pages are still at the docs root beside ${folders.size} folder(s).`, 'Move pages into their topic folder unless the catalogue deep-links them (useCases[].docsPage).');
  }
  for (const folder of folders) {
    if (folder.split('/').length > 2) {
      report('DOCS-004', 'info', `${docsRootRelative}/${folder}`, undefined, 'Folders nested more than two levels deep.', 'One level of topic folders is the default; nest only for a sub-area with several pages of its own.');
    }
  }

  // Names and slugs.
  const bySlug = new Map();
  for (const path of pages) {
    const relativePath = relativeToDocs(path);
    const slug = path === index ? 'index' : docSlug(relativePath);
    const segments = slug.split('/');
    if (segments.some((segment) => !SAFE_SEGMENT.test(segment))) {
      report('DOCS-005', 'error', repoPath(path), undefined, `"${relativePath}" reduces to the unpublishable slug "${slug}"; the site's sync fails on it.`, 'Rename every folder and the file with letters, digits and dashes.');
    } else {
      const unsafe = relativePath.replace(/\.md$/i, '').split('/').filter((segment) => !/^[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*$/.test(segment));
      if (unsafe.length > 0) {
        report('DOCS-005', 'warning', repoPath(path), undefined, `"${unsafe.join('/')}" is rewritten to publish (${slug}).`, 'Rename with letters, digits and single dashes so the file name and the URL agree.');
      }
    }
    if (slug === 'prerelease' || slug.startsWith('prerelease/')) {
      report('DOCS-005', 'error', repoPath(path), undefined, '"prerelease" is reserved for the prerelease documentation channel.', 'Rename the page or folder.');
    }
    const existing = bySlug.get(slug);
    if (existing) {
      report('DOCS-006', 'error', repoPath(path), undefined, `Publishes at the same URL as ${repoPath(existing)} (${slug}).`, 'Rename one of them.');
    } else {
      bySlug.set(slug, path);
    }
  }
  pageSlugs = new Set(bySlug.keys());

  // Catalogue deep links: a use case's docsPage that is not a page fails the site's catalogue check.
  for (const docsPage of catalogue?.docsPages ?? []) {
    if (!pageSlugs.has(docsPage)) {
      report('CAT-003', 'error', docsRootRelative, undefined, `The catalogue deep-links "${docsPage}" (useCases[].docsPage), which is not a page here; the site's build fails.`, 'Keep that page at the docs root under its name, or ask the site maintainers to update the use case in the same release.');
    }
  }
  if (catalogue && Object.keys(catalogue.redirects).length > 0) {
    report('CAT-004', 'info', `${docsRootRelative}/_redirects.yml`, undefined, `The catalogue entry carries ${Object.keys(catalogue.redirects).length} docs.redirects pair(s).`, 'Move them into _redirects.yml here, then ask the site maintainers to drop them from the entry.');
  }

  // Sidebar.
  const sidebar = docFiles.find((path) => /^_Sidebar\.md$/i.test(basename(path)));
  if (!sidebar && pages.length > 3) {
    report('DOCS-011', 'info', docsRootRelative, undefined, 'No _Sidebar.md; pages are ordered alphabetically on the site.', 'Add _Sidebar.md listing every page once, grouped by section, in reading order.');
  } else if (sidebar) {
    const listed = new Set();
    const text = read(sidebar);
    for (const match of text.matchAll(/\]\(([^)\s#]+\.md)(?:#[^)]*)?\)/g)) {
      const target = posix.normalize(match[1]);
      listed.add(target.toLowerCase());
      if (!existsSync(join(docsRoot, target))) {
        report('DOCS-011', 'warning', repoPath(sidebar), undefined, `Lists ${match[1]}, which does not exist.`, 'Fix or remove the entry.');
      }
    }
    for (const path of pages) {
      const relativePath = relativeToDocs(path);
      if (path !== index && !listed.has(relativePath.toLowerCase())) {
        report('DOCS-011', 'info', repoPath(path), undefined, 'Not listed in _Sidebar.md, so it sorts last on the site.', 'Add it to _Sidebar.md under its section.');
      }
    }
  }

  // Redirects.
  const redirectsFile = join(docsRoot, '_redirects.yml');
  const redirected = new Set();
  if (existsSync(redirectsFile)) {
    const { pairs, errors } = parseRedirects(read(redirectsFile));
    for (const error of errors) {
      report('RED-001', 'error', repoPath(redirectsFile), error.line, `${error.message}; the site's sync fails on an unreadable redirects file.`, 'Use one `previous: current` pair per line under `redirects:`.');
    }
    for (const pair of pairs) {
      const from = redirectSlug(pair.from);
      const to = redirectSlug(pair.to);
      redirected.add(from);
      if (from.split('/').some((segment) => !SAFE_SEGMENT.test(segment)) || to.split('/').some((segment) => !SAFE_SEGMENT.test(segment))) {
        report('RED-001', 'error', repoPath(redirectsFile), pair.line, `"${pair.from}: ${pair.to}" does not reduce to publishable slugs.`, 'Use slugs or Markdown paths from the docs root.');
      } else if (from === to) {
        report('RED-001', 'error', repoPath(redirectsFile), pair.line, `"${pair.from}" redirects to itself.`, 'Remove the line.');
      } else if (!pageSlugs.has(to)) {
        report('RED-002', 'warning', repoPath(redirectsFile), pair.line, `"${pair.from}" points at "${to}", which is not a page; no redirect is written.`, 'Point it at the page\'s current path.');
      } else if (pageSlugs.has(from)) {
        report('RED-003', 'info', repoPath(redirectsFile), pair.line, `"${from}" is still a page, so this redirect stays inactive.`, 'Expected only while the move is unreleased; otherwise remove the old page.');
      }
    }
    const sources = new Set(pairs.map((pair) => redirectSlug(pair.from)));
    for (const pair of pairs) {
      if (sources.has(redirectSlug(pair.to))) {
        report('RED-005', 'warning', repoPath(redirectsFile), pair.line, `"${pair.from}" points at "${pair.to}", which itself redirects.`, 'Point every old slug at the final page.');
      }
    }
  }

  // Moves in git history that left no redirect (the catalogue's docs.redirects counts, when given).
  for (const from of Object.keys(catalogue?.redirects ?? {})) {
    redirected.add(redirectSlug(from));
  }
  try {
    const log = execFileSync(
      'git',
      ['-C', root, 'log', '--max-count=300', '--find-renames', '--diff-filter=RD', '--name-status', '--format=', '--', docsRootRelative],
      { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] },
    );
    const missing = new Map();
    const fromDocs = (path) => path.slice(docsRootRelative.length + 1);
    for (const line of log.split('\n')) {
      const [status, oldPath, newPath] = line.split('\t');
      if (!status || !oldPath || !oldPath.endsWith('.md') || !oldPath.startsWith(`${docsRootRelative}/`)) continue;
      if (/^_/.test(basename(oldPath)) || isIndexName(basename(oldPath))) continue;
      const oldSlug = docSlug(fromDocs(oldPath));
      if (pageSlugs.has(oldSlug) || redirected.has(oldSlug) || missing.has(oldSlug)) continue;
      const newSlug =
        status.startsWith('R') && newPath?.startsWith(`${docsRootRelative}/`) ? docSlug(fromDocs(newPath)) : undefined;
      missing.set(oldSlug, newSlug && pageSlugs.has(newSlug) ? newSlug : undefined);
    }
    for (const [slug, current] of missing) {
      report(
        'RED-004',
        'warning',
        `${docsRootRelative}/_redirects.yml`,
        undefined,
        current
          ? `"${slug}" was renamed to "${current}" in git history and has no redirect.`
          : `"${slug}" was moved or deleted in git history and has no redirect.`,
        current
          ? `Add \`${slug}: ${current}\` to _redirects.yml (unless it was never published).`
          : `If it was published, add \`${slug}: <current-slug>\` to _redirects.yml. Pass --catalogue to count the site's docs.redirects.`,
      );
    }
  } catch {
    // Not a git checkout, or git is unavailable: the history check is best-effort.
  }
}

// ---------------------------------------------------------------------------
// Per-page checks (docs pages and the README)
// ---------------------------------------------------------------------------

for (const file of markdownFiles) {
  const page = repoPath(file);
  const scan = scanFences(read(file));

  const titles = [];
  scan.prose.forEach((line, index) => {
    if (/^#\s+\S/.test(line)) titles.push(index + 1);
  });
  if (titles.length === 0) {
    report('DOCS-007', 'warning', page, 1, 'No `# Title` heading; the site falls back to the file name.', 'Open the page with exactly one `# Title`.');
  } else if (titles.length > 1) {
    report('DOCS-007', 'warning', page, titles[1], `${titles.length} \`#\` headings; the site keeps the first as the title and demotes the rest.`, 'Use one `#` title and `##` for sections.');
  }

  scan.prose.forEach((rawLine, index) => {
    const line = stripInlineCode(rawLine);
    for (const match of line.matchAll(/(!?)\[[^\]]*\]\(([^)\s]+)(?:\s+"[^"]*")?\)/g)) {
      const target = match[2];
      if (/^(?:[a-z][a-z0-9+.-]*:|#|\/\/|<)/i.test(target)) continue;
      const path = decodeURI(target.split('#')[0].split('?')[0]);
      if (!path) continue;
      const absolute = path.startsWith('/') ? join(root, path) : join(dirname(file), path);
      if (!existsSync(absolute)) {
        const isImage = match[1] === '!';
        report(isImage ? 'DOCS-009' : 'DOCS-008', 'error', page, index + 1, `${isImage ? 'Image' : 'Link'} target ${target} does not exist.`, 'Fix the path — it resolves from the folder this page is in.');
      }
    }
  });

  for (const fence of scan.fences) {
    const language = fence.info.split(/\s+/)[0];
    if (fence.unclosed) {
      report('DOCS-013', 'error', page, fence.start + 1, 'A code fence is never closed; everything after it renders as code.', `Close it with ${fence.run}.`);
    }
    if (!language) {
      report('DOCS-010', 'info', page, fence.start + 1, 'A code fence has no language.', 'Label it (`csharp`, `json`, `shell`, `text`).');
    }
    const kind = classifyTextFence(fence);
    if (kind === 'diagram') {
      report('DIAG-001', 'warning', page, fence.start + 1, 'An ASCII-art diagram.', 'Replace it with a LikeC4 model plus a Mermaid fence in a purview:likec4 block (purview-site-docs, references/diagrams.md). Move any legend into a table.');
    } else if (kind === 'mermaid') {
      report('DIAG-008', 'warning', page, fence.start + 1, 'Mermaid source in a text fence, so neither GitHub nor the site draws it.', 'Label the fence ```mermaid — both render it — and, for an architecture or flow diagram, pair it with a LikeC4 model in a purview:likec4 block.');
    }
  }

  const blocks = checkDiagramBlocks(file, scan);
  const insideBlock = (line) => blocks.some((block) => line > block.start && line < block.end);
  for (const fence of scan.fences) {
    const language = fence.info.split(/\s+/)[0];
    if (insideBlock(fence.start)) continue;
    if (language === 'likec4' || language === 'c4') {
      if (language === 'likec4') {
        report('DIAG-003', 'warning', page, fence.start + 1, 'A bare ```likec4 fence: GitHub shows it as source code.', 'Move the model to a .c4 file and use a purview:likec4 block with a Mermaid fence for GitHub.');
      }
    } else if (language === 'mermaid') {
      report('DIAG-002', 'info', page, fence.start + 1, 'A Mermaid diagram with no LikeC4 pair. Both GitHub and the site render it.', 'Optional: for an architecture or flow diagram, add a LikeC4 model and wrap this fence in a purview:likec4 block.');
    }
  }
}

// ---------------------------------------------------------------------------
// Agent skills
// ---------------------------------------------------------------------------

const skillsRoot = join(root, 'skills');
if (existsSync(skillsRoot) && statSync(skillsRoot).isDirectory()) {
  for (const entry of readdirSync(skillsRoot, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;
    const folder = `skills/${entry.name}`;
    const skillFile = join(skillsRoot, entry.name, 'SKILL.md');
    if (!existsSync(skillFile)) {
      report('SKILL-001', 'error', folder, undefined, 'A skills folder with no SKILL.md.', 'Add SKILL.md, or move the folder out of skills/.');
      continue;
    }
    const text = read(skillFile);
    const frontMatter = /^---\n([\s\S]*?)\n---/.exec(text)?.[1];
    if (!frontMatter) {
      report('SKILL-002', 'error', `${folder}/SKILL.md`, 1, 'No YAML front matter.', 'Start the file with `---`, `name:` and `description:`, then `---`.');
      continue;
    }
    const name = /^name:\s*["']?([^"'\n]+?)["']?\s*$/m.exec(frontMatter)?.[1];
    const description = /^description:\s*(.*)$/m.exec(frontMatter)?.[1];
    if (!name) {
      report('SKILL-002', 'error', `${folder}/SKILL.md`, 1, 'Front matter has no `name`.', `Add \`name: ${entry.name}\`.`);
    } else if (name !== entry.name) {
      report('SKILL-003', 'error', `${folder}/SKILL.md`, 1, `name "${name}" does not match the folder "${entry.name}".`, 'Make them equal; the CLI and the site key the skill on the folder.');
    }
    if (name && !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(name)) {
      report('SKILL-004', 'warning', `${folder}/SKILL.md`, 1, `name "${name}" is not a lower-case slug.`, 'Use lower-case letters, digits and dashes.');
    }
    if (description === undefined || description.trim() === '') {
      report('SKILL-002', 'error', `${folder}/SKILL.md`, 1, 'Front matter has no `description`.', 'Say what the skill does and when to use it, in one paragraph.');
    }
    const scan = scanFences(text);
    scan.prose.forEach((line, index) => {
      for (const match of stripInlineCode(line).matchAll(/\]\(([^)\s#:]+)(?:#[^)]*)?\)/g)) {
        if (!existsSync(join(skillsRoot, entry.name, match[1]))) {
          report('SKILL-005', 'error', `${folder}/SKILL.md`, index + 1, `Links ${match[1]}, which is not in the skill folder.`, 'Ship the file beside SKILL.md, or fix the link.');
        }
      }
    });
  }
}

// ---------------------------------------------------------------------------
// Report
// ---------------------------------------------------------------------------

const order = { error: 0, warning: 1, info: 2 };
findings.sort((a, b) => order[a.severity] - order[b.severity] || a.id.localeCompare(b.id) || String(a.file).localeCompare(String(b.file)));
const counts = { error: 0, warning: 0, info: 0 };
for (const finding of findings) counts[finding.severity] += 1;

if (json) {
  console.log(JSON.stringify({ root, docsRoot: docsRootRelative ?? null, pages: pageSlugs.size, counts, findings }, null, 2));
} else {
  const lines = [
    `# purview.dev assessment: ${basename(root)}`,
    '',
    `Docs root: ${docsRootRelative ?? '(none — README only)'} · pages: ${pageSlugs.size || (readmePath ? 1 : 0)} · ` +
      `${counts.error} error(s), ${counts.warning} warning(s), ${counts.info} note(s)`,
    '',
  ];
  if (findings.length === 0) {
    lines.push('No findings.');
  } else {
    lines.push('| Severity | Rule | Where | Finding | Fix |', '| -- | -- | -- | -- | -- |');
    const cell = (value) => String(value).replace(/\|/g, '\\|').replace(/\n/g, ' ');
    for (const finding of findings) {
      const where = finding.file ? `${finding.file}${finding.line ? `:${finding.line}` : ''}` : '—';
      lines.push(`| ${finding.severity} | ${finding.id} | ${cell(where)} | ${cell(finding.message)} | ${cell(finding.fix)} |`);
    }
  }
  console.log(lines.join('\n'));
}

process.exit(counts.error > 0 ? 1 : 0);
