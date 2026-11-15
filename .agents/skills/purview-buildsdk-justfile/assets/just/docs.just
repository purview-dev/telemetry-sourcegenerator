# Optional: the project site a pm-docs-site skill (purview-dev/pm-skills) scaffolds. Copy this file
# into the module folder only when the repository has one; the barrel imports it if present. Each
# recipe runs inside the site, so none of them needs `cd`. Requires Bun.

# The site folder, relative to the repository root - pm-docs-site's SITE_ROOT.
docs_site := "src/docs-site"

# Serve the project site at http://localhost:4321
[group('Docs')]
[working-directory(docs_site)]
docs: docs-install
    bun run dev

# Install the project site's dependencies from its lockfile
[group('Docs')]
[working-directory(docs_site)]
docs-install:
    bun install --frozen-lockfile

# Test, type-check and build the project site - its build check
[group('Docs')]
[working-directory(docs_site)]
docs-check: docs-install
    bun run test
    bun run check
    bun run build
