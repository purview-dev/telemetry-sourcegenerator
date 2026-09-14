import * as fs from 'fs';

// Get version from package.json
const packageJson = JSON.parse(fs.readFileSync('package.json', 'utf8'));
const version: string = packageJson.version;

// Define regex patterns for updating versions
const regexPatterns: { pattern: RegExp; replacement: string }[] = [
	{
		// Match **Current Version:** 3.0.0
		pattern:
			/\*\*Current Version:\*\* ([\d]+\.[\d]+\.[\d]+(?:-[a-zA-Z0-9.]+)?(?:\+[a-zA-Z0-9.]+)?)/g,
		replacement: `**Current Version:** ${version}`,
	},
	{
		// Match Name="Purview.Telemetry.SourceGenerator" Version="3.0.0"
		pattern:
			/Include="Purview\.Telemetry\.SourceGenerator" Version="([\d]+\.[\d]+\.[\d]+(?:-[a-zA-Z0-9.]+)?(?:\+[a-zA-Z0-9.]+)?)"/g,
		replacement: `Include="Purview.Telemetry.SourceGenerator" Version="${version}"`,
	},
	{
		// Match "Purview.Telemetry.SourceGenerator", "3.0.0"
		pattern:
			/"Purview\.Telemetry\.SourceGenerator", "([\d]+\.[\d]+\.[\d]+(\.[\d])?(?:-[a-zA-Z0-9.]+)?(?:\+[a-zA-Z0-9.]+)?)"/g,
		replacement: `"Purview.Telemetry.SourceGenerator", "${version}"`,
	},
	{
		// Match dotnet add package Purview.Telemetry.SourceGenerator --version 3.0.0
		pattern:
			/Purview\.Telemetry\.SourceGenerator --version ([\d]+\.[\d]+\.[\d]+(?:-[a-zA-Z0-9.]+)?(?:\+[a-zA-Z0-9.]+)?)/g,
		replacement: `Purview.Telemetry.SourceGenerator --version ${version}`,
	},
	{
		// Match Install-Package Purview.Telemetry.SourceGenerator -Version 3.0.0
		pattern:
			/Purview\.Telemetry\.SourceGenerator -Version ([\d]+\.[\d]+\.[\d]+(?:-[a-zA-Z0-9.]+)?(?:\+[a-zA-Z0-9.]+)?)/g,
		replacement: `Purview.Telemetry.SourceGenerator -Version ${version}`,
	},
];

// Define the list of files to update
const filesToUpdate: string[] = ['README.md', 'AGENTS.md', 'docs/release-process.md'];

// Function to update version in specific files
function updateFilesVersion(): boolean {
	let anyUpdated = false;

	filesToUpdate.forEach((file) => {
		if (fs.existsSync(file)) {
			const originalContent = fs.readFileSync(file, 'utf8');
			let content = originalContent;

			regexPatterns.forEach(({ pattern, replacement }) => {
				// Reset lastIndex since patterns use the /g flag
				pattern.lastIndex = 0;
				content = content.replace(pattern, replacement);
			});

			if (content !== originalContent) {
				fs.writeFileSync(file, content, 'utf8');
				console.log(`✅ Updated version in: ${file}`);
				anyUpdated = true;
			} else if (regexPatterns.some(({ pattern }) => { pattern.lastIndex = 0; return pattern.test(originalContent); })) {
				console.log(`ℹ️ Version already up to date in: ${file}`);
			} else {
				console.log(`ℹ️ No matching version string found in: ${file}`);
			}
		} else {
			console.log(`⚠️ File not found: ${file}`);
		}
	});

	return anyUpdated;
}

updateFilesVersion();