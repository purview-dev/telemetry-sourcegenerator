import * as fs from 'fs';
import { execSync } from 'child_process';

// Get version from package.json
const packageJson = JSON.parse(fs.readFileSync('package.json', 'utf8'));
const version: string = packageJson.version;

const WIKI_DIR = '.wiki';

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
	`${WIKI_DIR}/Home.md`,
	`${WIKI_DIR}/Generated-Output.md`,
	//'samples/SampleApp/SampleApp.Host/SampleApp.Host.csproj'
];

function run(cmd: string, cwd?: string): string {
	return execSync(cmd, { cwd, encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe'] }).trim();
}

// Pull the wiki submodule so we have the latest content to update
function getWikiBranch(): string {
	// Detect the remote's default branch (master for most wikis)
	try {
		const ref = run('git symbolic-ref refs/remotes/origin/HEAD', WIKI_DIR);
		// e.g. "refs/remotes/origin/master" -> "master"
		return ref.replace('refs/remotes/origin/', '');
	} catch {
		return 'master';
	}
}

function pullWiki() {
	console.log(`🔄 Pulling wiki submodule (${WIKI_DIR})...`);

	const isSubmoduleInit = fs.existsSync(`${WIKI_DIR}/.git`);

	if (!isSubmoduleInit) {
		console.log('   Initializing submodule...');
		run(`git submodule update --init ${WIKI_DIR}`);
	}

	// After submodule init/update the repo is in detached HEAD state.
	// Fetch and switch to the tracking branch before pulling.
	run('git fetch origin', WIKI_DIR);
	const branch = getWikiBranch();
	const currentBranch = (() => {
		try { return run('git symbolic-ref --short HEAD', WIKI_DIR); } catch { return ''; }
	})();
	if (currentBranch !== branch) {
		console.log(`   Checking out branch '${branch}'...`);
		run(`git checkout ${branch}`, WIKI_DIR);
	}
	// Reset to remote state rather than ff-only pull, so diverged local branches
	// don't block the release. The script is about to overwrite these files anyway.
	run(`git reset --hard origin/${branch}`, WIKI_DIR);

	console.log(`✅ Wiki is up to date.`);
}

// Function to update version in specific files
function updateFilesVersion(): boolean {
	let anyUpdated = false;

	filesToUpdate.forEach((file) => {
		if (fs.existsSync(file)) {
			let content = fs.readFileSync(file, 'utf8');
			let updated = false;

			regexPatterns.forEach(({ pattern, replacement }) => {
				// Reset lastIndex since patterns use the /g flag
				pattern.lastIndex = 0;
				if (pattern.test(content)) {
					pattern.lastIndex = 0;
					content = content.replace(pattern, replacement);
					updated = true;
				}
			});

			if (updated) {
				fs.writeFileSync(file, content, 'utf8');
				console.log(`✅ Updated version in: ${file}`);
				anyUpdated = true;
			} else {
				console.log(`ℹ️ No matching version string found in: ${file}`);
			}
		} else {
			console.log(`⚠️ File not found: ${file}`);
		}
	});

	return anyUpdated;
}

// Commit and push any changes inside the wiki submodule
function pushWiki() {
	// Check whether there is anything to commit in the wiki submodule
	const status = run('git status --porcelain', WIKI_DIR);
	if (!status) {
		console.log(`ℹ️ No wiki changes to commit.`);
		return;
	}

	console.log(`🚀 Committing and pushing wiki changes...`);
	run('git add .', WIKI_DIR);
	run(`git commit -m "chore: bump version to ${version}"`, WIKI_DIR);
	run('git push', WIKI_DIR);
	console.log(`✅ Wiki pushed successfully.`);
}

// --- Main ---
pullWiki();
updateFilesVersion();
pushWiki();
