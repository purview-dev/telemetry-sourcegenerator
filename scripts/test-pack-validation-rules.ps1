#!/usr/bin/env pwsh
<#
.SYNOPSIS
	Proves every PackValidation ForbiddenContent rule in .config/purview-build.json actually fails a package.

.DESCRIPTION
	A forbidden-content glob that matches nothing - a typo, a wrong folder, a "*" where "**" was meant - passes
	silently forever. This script proves each rule bites, with the real tool (purview-build, the version
	.config/dotnet-tools.json pins) and the real rules. Each run validates copies of the packed packages in a scratch
	repository that holds the repository's purview-build.json verbatim and an empty solution, so the pipeline's build is
	a no-op and only pack validation does any work. RequireExplicitContent is turned off in every run, so the
	explicit-content rule cannot be what fails one: only ForbiddenContent can.

	1. Control: every package, untouched. Must pass, or the harness itself is broken.
	2. Per package: each package on its own, with one file per rule injected - a concrete path the rule's glob should
	   match (lib/**/Microsoft.CodeAnalysis* becomes lib/probe/Microsoft.CodeAnalysis.dll). Must fail, reporting every
	   rule. This proves the rules apply to every package id.
	3. Per rule: Purview.Telemetry.SourceGenerator with only that rule's file injected. Must fail, reporting that rule.
	   This proves each rule is triggered by its own file rather than by another rule's.

	The packages in -PackagesFolder are never modified.

.PARAMETER PackagesFolder
	Folder holding the packed .nupkg (and any .snupkg) files of the current version. Defaults to artifacts/, where
	`just pipeline-pack-validate` and `just pack` write them. `just pipeline-local-release` moves them to the local feed,
	so pass that folder after it.

.PARAMETER KeepArtifacts
	Leave the scratch repositories in place for inspection.
#>
[CmdletBinding()]
param (
	[string] $PackagesFolder = (Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts'),
	[switch] $KeepArtifacts
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$configPath = Join-Path $repositoryRoot '.config/purview-build.json'
$version = (Get-Content (Join-Path $repositoryRoot 'package.json') -Raw | ConvertFrom-Json).version
$config = Get-Content $configPath -Raw | ConvertFrom-Json

$rules = @($config.PackValidation.ForbiddenContent.PSObject.Properties['*'].Value)
if ($rules.Count -eq 0) { throw "No ForbiddenContent rules for '*' in $configPath." }

$packages = @(Get-ChildItem $PackagesFolder -Filter "*.$version.nupkg" -File | Sort-Object Name)
if ($packages.Count -eq 0) {
	throw (
		"No $version packages in '$PackagesFolder'. Pack first (just pipeline-pack-validate), or pass -PackagesFolder: " +
		'just pipeline-local-release moves its packages to the local feed.'
	)
}

# A concrete entry each glob must match: "**" spans one made-up folder, "*" becomes an assembly extension.
$cases = foreach ($rule in $rules) {
	[pscustomobject]@{ Rule = $rule; Entry = ($rule -replace '\*\*', 'probe' -replace '\*', '.dll') }
}

$scratch = Join-Path ([System.IO.Path]::GetTempPath()) "tsg-pack-rules-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
$runs = 0

# Validates $nupkgs (and their .snupkg) with $entries injected into each, and returns the rules the tool reported.
function Invoke-Validation([string] $name, [System.IO.FileInfo[]] $nupkgs, [string[]] $entries) {
	$root = Join-Path $scratch $name
	$artifacts = Join-Path $root 'artifacts'
	New-Item -ItemType Directory -Path (Join-Path $root '.config'), $artifacts -Force | Out-Null
	Copy-Item $configPath (Join-Path $root '.config/purview-build.json')
	Copy-Item (Join-Path $repositoryRoot '.config/dotnet-tools.json') (Join-Path $root '.config/dotnet-tools.json')
	Copy-Item (Join-Path $repositoryRoot 'global.json'), (Join-Path $repositoryRoot 'package.json') $root
	'<Solution />' | Set-Content (Join-Path $root 'empty.slnx') -Encoding utf8
	& git -C $root init --quiet

	foreach ($nupkg in $nupkgs) {
		$copy = Join-Path $artifacts $nupkg.Name
		Copy-Item $nupkg.FullName $copy
		$symbols = [System.IO.Path]::ChangeExtension($nupkg.FullName, '.snupkg')
		if (Test-Path $symbols) { Copy-Item $symbols $artifacts }

		if ($entries) {
			$archive = [System.IO.Compression.ZipFile]::Open($copy, [System.IO.Compression.ZipArchiveMode]::Update)
			try {
				foreach ($entry in $entries) {
					$writer = [System.IO.StreamWriter]::new($archive.CreateEntry($entry).Open())
					try { $writer.Write('injected by scripts/test-pack-validation-rules.ps1') } finally { $writer.Dispose() }
				}
			}
			finally { $archive.Dispose() }
		}
	}

	Push-Location $root
	try {
		& dotnet tool restore | Out-Null
		$output = & dotnet purview-build `
			--Build:Solution=empty.slnx `
			--Build:ArtifactsFolder=artifacts `
			--Build:CleanArtifacts=false `
			--Build:RunPack=false `
			--Build:ValidatePack=true `
			--Build:RunTests=false `
			--Build:RunLint=false `
			--PackValidation:RequireExplicitContent=false `
			--Release:Mode=None 2>&1 | ForEach-Object { "$_" }
		$exitCode = $LASTEXITCODE
	}
	finally {
		Pop-Location
	}

	$script:runs++
	# The console wraps long lines, so rejoin them before reading "Forbidden content '<rule>' must not be in ...".
	$text = ($output -join "`n") -replace '\r?\n\s*', ' '
	$reported = @([regex]::Matches($text, "Forbidden content '(?<rule>[^']+)'") | ForEach-Object { $_.Groups['rule'].Value } | Sort-Object -Unique)
	return [pscustomobject]@{ ExitCode = $exitCode; Reported = $reported; Output = ($output -join "`n") }
}

$failures = [System.Collections.Generic.List[string]]::new()
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
try {
	Write-Host "Control: $($packages.Count) untouched package(s)..." -ForegroundColor Cyan
	$control = Invoke-Validation 'control' $packages @()
	if ($control.ExitCode -ne 0) {
		Write-Host $control.Output
		throw 'The control run failed on untouched packages, so the harness cannot prove anything. Fix that first.'
	}
	Write-Host '  passed'

	Write-Host "Per package: each with all $($cases.Count) rule file(s) injected..." -ForegroundColor Cyan
	foreach ($package in $packages) {
		$result = Invoke-Validation "package-$($package.BaseName)" @($package) @($cases.Entry)
		$missed = @($cases.Rule | Where-Object { $result.Reported -notcontains $_ })
		if ($result.ExitCode -eq 0 -or $missed.Count -gt 0) {
			$failures.Add("$($package.Name): exit $($result.ExitCode); rules not reported: $($missed -join ', ')")
			Write-Host "  NOT REJECTED  $($package.Name) ($($missed.Count) rule(s) missed)" -ForegroundColor Red
		}
		else {
			Write-Host "  rejected      $($package.Name) ($($result.Reported.Count) of $($cases.Count) rules)"
		}
	}

	$core = $packages | Where-Object { $_.Name -ieq "Purview.Telemetry.SourceGenerator.$version.nupkg" }
	if (-not $core) { throw "Purview.Telemetry.SourceGenerator.$version.nupkg is not in '$PackagesFolder'." }

	Write-Host "Per rule: $($core.Name) with one rule's file injected..." -ForegroundColor Cyan
	$index = 0
	foreach ($case in $cases) {
		$index++
		$result = Invoke-Validation "rule-$index" @($core) @($case.Entry)
		$others = @($result.Reported | Where-Object { $_ -ne $case.Rule })
		if ($result.ExitCode -eq 0 -or $result.Reported -notcontains $case.Rule) {
			$failures.Add("$($case.Rule): injecting '$($case.Entry)' was not rejected by it (exit $($result.ExitCode)).")
			Write-Host ("  NOT REJECTED  {0,-55} {1}" -f $case.Entry, $case.Rule) -ForegroundColor Red
		}
		else {
			$also = $others.Count -gt 0 ? " (also matched: $($others -join ', '))" : ''
			Write-Host ("  rejected      {0,-55} {1}{2}" -f $case.Entry, $case.Rule, $also)
		}
	}
}
catch {
	$failures.Add($_.Exception.Message)
}
finally {
	if ($KeepArtifacts) { Write-Host "Scratch repositories kept at $scratch" -ForegroundColor Yellow }
	else { Remove-Item $scratch -Recurse -Force -ErrorAction SilentlyContinue }
}

Write-Host ("{0} validation run(s) in {1:N1}s." -f $runs, $stopwatch.Elapsed.TotalSeconds)
if ($failures.Count -gt 0) {
	foreach ($failure in $failures) {
		if ($env:GITHUB_ACTIONS) { Write-Host "::error::$failure" } else { Write-Host "ERROR: $failure" -ForegroundColor Red }
	}
	exit 1
}

Write-Host "Every ForbiddenContent rule ($($cases.Count)) rejected its file, in every package ($($packages.Count))." -ForegroundColor Green
