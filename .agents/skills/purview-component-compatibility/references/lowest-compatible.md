# Proving the floor

The rule is "declare the lowest compatible version". This file is the mechanics: how to find the floor,
how to prove it in a build, and the patterns that keep it from silently rising.

## What "compatible" means, precisely

A version is a valid floor when all three hold:

1. **It compiles against every API the component uses.** The compiler decides this, not a changelog. If
   you cannot say which member forced the version up, you have not found the floor.
2. **The component's own tests pass against it**, including the consumer-verification suite, which
   builds a throwaway consumer rather than the component's own test project.
3. **It is supported on every target framework in that dependency group.** A floor is per group, and a
   multi-targeting component can have several.

## Finding it

```bash
dotnet list package --outdated                          # what you declared against what exists
dotnet list package --include-transitive                # what actually resolved
dotnet nuget why <project> <package>                    # which path pulled a version in
```

Then work downward: lower the declared version, build, and read the compiler. The first version that
compiles *and* passes is the floor. Do not stop at the first failure and pick the version above it —
that is how a floor ends up one minor above what the code needs.

## Making the floor a build leg

Keep one property per family (so a family cannot be bumped halfway) and make each conditional on a floor
switch. Nothing here is NuGet-specific magic: it is MSBuild, and central package management already
gives you the single place to put it.

```xml
<PropertyGroup Label="Purview Versions">
	<PurviewTelemetryVersion Condition="'$(DependencyFloor)' != 'true'">LATEST</PurviewTelemetryVersion>
	<PurviewTelemetryVersion Condition="'$(DependencyFloor)' == 'true'">FLOOR</PurviewTelemetryVersion>
</PropertyGroup>
```

```bash
dotnet restore -p:DependencyFloor=true
dotnet build   -p:DependencyFloor=true --no-restore
dotnet test    -p:DependencyFloor=true --no-build
```

`LATEST` and `FLOOR` are literals in the file; the point is that they are two properties, not two files.
Run the ordinary leg at `LATEST` and a second, slower leg at `FLOOR`, and give the floor leg its own CI
job so a failure names the floor rather than the build.

Where a component multi-targets, add the framework dimension and run the pair per framework:

```bash
for tfm in net8.0 net9.0 net10.0; do
  dotnet build -p:DependencyFloor=true -f "$tfm"
done
```

## Transitive pins

A floor can be raised by something you never declared. Transitive pinning puts those versions where you
can see them:

```xml
<PropertyGroup>
	<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
</PropertyGroup>
```

Then an override is a deliberate line with a reason beside it — the persistence sample in this
repository does exactly that for a security-patched package the SQL client drags in. An override without
a comment is indistinguishable from an accident, and the next person deletes it.

## The ceiling, and the API surface

- The floor leg proves the **bottom**; the ordinary leg proves the **top**. Both, or you know neither.
- An API-compatibility check against the previous release catches the other direction of the same
  mistake: a floor can stay put while the public surface grows a member that only the newest dependency
  provides. `Microsoft.DotNet.ApiCompat.Task` wires that into the build; `Microsoft.DotNet.ApiCompat.Tool`
  runs it from a pipeline.
- A prerelease dependency is a floor with an expiry: it cannot be consumed by anyone who has not opted
  into prereleases. Say so in the package's README rather than letting the error message explain it.

## What not to do

| Temptation | Why it is wrong |
| -- | -- |
| `dotnet add package X` and commit | it writes the latest; correct for an application, wrong for a component |
| floating versions (`1.*`) | resolves differently on every restore, so the floor is nobody's decision |
| "it restores on my machine" | that proves one graph, on one date, with one SDK |
| raising a floor in a patch release | the consumer's build changes without their version changing |
| one floor leg for all frameworks | the framework with the lowest floor is the one nobody tested |
