# Agent instructions

The repository's primary instruction set is the [`AGENTS.md`](../AGENTS.md) file at the repository
root. Read it first and treat it as the source of truth for:

- Build, test, lint, format, and pack commands
- Project structure and layout
- Build and validation workflow
- Conventions (conventional commits, csharpier, `.slnx`, SDK behaviour)
- Source-generator and testing skills (`.agents/skills/`)
- Benchmarking and performance documentation rules
- Release process

If you are working on source generators, analyzers, code fixes, refactorings, or tests for Roslyn
components, load the relevant skill listed under "Source-generator and testing skills" in `AGENTS.md`
before starting. Fall back to searching the repository only when `AGENTS.md` does not match what you
observe.