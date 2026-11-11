#!/usr/bin/env node
/**
 * Prints a package's dependency groups exactly as NuGet declares them, so the floor a component
 * inherits can be read rather than guessed.
 *
 * The golden rule of package development is to declare the LOWEST compatible version, and that decision
 * needs the real graph: which dependency, at what minimum, on which target framework. This script reads
 * it from the registry - the `.nuspec` the published package carries - instead of from a table that
 * would be stale by the next release.
 *
 * Zero dependencies: it uses the platform `fetch`, so it runs anywhere Node 18 or later does.
 *
 *   node dependencies.mjs Purview.Containers
 *   node dependencies.mjs Purview.Architecture.Testing --tfm net11.0
 *   node dependencies.mjs Purview.Containers --walk        # recurse into Purview.* dependencies
 *   node dependencies.mjs Purview.Results --json
 *
 * An npm package is not a NuGet id; read it with `npm view <name> versions --json`.
 *
 * Exit codes: 0 on success, 1 when the package, version or network is not available.
 */

/** The NuGet flat-container root: the same host that serves the package's own `.nuspec`. */
const REGISTRY = "https://api.nuget.org/v3-flatcontainer";

/** How deep `--walk` follows dependencies before it stops. */
const MAX_DEPTH = 3;

/** True when a version string carries a prerelease label. */
function isPrerelease(version) {
	return version.includes("-");
}

/** Every published version of a package, oldest first. */
async function versionsOf(id) {
	const response = await fetch(`${REGISTRY}/${id.toLowerCase()}/index.json`);
	if (!response.ok) {
		throw new Error(`${id} is not on nuget.org (HTTP ${response.status}).`);
	}

	const body = await response.json();
	return body.versions;
}

/** The version to describe: the one asked for, else the newest stable, else the newest anything. */
function chooseVersion(available, requested) {
	if (requested) {
		const match = available.find((version) => version.toLowerCase() === requested.toLowerCase());
		if (!match) {
			throw new Error(`'${requested}' is not a published version. Newest is ${available.at(-1)}.`);
		}

		return match;
	}

	const stable = available.filter((version) => !isPrerelease(version));
	return stable.at(-1) ?? available.at(-1);
}

/** The package's `.nuspec`, which is what a consumer's restore actually reads. */
async function readNuspec(id, version) {
	const name = id.toLowerCase();
	const response = await fetch(`${REGISTRY}/${name}/${version.toLowerCase()}/${name}.nuspec`);
	if (!response.ok) {
		throw new Error(`No .nuspec for ${id} ${version} (HTTP ${response.status}).`);
	}

	return response.text();
}

/** The value of one XML attribute on a tag, or null. */
function attribute(tag, name) {
	const match = new RegExp(`${name}\\s*=\\s*"([^"]*)"`).exec(tag);
	return match ? match[1] : null;
}

/**
 * How a nuspec version string constrains a consumer: a bare version is a MINIMUM, and a bracketed one is
 * a range. This is the distinction the whole rule turns on, because a minimum you raise is a minimum
 * every consumer inherits.
 */
function classify(range) {
	if (!range) {
		return "any";
	}

	return range.startsWith("[") || range.startsWith("(") ? "range" : "minimum";
}

/** The dependency elements inside a fragment, normalized. */
function dependenciesIn(fragment) {
	const dependencies = [];
	const pattern = /<dependency\b([^>]*)\/?>/g;
	let match;

	while ((match = pattern.exec(fragment)) !== null) {
		const tag = match[1];
		const version = attribute(tag, "version");
		dependencies.push({ id: attribute(tag, "id"), version, constraint: classify(version) });
	}

	return dependencies;
}

/** The dependency groups a nuspec declares, one per target framework. */
function dependencyGroups(nuspec) {
	const groups = [];
	const pattern = /<group\b([^>]*)>([\s\S]*?)<\/group>/g;
	let match;

	while ((match = pattern.exec(nuspec)) !== null) {
		groups.push({
			framework: attribute(match[1], "targetFramework") ?? "(any)",
			dependencies: dependenciesIn(match[2]),
		});
	}

	if (groups.length === 0) {
		const flat = dependenciesIn(nuspec);
		if (flat.length > 0) {
			groups.push({ framework: "(any)", dependencies: flat });
		}
	}

	return groups;
}

/** A printable name for a target framework, spelled the way the ecosystem usually writes it. */
function shortFramework(framework) {
	return framework
		.replace(/^\.NETStandard/i, "netstandard")
		.replace(/^\.NETCoreApp/i, "net")
		.replace(/^\.NETFramework/i, "netframework")
		.toLowerCase();
}

/** Prints one package's groups, recursing into Purview components when asked. */
async function describe(id, requestedVersion, options, depth, seen) {
	const version = chooseVersion(await versionsOf(id), requestedVersion);
	const groups = dependencyGroups(await readNuspec(id, version));
	const indent = "  ".repeat(depth);

	console.log(`${indent}${id} ${version}`);

	for (const group of groups) {
		const framework = shortFramework(group.framework);
		if (options.tfm && framework !== options.tfm.toLowerCase()) {
			continue;
		}

		if (group.dependencies.length === 0) {
			console.log(`${indent}  ${framework}: no dependencies`);
			continue;
		}

		for (const dependency of group.dependencies) {
			const relation = dependency.constraint === "minimum" ? ">=" : dependency.constraint;
			console.log(
				`${indent}  ${framework}: ${dependency.id} ${relation} ${dependency.version ?? "any"}`,
			);
		}

		if (options.walk && depth < MAX_DEPTH) {
			for (const dependency of group.dependencies) {
				const isPurview = /^purview\./i.test(dependency.id ?? "");
				if (!(options.all || isPurview) || seen.has(dependency.id.toLowerCase())) {
					continue;
				}

				seen.add(dependency.id.toLowerCase());
				await describe(dependency.id, null, options, depth + 1, seen);
			}
		}
	}

	return { id, version, groups };
}

/** Parses the command line. An unknown argument is a mistake worth failing on. */
function parseArguments(argv) {
	const options = { packages: [], tfm: null, walk: false, all: false, json: false };

	for (let index = 0; index < argv.length; index += 1) {
		const argument = argv[index];

		if (argument === "--walk" || argument === "-w") {
			options.walk = true;
		} else if (argument === "--all") {
			options.all = true;
		} else if (argument === "--json") {
			options.json = true;
		} else if (argument === "--tfm") {
			options.tfm = argv[(index += 1)];
			if (!options.tfm) {
				throw new Error("--tfm needs a value, e.g. --tfm net10.0.");
			}
		} else if (argument.startsWith("-")) {
			throw new Error(`Unknown argument '${argument}'.`);
		} else {
			options.packages.push(argument);
		}
	}

	if (options.packages.length === 0) {
		throw new Error(
			"Name at least one package, e.g. `node dependencies.mjs Purview.Containers`.\n" +
				"A package may carry a version: `Purview.Containers@1.0.0-prerelease.11`.",
		);
	}

	return options;
}

/** The command. */
async function main() {
	const options = parseArguments(process.argv.slice(2));
	const results = [];

	for (const entry of options.packages) {
		const separator = entry.indexOf("@", 1);
		const id = separator === -1 ? entry : entry.slice(0, separator);
		const version = separator === -1 ? null : entry.slice(separator + 1);

		results.push(await describe(id, version, options, 0, new Set([id.toLowerCase()])));

		if (!options.walk && !options.json) {
			console.log("");
		}
	}

	if (options.json) {
		console.log(JSON.stringify(results, null, "\t"));
	}
}

main().catch((error) => {
	console.error(`error: ${error.message}`);
	process.exitCode = 1;
});
