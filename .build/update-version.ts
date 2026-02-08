import * as fs from 'fs';

// Get version from package.json
const packageJson = JSON.parse(fs.readFileSync('package.json', 'utf8'));
const version: string = packageJson.version;

// Define regex patterns for updating versions
const regexPatterns: { pattern: RegExp; replacement: string }[] = [
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
];

// Define the list of files to update
const filesToUpdate: string[] = [
	'README.md',
	'.wiki/Home.md',
	'.wiki/Generated-Output.md',
	//'samples/SampleApp/SampleApp.Host/SampleApp.Host.csproj'
];

// Function to update version in specific files
function updateFilesVersion() {
	filesToUpdate.forEach((file) => {
		if (fs.existsSync(file)) {
			let content = fs.readFileSync(file, 'utf8');
			let updated = false;

			regexPatterns.forEach(({ pattern, replacement }) => {
				if (pattern.test(content)) {
					content = content.replace(pattern, replacement);
					updated = true;
				}
			});

			if (updated) {
				fs.writeFileSync(file, content, 'utf8');
				console.log(`✅ Updated version in: ${file}`);
			} else {
				console.log(`ℹ️ No matching version string found in: ${file}`);
			}
		} else {
			console.log(`⚠️ File not found: ${file}`);
		}
	});
}

// Run the update
updateFilesVersion();
