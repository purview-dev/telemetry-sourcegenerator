// Bumps one part of the semantic version in package.json, for `just bump <major|minor|patch>`.
//
// The parts after the one bumped are reset to 0, and a pre-release or build suffix is dropped:
//
//   2.0.1       major -> 3.0.0   minor -> 2.1.0   patch -> 2.0.2
//   2.1.0-rc.1  patch -> 2.1.1
//
// Only the "version" value is rewritten, in place, so the file keeps its own indentation, key order
// and line endings - re-serialising the JSON would reformat it.
//
//   bun bump-version.ts <major|minor|patch> [package.json] [--dry-run]

import { readFileSync, writeFileSync } from "node:fs";

const parts = ["major", "minor", "patch"] as const;
type Part = (typeof parts)[number];

const args = process.argv.slice(2);
const dryRun = args.includes("--dry-run");
const [part, file = "package.json"] = args.filter((arg) => arg !== "--dry-run");

if (!parts.includes(part as Part)) {
	console.error(`Expected one of ${parts.join(", ")}, not '${part ?? ""}'.`);
	process.exit(1);
}

const text = readFileSync(file, "utf8");
const versionPattern = /("version"\s*:\s*")([^"]*)(")/;
const match = versionPattern.exec(text);
if (!match) {
	console.error(`No "version" field in ${file}.`);
	process.exit(1);
}

const current = match[2];
const semver = /^(\d+)\.(\d+)\.(\d+)(?:[-+].*)?$/.exec(current);
if (!semver) {
	console.error(`'${current}' in ${file} is not a major.minor.patch version.`);
	process.exit(1);
}

let [major, minor, patch] = semver.slice(1, 4).map(Number);
switch (part as Part) {
	case "major":
		[major, minor, patch] = [major + 1, 0, 0];
		break;
	case "minor":
		[minor, patch] = [minor + 1, 0];
		break;
	case "patch":
		patch += 1;
		break;
}

const next = `${major}.${minor}.${patch}`;
if (dryRun) {
	console.log(`  ${file}: ${current} -> ${next} (dry run, nothing written)`);
	process.exit(0);
}

writeFileSync(file, text.replace(versionPattern, `$1${next}$3`));
console.log(`  ${file}: ${current} -> ${next}`);
