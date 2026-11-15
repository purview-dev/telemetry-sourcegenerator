# Checking a claim against the code

For each kind of statement a document makes, where the truth lives and how to look it up. Search with
`git grep -n`, which only reads tracked files, and read the definition itself rather than a comment
that describes it.

| Claim | The truth | How to check |
| -- | -- | -- |
| An MSBuild property exists, and its default | the `.props`/`.targets` that define it (`Sdk/Props/*.props`, `Sdk/Sdk.props`, `Sdk/Sdk.targets`, `Sdk/Targets/*.targets`), and the `.buildschema.json` sidecar | `git grep -n '<Name\b' -- '*.props' '*.targets'`. The default is the unconditioned or `'$(Name)' == ''`-guarded assignment. For a real answer, evaluate one: `dotnet msbuild <project> -getProperty:Name` |
| A property must be set before the import | whether `Sdk.props`, or a file it imports, *reads* it | a read in a value or `Condition` in `Sdk.props`'s import graph, other than its own `== ''` guard, makes it pre-import |
| An analyzer reads `build_property.X` | a `CompilerVisibleProperty Include="X"` item, and the analyzer source | `git grep -n 'CompilerVisibleProperty' -- '*.props'` and `git grep -n 'build_property.X'` |
| A diagnostic id, its severity and its meaning | the `DiagnosticDescriptor` (analyzers) or the `Code="..."` on an MSBuild `Error`/`Warning`/`Message` (targets) | `git grep -n 'PRSKL0012'`. The severity is the descriptor's default or the task's `Importance`, not the docs table |
| A target exists and what it runs | the `<Target Name="...">`, its `DependsOnTargets`, `BeforeTargets`/`AfterTargets` | read the target; "never runs during a build" means it has no hook *and* nothing hooks it |
| A `just` recipe and what it does | the `Justfile` and every file it imports | `just --list` for names, `just --dry-run <recipe>` for the exact command — never read a recipe name off another document |
| A command line works | running it | run it, or `--dry-run`/`-p:...DryRun=true` when it changes things; do not quote output you did not see |
| A file or folder path | the file system | `git ls-files <path>`, which also proves it is tracked rather than generated |
| A package id, and what it contains | the `.csproj` (`PackageId`, `IsPackable`) and the packed `.nupkg` | `dotnet pack`, then list the archive; pack validation config (for example `.config/purview-build.json` `RequiredContent`) lists the expected files |
| A target framework or framework set | the `.props` defining the sets, and the project's evaluated value | `dotnet msbuild <project> -getProperty:TargetFrameworks` |
| A version | `package.json` `version`, `global.json` `msbuild-sdks`, `Directory.Packages.props` | read the file. A quick-start snippet's pin is checked by the repository's version-sync check, if there is one |
| A skill exists, and what it answers | `skills/<name>/SKILL.md` frontmatter | `name` and `description`; a skill listed in `AGENTS.md` must exist, and every authored skill must be listed |
| "Tested by X" | the test | `git grep -n 'X'` under the test folder, and read the test: does it assert what the document says it does? |
| Behaviour ("the SDK does Y when Z") | the code path, and ideally the test that pins it | find both. If no test pins it, say so in your report rather than strengthening the claim |

## Reading a document for claims

Go through a page section by section and list every checkable statement before changing anything.
For each, record **kept** (found, matches), **fixed** (found, differs — the code wins), **removed**
(not found anywhere) or **question** (found but ambiguous, or the code looks wrong). Report the
removed and question rows to the user: a removed claim may describe something that should exist.

## Things that look true and are not

- **A comment in a `.props` file** describing a default the next line no longer sets.
- **The same table in two places,** both stale. Fix both from the code, then delete one and link to
  the other where the tests allow it.
- **A recipe name in a doc** from before the `Justfile` was split into modules: check
  `just --list`, not memory.
- **An example `global.json` or package version** — usually a pin the version-sync check owns; leave
  the number to it.
- **"Removed" features** still described in a page nobody re-read when they went. Search for the
  feature's property names as well as its title.
