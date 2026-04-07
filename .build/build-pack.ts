import * as fs from 'fs';
import { execSync, spawnSync } from 'child_process';

const rootFolder = process.argv[2] ?? './src/';
const configuration = process.argv[3] ?? 'Release';
const artifactFolder = process.argv[4] ?? './artifacts/';

const packageJson = JSON.parse(fs.readFileSync('package.json', 'utf8'));
const version: string = packageJson.version;

function run(cmd: string): string {
	return execSync(cmd, { encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe'] }).trim();
}

const branch = run('git rev-parse --abbrev-ref HEAD');
const commit = run('git rev-parse HEAD');
const year = new Date().getFullYear();

const projectPath = `${rootFolder}Purview.Telemetry.SourceGenerator/Purview.Telemetry.SourceGenerator.csproj`;

const result = spawnSync(
	'dotnet',
	[
		'pack',
		projectPath,
		'--configuration', configuration,
		'--output', artifactFolder,
		'--include-symbols',
		`--property:Version=${version}`,
		`--property:RepositoryBranch=${branch}`,
		`--property:RepositoryCommit=${commit}`,
		`--property:COPYRIGHT_YEAR=${year}`,
	],
	{ stdio: 'inherit' },
);

process.exit(result.status ?? 1);
