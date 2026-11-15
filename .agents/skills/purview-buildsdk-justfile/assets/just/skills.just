# The Purview.BuildSdk agent-skill targets. Each recipe names one target; the behaviour lives there.
#
# The targets are a thin wrapper over the standard skills CLI (`bun x skills` by default), run from the
# repository root. Every skill target runs once for the whole solution, however many projects it holds,
# so the recipes name the solution, not a hand-picked project.
#
# Every recipe here is run by a person, on purpose. Never make one a dependency of build, test or
# default, or call skills-add, -install, -update or -fix from a git hook: they change the repository,
# and the SDK refuses to run them inside a build (PRSKL0009). skills-check is read-only.
#
# The runner and agents are MSBuild properties (PurviewSkillsRunner, PurviewSkillsAgents), set once in
# Directory.Build.props. Pass a one-off through *args; `dotnet msbuild -p:` splits on `,` and `;`, so
# escape the separator: `just skills-update -p:PurviewSkillsAgents=claude-code%2Ccodex`.

skills_project := solution_file

# Install skills from another repository through the skills CLI (append '#<ref>' to pin it)
#
#   just skills-add purview-dev/engineering-playbook
[doc("Install skills from another repository through the skills CLI (append '#<ref>' to pin it)")]
[group('Agent Skills')]
skills-add source *args:
    echo "==> Installing skills from {{ BLUE }}{{ source }}{{ NORMAL }}"
    dotnet msbuild "{{ skills_project }}" -t:InstallPurviewSkills -p:PurviewSkillsSource={{ source }} -v:n {{ args }}

# Restore every skill skills-lock.json records - how a fresh clone gets the repository's skills back
[group('Agent Skills')]
skills-install *args:
    dotnet msbuild "{{ skills_project }}" -t:InstallPurviewSkills -v:n {{ args }}

# Update the agent skills by re-adding every unique source in skills-lock.json
#
# Each source is re-added at its latest with every skill it has, which refreshes the installed skills
# and adds any a source has gained; `skills update` is never run. FixPurviewSkills then removes links
# from agent directories PurviewSkillsAgents does not select, and does nothing when it is unset.
# The -t: value is quoted because sh would otherwise end the command at the `;`.
#
#   just skills-update -p:PurviewSkillsDryRun=true
[doc('Update the agent skills by re-adding every unique source in skills-lock.json')]
[group('Agent Skills')]
skills-update *args:
    dotnet msbuild "{{ skills_project }}" "-t:UpdatePurviewSkills;FixPurviewSkills" -v:n {{ args }}

# Check that only the skill directories of the agents in PurviewSkillsAgents exist
[group('Agent Skills')]
skills-check *args:
    dotnet msbuild "{{ skills_project }}" -t:CheckPurviewSkills -v:n {{ args }}

# Remove the skills CLI's links from agent directories PurviewSkillsAgents does not select
[group('Agent Skills')]
skills-fix *args:
    dotnet msbuild "{{ skills_project }}" -t:FixPurviewSkills -v:n {{ args }}
