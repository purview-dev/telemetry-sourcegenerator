#!/usr/bin/env pwsh
<#
.SYNOPSIS
	Proves the Purview.Telemetry.SourceGenerator package works for real consumers - Native AOT, Blazor WebAssembly
	(trimmed and AOT-compiled) and a Blazor Server host - built from the .nupkg alone.

.DESCRIPTION
	The in-repository tests drive the generator in-process, and the samples reference the generator project as an
	analyzer. Neither is what ships: an IL-merged generator and refactorings assembly under analyzers/, consumed through
	a PackageReference. A ProjectReference build also hides the class of bug this gate exists for:
	Purview.SourceGeneratorFramework.dll - a compiler-host-only assembly - leaked into the build output of every
	project that referenced a generator (SGF 1.2.1 put it in all eight sample bins here), and Blazor WebAssembly AOT
	compilation fails on it.

	This gate consumes the package exactly as a user does:

	1. Preflight: checks Purview.SourceGeneratorFramework (the generator's build-time framework, pinned in
	   Directory.Packages.props) and Purview.BuildSdk (the MSBuild SDK, pinned in global.json) can be restored, and fails
	   early, with what to do, if either cannot.
	2. Packs the shipping package (the ids in .config/purview-build.json PackValidation.RequiredContent) to an isolated
	   feed, with an isolated NUGET_PACKAGES, so no previously extracted package of the same version can be reused. The
	   feed must hold exactly the expected package, and it may carry no compiler-host assembly outside analyzers/ and no
	   generator assembly under lib/, ref/ or runtimes/.
	3. Copies the consumers in scripts/packed-consumers/ outside the repository, beside guard
	   Directory.Build.props/.targets and Directory.Packages.props files that stop any inheritance, with a nuget.config
	   that resolves Purview.Telemetry.SourceGenerator from the isolated feed only. Every consumer references
	   PackedTelemetry.Library, a class library with its own generated telemetry, so the generator runs in two
	   assemblies of each application:
	   (a) AotConsole - a Native AOT console. Published with -warnaserror (any IL2xxx/IL3xxx fails it), then run: with an
	       in-process ActivityListener, MeterListener and a capturing logger provider it checks the multi-target method
	       (an Activity, an Information log entry and a counter from one call), activity tags, baggage and events
	       (including the OpenTelemetry exception event), every metric measurement and its tags, the v1 and v2 log
	       entries, log-level gating (a Debug entry must not be written when only Information is enabled) and the
	       TelemetryNames aggregation of both assemblies. It exits non-zero on any mismatch.
	   (b) BlazorWasm - a Blazor WebAssembly app using the generated telemetry from a page and a service. Published
	       trimmed with -warnaserror, then again with RunAOTCompilation=true. _framework must hold both assemblies; the
	       assemblies ILLink wrote must still define every generated telemetry member (the library is IsTrimmable, so it
	       is genuinely trimmed); and the AOT publish must have compiled every generated method to WebAssembly.
	   (c) BlazorServer - a Blazor Web App with interactive server rendering. Published, then run with --self-test: the
	       host starts on an in-memory TestServer, the page is requested and rendered on the server, and the same
	       telemetry checks as (a) run against what the page emitted.
	4. (d) Scans every consumer bin and publish folder with scripts/assert-no-compiler-host-assemblies.ps1.

	The temporary workspace is deleted afterwards unless -KeepArtifacts is passed. Packing restores the shipping
	projects against the isolated cache, so they are restored again against the normal cache at the end.

.PARAMETER Configuration
	Configuration the package is packed in. Defaults to Release: this gate checks what ships.

.PARAMETER RuntimeIdentifier
	RID for the Native AOT console. Native AOT cannot cross-compile between operating systems, so it defaults to this
	machine's RID (win-x64, linux-x64, osx-arm64, ...).

.PARAMETER PrereleaseFeed
	A NuGet feed (folder or URL) to restore Purview.SourceGeneratorFramework and Purview.BuildSdk from, in addition to
	nuget.org - for versions that are not on nuget.org yet. The feed is used for those packages only. Defaults to
	$env:TELEMETRY_PRERELEASE_FEED, then $env:LOCAL_NUGET_FEED_PATH.

.PARAMETER WasmAot
	Auto (default) runs the WebAssembly AOT publish when the wasm-tools workload is installed and says so when it is
	skipped. Require fails when the workload is missing (CI installs it). Skip never runs it.

.PARAMETER PackOutput
	Also copy the packed .nupkg/.snupkg files here, so a later step can inspect exactly what this gate consumed - CI
	runs scripts/test-pack-validation-rules.ps1 against them.

.PARAMETER PreflightOnly
	Run only the restore checks, so CI can fail on them before installing toolchains.

.PARAMETER KeepArtifacts
	Leave the temporary workspace in place for inspection.

.EXAMPLE
	just smoke-packed-consumers

.EXAMPLE
	./scripts/test-packed-consumers.ps1 -RuntimeIdentifier linux-x64 -WasmAot Require -KeepArtifacts
#>
[CmdletBinding()]
param (
	[string] $Configuration = 'Release',
	[string] $RuntimeIdentifier = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier,
	[string] $PrereleaseFeed = ($env:TELEMETRY_PRERELEASE_FEED ? $env:TELEMETRY_PRERELEASE_FEED : $env:LOCAL_NUGET_FEED_PATH),
	[ValidateSet('Auto', 'Require', 'Skip')]
	[string] $WasmAot = 'Auto',
	[string] $PackOutput,
	[switch] $PreflightOnly,
	[switch] $KeepArtifacts
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$leakScan = Join-Path $PSScriptRoot 'assert-no-compiler-host-assemblies.ps1'
$consumerTemplates = Join-Path $PSScriptRoot 'packed-consumers'

$version = (Get-Content (Join-Path $repositoryRoot 'package.json') -Raw | ConvertFrom-Json).version
$packageVersions = [xml] (Get-Content (Join-Path $repositoryRoot 'Directory.Packages.props') -Raw)
function Get-PackageVersion([string] $id) {
	$value = $packageVersions.SelectSingleNode("//PackageVersion[@Include='$id']")?.GetAttribute('Version')
	if (-not $value) { throw "Could not read the $id version from Directory.Packages.props." }
	return $value
}
function Get-VersionProperty([string] $name) {
	$value = $packageVersions.SelectSingleNode("//$name")?.InnerText
	if (-not $value) { throw "Could not read the $name property from Directory.Packages.props." }
	return $value
}

$sgfVersion = Get-VersionProperty 'PurviewSGFVersion'
$extensionsVersion = Get-VersionProperty 'DotnetRuntimeVersion'
$extensionsTelemetryVersion = Get-VersionProperty 'MSExtensionVersion'
$aspNetCoreVersion = Get-PackageVersion 'Microsoft.AspNetCore.Components.WebAssembly'
$testHostVersion = Get-PackageVersion 'Microsoft.AspNetCore.TestHost'
if ($testHostVersion -ne $aspNetCoreVersion) {
	throw "Microsoft.AspNetCore.TestHost ($testHostVersion) and Microsoft.AspNetCore.Components.WebAssembly ($aspNetCoreVersion) must share a version in Directory.Packages.props."
}

$globalJson = Get-Content (Join-Path $repositoryRoot 'global.json') -Raw | ConvertFrom-Json
$sdk = $globalJson.sdk
$buildSdkVersion = $globalJson.'msbuild-sdks'.'Purview.BuildSdk'
if (-not $buildSdkVersion) { throw 'Could not read the Purview.BuildSdk pin from global.json msbuild-sdks.' }

# The shipping packages are exactly the ids pack validation knows about. Each maps to a project under src/src by its
# id without "Purview.Telemetry." - Purview.Telemetry.SourceGenerator to src/src/SourceGenerator.
$packValidation = (Get-Content (Join-Path $repositoryRoot '.config/purview-build.json') -Raw | ConvertFrom-Json).PackValidation
$shipping = foreach ($id in $packValidation.RequiredContent.PSObject.Properties.Name) {
	$folder = $id -ireplace '^purview\.telemetry\.', ''
	$directory = Get-ChildItem (Join-Path $repositoryRoot 'src/src') -Directory | Where-Object { $_.Name -ieq $folder }
	$project = $directory ? (Join-Path $directory.FullName "$($directory.Name).csproj") : $null
	if (-not $project -or -not (Test-Path $project)) {
		throw "No project for the shipping package '$id' (expected src/src/$folder/$folder.csproj)."
	}

	[pscustomobject]@{ Id = $id; Project = $project }
}

# The refactorings assembly is packed inside the generator package by an MSBuild task rather than a ProjectReference
# build, so it must be restored explicitly (NETSDK1004 otherwise).
$packInputs = @(
	$shipping.Project
	(Join-Path $repositoryRoot 'src/src/SourceGenerator.Refactorings/SourceGenerator.Refactorings.csproj')
)

$workspace = Join-Path ([System.IO.Path]::GetTempPath()) "tsg-packed-consumers-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
$feed = Join-Path $workspace 'feed'
$packages = Join-Path $workspace 'packages'
$logs = Join-Path $workspace 'logs'
$consumers = Join-Path $workspace 'consumers'
$output = Join-Path $workspace 'out'
$exe = $IsWindows ? '.exe' : ''

$timings = [System.Collections.Generic.List[object]]::new()
$scanned = [System.Collections.Generic.List[string]]::new()
$previousNuGetPackages = $env:NUGET_PACKAGES
$repositoryRestoreTouched = $false

function Write-Failure([string] $message) {
	if ($env:GITHUB_ACTIONS) { Write-Host "::error::$message" } else { Write-Host "ERROR: $message" -ForegroundColor Red }
}

function Invoke-Step([string] $name, [scriptblock] $body) {
	if ($env:GITHUB_ACTIONS) { Write-Host "::group::$name" } else { Write-Host "`n=== $name" -ForegroundColor Cyan }
	$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
	$status = 'passed'
	try {
		& $body
	}
	catch {
		$status = 'FAILED'
		throw
	}
	finally {
		$stopwatch.Stop()
		$timings.Add([pscustomobject]@{ Step = $name; Result = $status; Seconds = [math]::Round($stopwatch.Elapsed.TotalSeconds, 1) })
		if ($env:GITHUB_ACTIONS) { Write-Host '::endgroup::' }
	}
}

function Invoke-Skipped([string] $name, [string] $reason) {
	Write-Host "`n=== $name - SKIPPED: $reason" -ForegroundColor Yellow
	if ($env:GITHUB_ACTIONS) { Write-Host "::warning::$name skipped: $reason" }
	$timings.Add([pscustomobject]@{ Step = $name; Result = 'skipped'; Seconds = 0 })
}

# Runs dotnet, tees its output to a log, and fails on a non-zero exit. -warnaserror already fails the publish on any
# trim or AOT warning; the log check is a second gate in case one is ever logged without failing the command.
function Invoke-Dotnet([string] $logName, [string[]] $arguments) {
	$log = Join-Path $logs "$logName.log"
	Write-Host "dotnet $($arguments -join ' ')" -ForegroundColor DarkGray
	& dotnet @arguments 2>&1 | Tee-Object -FilePath $log | Out-Host
	if ($LASTEXITCODE -ne 0) { throw "dotnet $($arguments[0]) failed with exit code $LASTEXITCODE (log: $log)." }

	$warnings = Select-String -Path $log -Pattern 'warning IL[23][0-9]{3}' | Select-Object -First 5
	if ($warnings) {
		throw "dotnet $($arguments[0]) reported trim or AOT warnings:`n$($warnings.Line -join "`n")"
	}
}

function Assert-NoLeaks([string[]] $directories) {
	$global:LASTEXITCODE = 0
	& $leakScan -Path $directories
	if ($LASTEXITCODE -ne 0) { throw "The compiler-host leak scan failed for: $($directories -join ', ')." }
	$scanned.AddRange($directories)
}

function Assert-Contains([string] $directory, [string] $pattern, [string] $what) {
	$found = @(Get-ChildItem $directory -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -match $pattern })
	if ($found.Count -eq 0) { throw "$what is missing from '$directory' (no file matches '$pattern')." }
	Write-Host "  found $what ($($found[0].Name))"
}

# The generated telemetry, per assembly: the implementation type the generator writes for each interface, and the
# members a consumer reaches through that interface. The constructor and the meter initialisation are reached through
# dependency injection.
$generatedMembers = [ordered]@{
	PackedTelemetryWasm = [ordered]@{
		'PackedTelemetry.OrderTelemetryCore' = @(
			'.ctor', 'InitializeMeters', 'PlacingOrder', 'OrderValidated', 'OrderTotal', 'OrdersInFlight', 'OrderShipped',
			'OrderFailed', '_activitySource', '_logger', '_meter'
		)
	}
	'PackedTelemetry.Library' = [ordered]@{
		'PackedTelemetry.Library.InventoryTelemetryCore' = @('.ctor', 'ReservingStock', 'StockReserved', '_activitySource', '_meter')
	}
}

function Open-Metadata([string] $path) {
	$stream = [System.IO.File]::OpenRead($path)
	$reader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
	return [pscustomobject]@{
		Stream = $stream
		Reader = $reader
		Metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($reader)
	}
}

function Close-Metadata($opened) {
	$opened.Reader.Dispose()
	$opened.Stream.Dispose()
}

# Type full name -> member names (methods and fields), and method definition token -> "Type::Method".
function Read-Members([string] $path) {
	$opened = Open-Metadata $path
	try {
		$metadata = $opened.Metadata
		$types = @{}
		$methods = @{}
		foreach ($handle in $metadata.TypeDefinitions) {
			$type = $metadata.GetTypeDefinition($handle)
			$namespace = $metadata.GetString($type.Namespace)
			$name = ($namespace ? "$namespace." : '') + $metadata.GetString($type.Name)
			$members = [System.Collections.Generic.HashSet[string]]::new()
			foreach ($method in $type.GetMethods()) {
				$methodName = $metadata.GetString($metadata.GetMethodDefinition($method).Name)
				[void] $members.Add($methodName)
				$token = 0x06000000 -bor [System.Reflection.Metadata.Ecma335.MetadataTokens]::GetRowNumber(
					[System.Reflection.Metadata.MethodDefinitionHandle] $method
				)
				$methods[$token] = "${name}::$methodName"
			}
			foreach ($field in $type.GetFields()) { [void] $members.Add($metadata.GetString($metadata.GetFieldDefinition($field).Name)) }
			$types[$name] = $members
		}

		return [pscustomobject]@{ Types = $types; Methods = $methods }
	}
	finally {
		Close-Metadata $opened
	}
}

# ILLink writes the assemblies it trimmed to obj/<configuration>/<tfm>/linked. A generated member missing there was
# trimmed away (or never generated), and the app would fail where it calls it.
function Assert-GeneratedMembersSurviveTrimming([string] $projectDirectory) {
	$linked = Get-ChildItem (Join-Path $projectDirectory 'obj') -Recurse -Directory -Filter 'linked' | Select-Object -First 1
	if (-not $linked) { throw "ILLink wrote no linked assemblies under '$projectDirectory/obj': the publish did not trim." }

	foreach ($assembly in $generatedMembers.Keys) {
		$path = Join-Path $linked.FullName "$assembly.dll"
		if (-not (Test-Path $path)) { throw "ILLink did not write $assembly.dll to '$($linked.FullName)'." }

		$shape = Read-Members $path
		foreach ($type in $generatedMembers[$assembly].Keys) {
			if (-not $shape.Types.ContainsKey($type)) { throw "The trimmed $assembly.dll does not define the generated $type." }
			$missing = @($generatedMembers[$assembly][$type] | Where-Object { -not $shape.Types[$type].Contains($_) })
			if ($missing.Count -gt 0) {
				throw "Trimming removed generated members of $type from ${assembly}.dll: $($missing -join ', ')."
			}
			Write-Host "  trimmed $assembly.dll keeps $type ($($generatedMembers[$assembly][$type].Count) members)"
		}
	}
}

# The Mono AOT compiler records, per assembly, the method tokens it compiled to WebAssembly. Every generated telemetry
# method must be among them: a method that is not was left to the interpreter, or does not exist.
function Assert-GeneratedMethodsAotCompiled([string] $projectDirectory) {
	foreach ($assembly in $generatedMembers.Keys) {
		$tokensFile = Get-ChildItem (Join-Path $projectDirectory 'obj') -Recurse -File `
			-Filter "$($assembly.Replace('.', '_'))_dll_compiled_methods.txt" | Select-Object -First 1
		if (-not $tokensFile) { throw "The WebAssembly AOT compiler recorded no compiled methods for $assembly.dll." }

		$lines = @(Get-Content $tokensFile.FullName)
		$aotInput = $lines[0].Trim()
		if (-not (Test-Path $aotInput)) { throw "The AOT compiler's input assembly '$aotInput' does not exist." }
		$shape = Read-Members $aotInput
		$compiled = [System.Collections.Generic.HashSet[string]]::new()
		foreach ($line in $lines | Select-Object -Skip 2) {
			if (-not $line.Trim()) { continue }
			$token = [System.Convert]::ToInt32($line.Trim(), 16)
			if ($shape.Methods.ContainsKey($token)) { [void] $compiled.Add($shape.Methods[$token]) }
		}

		foreach ($type in $generatedMembers[$assembly].Keys) {
			$methods = @($generatedMembers[$assembly][$type] | Where-Object { -not $_.StartsWith('_') })
			$missing = @($methods | Where-Object { -not $compiled.Contains("${type}::$_") })
			if ($missing.Count -gt 0) {
				throw "The WebAssembly AOT compiler did not compile these generated $type methods: $($missing -join ', ')."
			}
			Write-Host "  AOT-compiled every generated $type method ($($methods.Count)) in $assembly.dll"
		}
	}
}

function New-NuGetConfig([string] $path, [hashtable[]] $sources) {
	$sourceXml = ($sources | ForEach-Object { "`t`t<add key=`"$($_.Key)`" value=`"$($_.Value)`" />" }) -join "`n"
	$mappingXml = ($sources | ForEach-Object {
			$patterns = ($_.Patterns | ForEach-Object { "`t`t`t<package pattern=`"$_`" />" }) -join "`n"
			"`t`t<packageSource key=`"$($_.Key)`">`n$patterns`n`t`t</packageSource>"
		}) -join "`n"

	@"
<?xml version="1.0" encoding="utf-8"?>
<!-- Generated by scripts/test-packed-consumers.ps1. Used as RestoreConfigFile, so no other configuration applies. -->
<configuration>
	<packageSources>
		<clear />
$sourceXml
	</packageSources>
	<fallbackPackageFolders>
		<clear />
	</fallbackPackageFolders>
	<packageSourceMapping>
$mappingXml
	</packageSourceMapping>
</configuration>
"@ | Set-Content $path -Encoding utf8
}

$exitCode = 0
$totalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
	New-Item -ItemType Directory -Path $feed, $packages, $logs, $consumers, $output -Force | Out-Null

	Write-Host "Packed-consumer gate: Purview.Telemetry.SourceGenerator $version ($Configuration), RID $RuntimeIdentifier" -ForegroundColor Cyan
	Write-Host "Workspace: $workspace"

	# Guards at the workspace root: MSBuild stops at the first Directory.Build.props/.targets and
	# Directory.Packages.props it finds walking up, so nothing above the workspace - a stray file in the temp folder,
	# a user profile - can change how the consumers build. The consumers must see the package and nothing else.
	foreach ($guard in 'Directory.Build.props', 'Directory.Build.targets') {
		@"
<Project>
	<!-- Guard written by scripts/test-packed-consumers.ps1: stops $guard inheritance from any parent folder. -->
</Project>
"@ | Set-Content (Join-Path $workspace $guard) -Encoding utf8
	}

	@'
<Project>
	<!-- Guard written by scripts/test-packed-consumers.ps1: the consumers version their packages themselves. -->
	<PropertyGroup>
		<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
	</PropertyGroup>
</Project>
'@ | Set-Content (Join-Path $workspace 'Directory.Packages.props') -Encoding utf8

	# The repository's SDK, without its MSBuild SDKs (Purview.BuildSdk): the consumers are plain projects.
	[ordered]@{ sdk = [ordered]@{ version = $sdk.version; rollForward = $sdk.rollForward; allowPrerelease = $true } } |
		ConvertTo-Json | Set-Content (Join-Path $workspace 'global.json') -Encoding utf8

	# Pack-time sources: nuget.org for everything, plus the optional feed for the prerelease build dependencies only.
	$prereleasePatterns = @('Purview.SourceGeneratorFramework', 'Purview.SourceGeneratorFramework.*', 'Purview.BuildSdk')
	$packSources = @(@{ Key = 'nuget.org'; Value = 'https://api.nuget.org/v3/index.json'; Patterns = @('*') + $prereleasePatterns })
	if ($PrereleaseFeed) {
		$packSources += @{ Key = 'prerelease'; Value = $PrereleaseFeed; Patterns = $prereleasePatterns }
	}
	$packConfig = Join-Path $workspace 'pack.nuget.config'
	New-NuGetConfig $packConfig $packSources

	# Consumer sources: Purview.Telemetry.SourceGenerator from the package just packed and nowhere else - not nuget.org,
	# not a local feed holding an older build of the same version.
	$consumerConfig = Join-Path $consumers 'nuget.config'
	New-NuGetConfig $consumerConfig @(
		@{ Key = 'packed'; Value = $feed; Patterns = @('Purview.Telemetry.SourceGenerator') }
		@{ Key = 'nuget.org'; Value = 'https://api.nuget.org/v3/index.json'; Patterns = @('*') }
	)

	# An isolated extraction directory: NuGet reuses an already-extracted package of the same id and version, so a
	# stale copy from an earlier run - or an earlier SourceGeneratorFramework build - would be used instead.
	$env:NUGET_PACKAGES = $packages

	Invoke-Step "Preflight: restore Purview.SourceGeneratorFramework $sgfVersion and Purview.BuildSdk $buildSdkVersion" {
		$probe = Join-Path $workspace 'probe'
		New-Item -ItemType Directory -Path $probe -Force | Out-Null
		@"
<Project Sdk="Microsoft.NET.Sdk">
	<PropertyGroup>
		<TargetFramework>net10.0</TargetFramework>
	</PropertyGroup>
	<ItemGroup>
		<PackageDownload Include="Purview.SourceGeneratorFramework" Version="[$sgfVersion]" />
		<PackageDownload Include="Purview.BuildSdk" Version="[$buildSdkVersion]" />
	</ItemGroup>
</Project>
"@ | Set-Content (Join-Path $probe 'probe.csproj') -Encoding utf8

		& dotnet restore (Join-Path $probe 'probe.csproj') "-p:RestoreConfigFile=$packConfig" 2>&1 |
			Tee-Object -FilePath (Join-Path $logs 'probe.log') | Out-Host
		if ($LASTEXITCODE -ne 0) {
			$from = ($packSources | ForEach-Object Value) -join ', '
			throw (
				"Purview.SourceGeneratorFramework $sgfVersion (Directory.Packages.props) or Purview.BuildSdk $buildSdkVersion " +
				"(global.json) could not be restored from $from. The generator is built with both, so nothing can be " +
				'packed. If a version is not on nuget.org yet, publish it there, or point this gate at a feed that has ' +
				'it: -PrereleaseFeed <folder or URL>, or set TELEMETRY_PRERELEASE_FEED (LOCAL_NUGET_FEED_PATH is used ' +
				'when set). Purview.BuildSdk is resolved by MSBuild through the normal NuGet configuration, so that feed ' +
				'must also be a configured source for the pack itself.'
			)
		}
	}

	if ($PreflightOnly) {
		Write-Host 'The prerelease build dependencies restore; stopping after the preflight (-PreflightOnly).' -ForegroundColor Green
		return
	}

	if ($IsLinux -and -not (Get-Command clang -ErrorAction SilentlyContinue)) {
		throw 'Native AOT on Linux needs clang and the zlib headers: sudo apt-get install -y clang zlib1g-dev.'
	}

	Invoke-Step 'Pack the shipping package to the isolated feed' {
		$script:repositoryRestoreTouched = $true
		foreach ($project in $packInputs) {
			Invoke-Dotnet "restore-$([System.IO.Path]::GetFileNameWithoutExtension($project))" @(
				'restore', $project, "-p:RestoreConfigFile=$packConfig", '-nr:false'
			)
		}

		foreach ($package in $shipping) {
			Invoke-Dotnet "pack-$($package.Id)" @(
				'pack', $package.Project, '--configuration', $Configuration, '--output', $feed, '--no-restore',
				'-m:2', '-nr:false'
			)
		}

		$expected = @($shipping.Id | ForEach-Object { "$_.$version.nupkg".ToLowerInvariant() } | Sort-Object)
		$actual = @(Get-ChildItem $feed -Filter '*.nupkg' | ForEach-Object { $_.Name.ToLowerInvariant() } | Sort-Object)
		$difference = Compare-Object $expected $actual
		if ($difference) {
			throw "The feed does not hold exactly the shipping packages:`n$($difference | Out-String)"
		}

		# The bytes the consumers restore: a compiler-host assembly anywhere but analyzers/, or a generator assembly
		# under lib/, ref/ or runtimes/, would reach every application that references the package.
		Add-Type -AssemblyName System.IO.Compression.FileSystem
		$hostOnly = '(^|/)(Purview\.SourceGeneratorFramework|Microsoft\.CodeAnalysis)[^/]*$'
		$generatorOnly = '^(lib|ref|runtimes)/.*Purview\.Telemetry\.SourceGenerator[^/]*$'
		foreach ($nupkg in Get-ChildItem $feed -Filter '*.nupkg') {
			$archive = [System.IO.Compression.ZipFile]::OpenRead($nupkg.FullName)
			try {
				$entries = @($archive.Entries.FullName)
			}
			finally { $archive.Dispose() }

			$bad = @($entries | Where-Object { ($_ -match $hostOnly -and $_ -notmatch '^analyzers/') -or $_ -match $generatorOnly })
			if ($bad.Count -gt 0) { throw "$($nupkg.Name) ships compiler-host content outside analyzers/: $($bad -join ', ')." }
			$looseFramework = @($entries | Where-Object { $_ -match '^analyzers/.*Purview\.SourceGeneratorFramework[^/]*$' })
			if ($looseFramework.Count -gt 0) {
				throw "$($nupkg.Name) ships a loose framework assembly under analyzers/ (the IL merge did not run): $($looseFramework -join ', ')."
			}
			Write-Host "  $($nupkg.Name): no compiler-host content outside analyzers/, and the generator is IL-merged"
		}

		if ($PackOutput) {
			New-Item -ItemType Directory -Path $PackOutput -Force | Out-Null
			Copy-Item (Join-Path $feed '*') $PackOutput -Force
			Write-Host "  copied the package to $PackOutput"
		}
	}

	Copy-Item (Join-Path $consumerTemplates '*') $consumers -Recurse -Force
	$consumerProperties = @(
		"-p:TelemetryPackageVersion=$version"
		"-p:MicrosoftExtensionsVersion=$extensionsVersion"
		"-p:MicrosoftExtensionsTelemetryVersion=$extensionsTelemetryVersion"
		"-p:AspNetCorePackageVersion=$aspNetCoreVersion"
		"-p:RestoreConfigFile=$consumerConfig"
		'-nr:false'
	)
	$libraryBin = Join-Path $consumers 'Library/bin'

	Invoke-Step '(a) Native AOT console: publish and run' {
		$project = Join-Path $consumers 'AotConsole/AotConsole.csproj'
		$publish = Join-Path $output 'aot-console'

		# A framework-dependent build first: its bin holds the deps.json and runtimeconfig.json that list every runtime
		# dependency, which is where an analyzer-only assembly that leaks shows up first.
		Invoke-Dotnet 'aot-console-build' (@('build', $project, '--configuration', 'Release', '-warnaserror', '-m:2') + $consumerProperties)
		Assert-Contains (Join-Path $consumers 'AotConsole/bin/Release/net11.0') '^PackedTelemetryAot\.deps\.json$' 'the build deps.json'

		Invoke-Dotnet 'aot-console-publish' (@(
				'publish', $project, '--configuration', 'Release', '--runtime', $RuntimeIdentifier, '--output', $publish,
				'-warnaserror'
			) + $consumerProperties)

		& (Join-Path $publish "PackedTelemetryAot$exe") | Out-Host
		if ($LASTEXITCODE -ne 0) { throw "The Native AOT console exited with ${LASTEXITCODE}: the generated telemetry is wrong." }

		Assert-NoLeaks @((Join-Path $consumers 'AotConsole/bin'), $libraryBin, $publish)
	}

	function Assert-WasmOutput([string] $publish) {
		$framework = Join-Path $publish 'wwwroot/_framework'
		Assert-Contains $framework '^PackedTelemetryWasm\.[a-z0-9]+\.wasm$' 'the app assembly'
		Assert-Contains $framework '^PackedTelemetry\.Library\.[a-z0-9]+\.wasm$' 'the library assembly'
	}

	Invoke-Step '(b) Blazor WebAssembly: trimmed publish' {
		$projectDirectory = Join-Path $consumers 'BlazorWasm'
		$publish = Join-Path $output 'wasm-trimmed'
		Invoke-Dotnet 'wasm-trimmed-publish' (@(
				'publish', (Join-Path $projectDirectory 'BlazorWasm.csproj'), '--configuration', 'Release', '--output', $publish,
				'-warnaserror'
			) + $consumerProperties)

		Assert-WasmOutput $publish
		Assert-GeneratedMembersSurviveTrimming $projectDirectory
		Assert-NoLeaks @((Join-Path $projectDirectory 'bin'), $libraryBin, $publish)
	}

	$wasmTools = $false
	if ($WasmAot -ne 'Skip') {
		Push-Location $workspace
		try { $wasmTools = [bool] (& dotnet workload list 2>$null | Select-String -Pattern '^\s*wasm-tools\s') }
		finally { Pop-Location }
	}

	if ($WasmAot -eq 'Skip') {
		Invoke-Skipped '(b) Blazor WebAssembly: RunAOTCompilation publish' 'disabled with -WasmAot Skip.'
	}
	elseif (-not $wasmTools -and $WasmAot -eq 'Auto') {
		Invoke-Skipped '(b) Blazor WebAssembly: RunAOTCompilation publish' (
			'the wasm-tools workload is not installed, so the WebAssembly AOT compile was NOT checked. ' +
			'Install it with `dotnet workload install wasm-tools` and rerun, or pass -WasmAot Require to make this an error.'
		)
	}
	else {
		Invoke-Step '(b) Blazor WebAssembly: RunAOTCompilation publish' {
			if (-not $wasmTools) {
				throw 'The wasm-tools workload is not installed (-WasmAot Require): run `dotnet workload install wasm-tools`.'
			}

			# A copy of the project, so the AOT build starts from clean obj/bin rather than the trimmed build's.
			$projectDirectory = Join-Path $consumers 'BlazorWasmAot'
			Copy-Item (Join-Path $consumerTemplates 'BlazorWasm') $projectDirectory -Recurse -Force
			$publish = Join-Path $output 'wasm-aot'
			Invoke-Dotnet 'wasm-aot-publish' (@(
					'publish', (Join-Path $projectDirectory 'BlazorWasm.csproj'), '--configuration', 'Release',
					'--output', $publish, '-p:RunAOTCompilation=true', '-warnaserror'
				) + $consumerProperties)

			Assert-WasmOutput $publish
			Assert-Contains (Join-Path $publish 'wwwroot/_framework') '^dotnet\.native\.[a-z0-9]+\.wasm$' 'the AOT-compiled runtime'
			Assert-GeneratedMembersSurviveTrimming $projectDirectory
			Assert-GeneratedMethodsAotCompiled $projectDirectory
			Assert-NoLeaks @((Join-Path $projectDirectory 'bin'), $libraryBin, $publish)
		}
	}

	Invoke-Step '(c) Blazor Server: publish, render the page on a TestServer and check its telemetry' {
		$project = Join-Path $consumers 'BlazorServer/BlazorServer.csproj'
		$publish = Join-Path $output 'blazor-server'
		Invoke-Dotnet 'blazor-server-publish' (@(
				'publish', $project, '--configuration', 'Release', '--output', $publish, '-warnaserror'
			) + $consumerProperties)

		Push-Location $publish
		try {
			& dotnet (Join-Path $publish 'PackedTelemetryServer.dll') --self-test | Out-Host
			if ($LASTEXITCODE -ne 0) { throw "The Blazor Server self-test exited with ${LASTEXITCODE}: the page or its telemetry is wrong." }
		}
		finally {
			Pop-Location
		}

		Assert-NoLeaks @((Join-Path $consumers 'BlazorServer/bin'), $libraryBin, $publish)
	}
}
catch {
	$exitCode = 1
	Write-Failure "Packed-consumer gate failed: $($_.Exception.Message)"
	if (Test-Path $logs) { Write-Host "Logs: $logs" }
}
finally {
	if ($null -eq $previousNuGetPackages) { Remove-Item Env:\NUGET_PACKAGES -ErrorAction SilentlyContinue }
	else { $env:NUGET_PACKAGES = $previousNuGetPackages }

	# Packing restored the shipping projects against the isolated cache that is about to be deleted; restore them
	# against the normal cache, so a later --no-restore build does not point at a missing folder.
	if ($repositoryRestoreTouched) {
		Write-Host "`nRestoring the shipping projects against the normal package cache..." -ForegroundColor Cyan
		foreach ($project in $packInputs) {
			& dotnet restore $project -nr:false | Out-Null
			if ($LASTEXITCODE -ne 0) { Write-Failure "Restoring $project against the normal cache failed; run dotnet restore." }
		}
	}

	$totalStopwatch.Stop()
	Write-Host "`nPacked-consumer gate timings:" -ForegroundColor Cyan
	$timings | Format-Table -AutoSize | Out-Host
	Write-Host ("Total: {0:N1}s" -f $totalStopwatch.Elapsed.TotalSeconds)
	if ($scanned.Count -gt 0) { Write-Host "Leak-scanned $($scanned.Count) folder(s)." }

	if ($KeepArtifacts) {
		Write-Host "Artifacts kept at $workspace" -ForegroundColor Yellow
	}
	else {
		Remove-Item $workspace -Recurse -Force -ErrorAction SilentlyContinue
	}
}

if ($exitCode -ne 0) { exit $exitCode }

Write-Host 'Packed-consumer gate passed.' -ForegroundColor Green
