#!/usr/bin/env node
/**
 * Snapshots every public, non-archived `purview-dev` repository at `main` into
 * `.config/org-inventory.json`, and reports the drift between that snapshot and the organisation.
 *
 * The snapshot is what makes the harvest rule checkable offline: `OrgInventoryTests` reads the file
 * the pipeline has, never the network. This script is the only thing that writes it, so a repository
 * cannot appear in the snapshot by being remembered.
 *
 * Zero dependencies: it shells out to `gh` and so reuses the CLI's own authentication rather than
 * asking for a token.
 *
 *   node inventory.mjs                   # print the drift against the snapshot (default)
 *   node inventory.mjs --write           # rewrite the snapshot from the organisation
 *   node inventory.mjs --repo containers # narrow to one repository; merges on --write
 *   node inventory.mjs --out <path>      # write somewhere else, e.g. while adopting the skill
 *   node inventory.mjs --help
 *
 * Exit codes: 0 on success, including when drift is found - a stale snapshot is a review obligation,
 * not a red pull request for whoever happened to push next. 1 when `gh` is missing, unauthenticated,
 * or the organisation cannot be read.
 */
import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";

/** The organisation this playbook describes. */
const ORG = "purview-dev";

/** The branch a convention is read at. A local clone is a cache; this is the authority. */
const BRANCH = "main";

/** Bumped when a field is added or its meaning changes. */
const SCHEMA_VERSION = 1;

/** Past this age the snapshot is reported as stale. Advisory: nothing fails on a clock. */
const STALE_AFTER_DAYS = 30;

/** Files a repository may put at its root to guide an agent. Recorded in this order. */
const AGENT_GUIDANCE_FILES = ["AGENTS.md", "CLAUDE.md", ".github/copilot-instructions.md", "llms.txt"];

/** Directories a published documentation site is read from, most specific first. */
const DOCS_DIRECTORIES = ["docs/wiki", "docs"];

/**
 * The concern each repository is best-in-class for, so a harvest reads one owner instead of sixteen
 * repositories. `references/best-in-class.md` is the long form of this table and is the file to edit
 * first; this map only records the answer in the snapshot.
 */
const CONCERNS = new Map([
	["architests", ["architecture policy", "dependency and naming rules"]],
	["aspire-resourcekit", ["Aspire hosting resources"]],
	["aspirec4", ["architecture diagrams from source"]],
	["astroc4", ["architecture diagrams in an Astro site"]],
	["build", ["pipeline", "release"]],
	["build-sdk", ["repository shape", "analyzers and style", "MSBuild defaults", "skill authoring"]],
	["containers", ["local dependencies in tests"]],
	["engineering-playbook", ["the conventions themselves"]],
	["event-sourcing", ["event streams", "event storage"]],
	["openapi-xmlcomments", ["OpenAPI from XML comments"]],
	["pm-skills", ["skill-set design"]],
	["purview-dev.github.io", ["the public project catalogue"]],
	["results", ["operation outcomes"]],
	["sourcegenerator-framework", ["source generator authoring"]],
	["telemetry-sourcegenerator", ["logging", "tracing", "metrics"]],
	["value-objects", ["validated identity types"]],
	["zodsharp", ["TypeScript contracts from C#"]],
]);

/** Runs `gh` and returns stdout, turning a CLI failure into an actionable message. */
function gh(args) {
	try {
		return execFileSync("gh", args, {
			encoding: "utf8",
			maxBuffer: 256 * 1024 * 1024,
			stdio: ["ignore", "pipe", "pipe"],
		});
	} catch (error) {
		const detail = (error.stderr ?? "").toString().trim() || error.message;
		throw new Error(
			`gh ${args.join(" ")} failed.\n${detail}\n\n` +
				"This script reads the organisation, not a local clone. Run `gh auth status`, then retry.",
		);
	}
}

/** Calls one GitHub REST endpoint and parses the JSON response. */
function api(endpoint) {
	return JSON.parse(gh(["api", endpoint]));
}

/** The root of the repository the command was run in, so the skill works from any repository. */
function repositoryRoot() {
	try {
		return execFileSync("git", ["rev-parse", "--show-toplevel"], {
			encoding: "utf8",
			stdio: ["ignore", "pipe", "pipe"],
		}).trim();
	} catch {
		throw new Error("Not inside a git repository. Run this from a clone, or pass --out <path>.");
	}
}

/** Membership, as the organisation reports it. The catalogue is a convenience index, not the list. */
function readMembership(source) {
	const listed = source
		? JSON.parse(readFileSync(source, "utf8"))
		: JSON.parse(
				gh([
					"repo",
					"list",
					ORG,
					"--limit",
					"200",
					"--json",
					"name,visibility,isArchived,pushedAt,defaultBranchRef",
				]),
			);

	return (
		listed
			// Deny by default. The filter is "exactly public and not archived" rather than "not
			// private", so a visibility value this script has never seen fails closed instead of
			// falling through into a file that is committed to a public repository.
			.filter((repository) => repository.visibility === "PUBLIC" && repository.isArchived === false)
			.sort((left, right) => left.name.localeCompare(right.name, "en"))
	);
}

/** The recorded commit and file list for each repository, when a run replays a previous harvest. */
function readDetails(source) {
	return new Map(Object.entries(JSON.parse(readFileSync(source, "utf8"))));
}

/** The Markdown pages directly inside a docs directory. */
function countPages(paths, directory) {
	const prefix = `${directory}/`;
	return paths.filter((path) => {
		if (!path.startsWith(prefix)) {
			return false;
		}

		const remainder = path.slice(prefix.length);
		return remainder.endsWith(".md") && !remainder.includes("/");
	}).length;
}

/** A skill is a folder under `skills/` that carries a `SKILL.md`. */
function authoredSkills(paths) {
	const names = paths
		.filter((path) => /^skills\/[^/]+\/SKILL\.md$/.test(path))
		.map((path) => path.split("/")[1]);

	return [...new Set(names)].sort((left, right) => left.localeCompare(right, "en"));
}

/** Everything the snapshot records about one repository, read at `BRANCH`. */
function describeRepository(repository, details) {
	// The filter above already excluded these, and this is the second lock on the same door: a
	// private repository must never reach the snapshot, so the guard is here as well as there.
	if (repository.visibility !== "PUBLIC" || repository.isArchived !== false) {
		throw new Error(
			`${ORG}/${repository.name} is ${repository.visibility} and archived ${repository.isArchived}. ` +
				"The snapshot records public, unarchived repositories only, so nothing was written.",
		);
	}

	const defaultBranch = repository.defaultBranchRef?.name ?? BRANCH;
	if (defaultBranch !== BRANCH) {
		throw new Error(
			`${ORG}/${repository.name} has default branch '${defaultBranch}', not '${BRANCH}'. ` +
				"The snapshot pins one branch, so either the rule or the repository has to change.",
		);
	}

	const recorded = details?.get(repository.name);
	if (details && !recorded) {
		throw new Error(
			`${repository.name} has no recorded detail, so --details cannot describe it. ` +
				"Replay a membership the details file was captured with, or run without --details.",
		);
	}

	const commitSha = recorded?.commitSha ?? api(`repos/${ORG}/${repository.name}/git/ref/heads/${BRANCH}`).object.sha;
	const paths = recorded?.paths ?? readTree(repository.name);
	const docsDir = DOCS_DIRECTORIES.find((candidate) =>
		paths.some((path) => path.startsWith(`${candidate}/`)),
	);

	return {
		name: repository.name,
		visibility: repository.visibility.toLowerCase(),
		archived: repository.isArchived,
		commitSha,
		pushedAt: repository.pushedAt,
		docsDir: docsDir ?? null,
		docsPages: docsDir ? countPages(paths, docsDir) : 0,
		skills: authoredSkills(paths),
		agentGuidance: AGENT_GUIDANCE_FILES.filter((file) => paths.includes(file)),
		concerns: CONCERNS.get(repository.name) ?? [],
	};
}

/** The blob paths of one repository at `BRANCH`. */
function readTree(name) {
	const tree = api(`repos/${ORG}/${name}/git/trees/${BRANCH}?recursive=1`);

	if (tree.truncated) {
		process.stderr.write(
			`warning: ${name}'s tree is truncated, so its skills and docs are undercounted\n`,
		);
	}

	return tree.tree.filter((entry) => entry.type === "blob").map((entry) => entry.path);
}

/** The snapshot document. */
function snapshotOf(repositories) {
	return {
		schemaVersion: SCHEMA_VERSION,
		org: ORG,
		branch: BRANCH,
		fetchedAt: new Date().toISOString().replace(/\.\d{3}Z$/, "Z"),
		repositories,
	};
}

/** The snapshot on disk, or null when the repository has not committed one yet. */
function readSnapshot(path) {
	if (!existsSync(path)) {
		return null;
	}

	// The snapshot is the one file in this workflow a person is likely to open and save by hand, and
	// an editor that writes a byte-order mark produces a file JSON.parse rejects with a message that
	// names neither the file nor the cause.
	return JSON.parse(readFileSync(path, "utf8").replace(/^\uFEFF/, ""));
}

/** A field's value in the form the drift report prints. */
function render(value) {
	if (value === null || value === undefined) {
		return "none";
	}

	if (Array.isArray(value)) {
		return value.length === 0 ? "none" : value.join(", ");
	}

	return String(value);
}

/** A field's value in the form the drift report compares: structured values compare as one string. */
function fingerprint(value) {
	return JSON.stringify(value ?? null);
}

/** The fields whose change is worth reporting, in the order a reader cares about them. */
const COMPARED_FIELDS = [
	"commitSha",
	"docsDir",
	"docsPages",
	"skills",
	"agentGuidance",
	"concerns",
	"visibility",
	"archived",
];

/** The differences between the organisation and the snapshot. */
function drift(existing, observed) {
	const findings = [];
	const before = new Map((existing?.repositories ?? []).map((entry) => [entry.name, entry]));
	const after = new Map(observed.map((entry) => [entry.name, entry]));

	for (const [name, entry] of after) {
		if (!before.has(name)) {
			findings.push(`${name}: not in the snapshot`);
			continue;
		}

		const previous = before.get(name);
		for (const field of COMPARED_FIELDS) {
			if (fingerprint(previous[field]) === fingerprint(entry[field])) {
				continue;
			}

			findings.push(`${name}: ${field} ${render(previous[field])} -> ${render(entry[field])}`);
		}
	}

	for (const name of before.keys()) {
		if (!after.has(name)) {
			findings.push(`${name}: in the snapshot but no longer a public repository`);
		}
	}

	return findings;
}

/** How long ago the snapshot was taken, in whole days, or null when it carries no usable date. */
function ageInDays(fetchedAt) {
	const fetched = Date.parse(fetchedAt ?? "");
	return Number.isNaN(fetched) ? null : Math.floor((Date.now() - fetched) / 86_400_000);
}

/** Parses the command line. Unknown arguments are a mistake worth failing on. */
function parseArguments(argv) {
	const options = { write: false, repos: [], out: null, from: null, details: null, help: false };

	for (let index = 0; index < argv.length; index++) {
		const argument = argv[index];
		if (argument === "--write") {
			options.write = true;
		} else if (argument === "--print") {
			options.write = false;
		} else if (argument === "--help" || argument === "-h") {
			options.help = true;
		} else if (argument === "--repo") {
			const value = argv[++index];
			if (!value) {
				throw new Error("--repo needs a repository name.");
			}

			options.repos.push(value);
		} else if (argument === "--out") {
			const value = argv[++index];
			if (!value) {
				throw new Error("--out needs a path.");
			}

			options.out = value;
		} else if (argument === "--from") {
			const value = argv[++index];
			if (!value) {
				throw new Error("--from needs a path to a recorded `gh repo list` response.");
			}

			options.from = value;
		} else if (argument === "--details") {
			const value = argv[++index];
			if (!value) {
				throw new Error("--details needs a path to a recorded detail file.");
			}

			options.details = value;
		} else {
			throw new Error(`Unknown argument '${argument}'. Run with --help.`);
		}
	}

	return options;
}

/** The documented usage. */
const HELP = `Snapshot every public, non-archived ${ORG} repository at ${BRANCH}.

  node inventory.mjs               print the drift between the organisation and the snapshot
  node inventory.mjs --write       rewrite the snapshot from the organisation
  node inventory.mjs --repo <name> narrow to one repository; merges when writing
  node inventory.mjs --out <path>  write somewhere other than .config/org-inventory.json
  node inventory.mjs --from <path> replay a recorded "gh repo list --json" response instead of
                                   asking the organisation
  node inventory.mjs --details <path> replay recorded commits and file lists, so a run touches no
                                   network at all; pair it with --from
  node inventory.mjs --help        this text

The snapshot is generated, never hand-edited. A stale snapshot is a review obligation rather than a
build failure, so drift prints and the exit code stays 0.`;

/** Runs the command. */
function main() {
	const options = parseArguments(process.argv.slice(2));
	if (options.help) {
		console.log(HELP);
		return;
	}

	const snapshotPath = options.out ?? join(repositoryRoot(), ".config", "org-inventory.json");
	const existing = readSnapshot(snapshotPath);

	let observed = readMembership(options.from);
	if (options.repos.length > 0) {
		const known = new Set(observed.map((repository) => repository.name));
		for (const name of options.repos) {
			if (!known.has(name)) {
				throw new Error(
					`'${name}' is not a public, non-archived ${ORG} repository. ` +
						`Run without --repo to include all ${observed.length}.`,
				);
			}
		}

		observed = observed.filter((repository) => options.repos.includes(repository.name));
	}

	const details = options.details ? readDetails(options.details) : null;
	const described = observed.map((repository) => describeRepository(repository, details));

	if (options.write) {
		const merged = new Map((existing?.repositories ?? []).map((entry) => [entry.name, entry]));
		for (const entry of described) {
			merged.set(entry.name, entry);
		}

		const written = snapshotOf(
			[...merged.values()].sort((left, right) => left.name.localeCompare(right.name, "en")),
		);

		// The merge carries forward whatever the file already held, which is how a hand-edited entry
		// would survive a regeneration. Refuse to write a snapshot that records anything else.
		const notPublic = written.repositories.filter(
			(entry) => entry.visibility !== "public" || entry.archived !== false,
		);
		if (notPublic.length > 0) {
			throw new Error(
				"Refusing to write a snapshot that records a repository which is not public and " +
					`unarchived: ${notPublic.map((entry) => entry.name).join(", ")}. ` +
					"Delete the entries and re-run --write.",
			);
		}

		mkdirSync(dirname(snapshotPath), { recursive: true });
		writeFileSync(snapshotPath, `${JSON.stringify(written, null, "\t")}\n`, "utf8");
		console.log(
			`wrote ${written.repositories.length} repositories to ${snapshotPath} at ${written.fetchedAt}`,
		);
		return;
	}

	const findings = drift(existing, described);
	const age = ageInDays(existing?.fetchedAt);

	console.log(`${ORG}: ${observed.length} public, non-archived repositories at ${BRANCH}`);
	if (existing) {
		console.log(
			`snapshot: ${existing.repositories?.length ?? 0} repositories, fetched ${existing.fetchedAt}` +
				(age === null ? "" : ` (${age} day${age === 1 ? "" : "s"} old)`),
		);
	} else {
		console.log(`no snapshot at ${snapshotPath} - run with --write to create one`);
	}

	if (findings.length === 0) {
		console.log("no drift");
	} else {
		console.log(`${findings.length} difference${findings.length === 1 ? "" : "s"}:`);
		for (const finding of findings) {
			console.log(`  ${finding}`);
		}
	}

	if (age !== null && age > STALE_AFTER_DAYS) {
		console.log(
			`warning: the snapshot is older than ${STALE_AFTER_DAYS} days. ` +
				"Re-run with --write and review the diff.",
		);
	}
}

try {
	main();
} catch (error) {
	console.error(`error: ${error.message}`);
	process.exitCode = 1;
}
