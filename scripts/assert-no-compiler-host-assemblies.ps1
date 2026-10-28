#!/usr/bin/env pwsh
<#
.SYNOPSIS
	Fails when a build or publish output carries a compiler-host-only assembly.

.DESCRIPTION
	The telemetry source generator, its refactorings and the framework they are built on run inside the compiler. None
	of them may reach an application: Purview.SourceGeneratorFramework.dll once leaked into app build and publish output
	through analyzer-only references (Purview.SourceGeneratorFramework 1.2.1 put it in every sample's bin here), and
	Blazor WebAssembly AOT compilation fails on it. A build stays green while it leaks, because nothing looks at what is
	actually written to disk.

	This script looks. Every file under each -Path is checked by name, and every dependency manifest
	(*.deps.json, *.runtimeconfig.json, blazor.boot.json, dotnet.boot.js, and dotnet.js, which embeds the boot manifest since
	.NET 10 - including fingerprinted and Brotli/gzip compressed copies) is checked by content. A file, or a manifest entry, naming any of these fails the scan:

		Purview.SourceGeneratorFramework*
		Microsoft.CodeAnalysis*
		Purview.Telemetry.SourceGenerator*   (the generator, and Purview.Telemetry.SourceGenerator.Refactorings)

	A path that does not exist, or that holds no files, fails too: a scan of nothing proves nothing.

	Used by scripts/test-packed-consumers.ps1 on every consumer bin and publish folder, by the samples CI job on the
	sample build output, and by `just scan-compiler-host-leaks`.

.PARAMETER Path
	One or more build or publish output directories to scan, recursively. Space-separated when run with -File.

.PARAMETER Allow
	Wildcard patterns (PowerShell -like, matched against each file's path relative to the scanned directory, with
	forward slashes) for third-party tools that legitimately carry Roslyn and are never loaded by the application -
	for example '*BlazorDebugProxy/*', the WebAssembly debugging proxy Microsoft.AspNetCore.Components.WebAssembly.Server
	publishes beside a Blazor server. Allowed files are counted and reported, never silently ignored. Keep each pattern
	as narrow as the tool's own folder.

.EXAMPLE
	./scripts/assert-no-compiler-host-assemblies.ps1 -Path samples/SampleApp/SampleApp.Web/bin

.EXAMPLE
	./scripts/assert-no-compiler-host-assemblies.ps1 -Path artifacts/blazor-host -Allow '*BlazorDebugProxy/*'

.EXAMPLE
	pwsh -NoProfile -File scripts/assert-no-compiler-host-assemblies.ps1 -Path artifacts/aot-console bin/Release
#>
[CmdletBinding()]
param (
	[Parameter(Mandatory, Position = 0)]
	[string[]] $Path,

	[string[]] $Allow = @(),

	# Collects the rest of `pwsh -File ... -Path a b` (space-separated): -File passes arguments as strings and does
	# not parse PowerShell's comma array syntax.
	[Parameter(ValueFromRemainingArguments, DontShow)]
	[string[]] $AdditionalPath = @()
)

$ErrorActionPreference = 'Stop'
$Path = @($Path) + @($AdditionalPath)

# File-name prefixes. Published WebAssembly files carry a fingerprint (Name.abc123.wasm), which a prefix still matches.
$forbiddenNames = @(
	'Purview.SourceGeneratorFramework'
	'Microsoft.CodeAnalysis'
	'Purview.Telemetry.SourceGenerator'
)

$forbiddenContent = [regex]::new(
	(($forbiddenNames | ForEach-Object { [regex]::Escape($_) }) -join '|'),
	[System.Text.RegularExpressions.RegexOptions]::IgnoreCase
)

# Dependency manifests, optionally fingerprinted (dotnet.boot.abc123.js) and optionally compressed (.br, .gz). Since .NET 10
# the WebAssembly boot manifest is embedded in _framework/dotnet.js, so that file is a manifest too; dotnet.native.*.js and
# dotnet.runtime.*.js are the runtime itself and never list assemblies.
$manifestPattern = [regex]::new(
	'(\.deps\.json|\.runtimeconfig\.json|^blazor\.boot(\.[a-z0-9]+)?\.json|^dotnet(\.boot)?(\.[a-z0-9]+)?\.js)(\.br|\.gz)?$',
	[System.Text.RegularExpressions.RegexOptions]::IgnoreCase
)

function Read-ManifestText([System.IO.FileInfo] $file) {
	$stream = [System.IO.File]::OpenRead($file.FullName)
	try {
		$source = switch -Regex ($file.Name) {
			'\.br$' { [System.IO.Compression.BrotliStream]::new($stream, [System.IO.Compression.CompressionMode]::Decompress) }
			'\.gz$' { [System.IO.Compression.GZipStream]::new($stream, [System.IO.Compression.CompressionMode]::Decompress) }
			default { $stream }
		}

		$reader = [System.IO.StreamReader]::new($source)
		try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
	}
	finally {
		$stream.Dispose()
	}
}

function Write-Failure([string] $message) {
	if ($env:GITHUB_ACTIONS) { Write-Host "::error::$message" } else { Write-Host "ERROR: $message" -ForegroundColor Red }
}

$violations = [System.Collections.Generic.List[string]]::new()

foreach ($directory in $Path) {
	if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
		$violations.Add("'$directory' does not exist or is not a directory, so it cannot be scanned.")
		continue
	}

	$root = (Resolve-Path -LiteralPath $directory).Path
	$files = @(Get-ChildItem -LiteralPath $root -Recurse -File -Force)
	if ($files.Count -eq 0) {
		$violations.Add("'$root' holds no files, so the scan would prove nothing.")
		continue
	}

	$manifests = 0
	$allowed = 0
	foreach ($file in $files) {
		$relative = [System.IO.Path]::GetRelativePath($root, $file.FullName)
		$forwardSlashed = $relative.Replace('\', '/')
		if ($Allow | Where-Object { $forwardSlashed -like $_ }) {
			$allowed++
			continue
		}

		foreach ($name in $forbiddenNames) {
			if ($file.Name.StartsWith($name, [System.StringComparison]::OrdinalIgnoreCase)) {
				$violations.Add("compiler-host file '$relative' in '$root'.")
				break
			}
		}

		if ($manifestPattern.IsMatch($file.Name)) {
			$manifests++
			$matched = $forbiddenContent.Matches((Read-ManifestText $file)) |
				ForEach-Object Value |
				Sort-Object -Unique
			if ($matched) {
				$violations.Add("manifest '$relative' in '$root' names $($matched -join ', ').")
			}
		}
	}

	$allowedNote = $allowed -gt 0 ? (", {0} allowed by -Allow {1}" -f $allowed, ($Allow -join ', ')) : ''
	Write-Host ("Scanned {0}: {1} file(s), {2} manifest(s){3}." -f $root, $files.Count, $manifests, $allowedNote)
}

if ($violations.Count -gt 0) {
	foreach ($violation in $violations) { Write-Failure $violation }
	Write-Failure (
		"$($violations.Count) compiler-host leak(s) found. Source generators, analyzers and their dependencies run " +
		'inside the compiler and must never reach build or publish output; check analyzer-only references ' +
		'(OutputItemType="Analyzer", ReferenceOutputAssembly="false", PrivateAssets="all") and the package layout.'
	)
	exit 1
}

Write-Host 'No compiler-host assemblies or manifest references found.' -ForegroundColor Green
