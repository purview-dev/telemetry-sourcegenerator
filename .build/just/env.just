# The environment every recipe runs in: telemetry opt-outs, and the .NET CLI speed-ups.
#
# `export` hands each value to every process a recipe starts - dotnet, bun, pwsh, git. Each one is a
# default: `env(NAME, value)` keeps whatever the caller's environment already says, so a developer
# who has opted back in, or a CI job that sets its own value, is never overridden.

# -- Telemetry and tracking --------------------------------------------------------------------------

# The .NET CLI, MSBuild and Aspire.
export DOTNET_CLI_TELEMETRY_OPTOUT := env("DOTNET_CLI_TELEMETRY_OPTOUT", "1")
export MSBUILD_TELEMETRY_OPTOUT := env("MSBUILD_TELEMETRY_OPTOUT", "1")
export ASPIRE_CLI_TELEMETRY_OPTOUT := env("ASPIRE_CLI_TELEMETRY_OPTOUT", "true")
export ASPIRE_DASHBOARD_TELEMETRY_OPTOUT := env("ASPIRE_DASHBOARD_TELEMETRY_OPTOUT", "true")

# The Azure SDKs and the Azure MCP server.
export AZURE_CORE_COLLECT_TELEMETRY := env("AZURE_CORE_COLLECT_TELEMETRY", "0")
export AZURE_MCP_COLLECT_TELEMETRY := env("AZURE_MCP_COLLECT_TELEMETRY", "0")

# PowerShell, for scripts a recipe runs through pwsh, and Git Credential Manager.
export POWERSHELL_TELEMETRY_OPTOUT := env("POWERSHELL_TELEMETRY_OPTOUT", "1")
export GCM_TELEMETRY_OPTOUT := env("GCM_TELEMETRY_OPTOUT", "1")

# JavaScript tooling: Next.js, Astro, Yarn, and the funding banners printed by postinstall scripts.
export NEXT_TELEMETRY_DISABLED := env("NEXT_TELEMETRY_DISABLED", "1")
export ASTRO_TELEMETRY_DISABLED := env("ASTRO_TELEMETRY_DISABLED", "1")
export YARN_ENABLE_TELEMETRY := env("YARN_ENABLE_TELEMETRY", "0")
export DISABLE_OPENCOLLECTIVE := env("DISABLE_OPENCOLLECTIVE", "true")

# Claude Code, when a recipe launches it.
export CLAUDE_CLI_TELEMETRY_OPTOUT := env("CLAUDE_CLI_TELEMETRY_OPTOUT", "1")
export CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC := env("CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC", "1")
export DISABLE_ERROR_REPORTING := env("DISABLE_ERROR_REPORTING", "1")
export DISABLE_TELEMETRY := env("DISABLE_TELEMETRY", "1")

# The cross-tool conventions, honoured by Bun and many other CLIs.
export DO_NOT_TRACK := env("DO_NOT_TRACK", "1")
export TELEMETRY_DISABLED := env("TELEMETRY_DISABLED", "1")

# -- .NET CLI speed and noise ------------------------------------------------------------------------

# No welcome banner or first-run notice.
export DOTNET_NOLOGO := env("DOTNET_NOLOGO", "1")

# The NativeAOT CLI host (.NET 11): near-instant `dotnet --version`, `--help` and tool launches, with
# everything else falling back to the managed CLI. On by default; opt out with DOTNET_CLI_ENABLEAOT=false.
export DOTNET_CLI_ENABLEAOT := env("DOTNET_CLI_ENABLEAOT", "true")

# The MSBuild server (DOTNET_CLI_USE_MSBUILD_SERVER) is deliberately left to the SDK's own default.
# Forcing it on makes every `dotnet build` share one long-lived MSBuild process, and its NuGet SDK
# resolver keeps the settings it first read: a build under a different nuget.config - a test
# harness's throwaway consumer, a second repository - then resolves SDKs with the wrong sources and
# fallback folders, and fails with MSB4242. Set it yourself where every build shares one NuGet
# configuration; `just scrub` shuts the server down.

# Multithreaded MSBuild (-mt) is deliberately not set here. Purview.BuildSdk bootstraps it into the
# repository's Directory.Build.rsp, so a plain `dotnet build` and an IDE build get it as well.
