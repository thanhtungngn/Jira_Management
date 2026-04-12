# Copilot Workspace Instructions

## Documentation Update Rule

After every code change (create, edit, refactor, or delete), always update all project documentation to match the latest behavior and structure.

Required checklist after each code change:
- Update `README.md` if setup, usage, commands, endpoints, or architecture details changed.
- Update all files in `docs/` to keep architecture, workflows, and technical references in sync.
- Update inline XML comments and public API descriptions in source code when behavior changes.
- Update examples and sample configuration files if related fields or flows changed.
- Do not finish the task until documentation updates are complete.

If there is no documentation impact, explicitly state that in the final response with a short verification note.

## Documentation Definition Of Done

Every implementation task must include a documentation pass before completion.

Mandatory process after each code change:
1. Identify impacted surfaces:
	- API endpoints, request/response contracts, error behavior
	- MCP tools, parameters, return shape, and tool names
	- Configuration keys, environment variables, startup wiring
	- Architecture, data flow, deployment, or dependency changes
2. Update documentation artifacts:
	- `README.md` for usage, setup, examples, and quick reference
	- all files in `docs/` for architecture and technical details
	- XML comments and public API summaries where behavior changed
	- example config files and sample payloads when relevant
3. Validate consistency:
	- no stale endpoint/tool names
	- no stale config keys
	- examples compile conceptually with current code
4. Validate test coverage:
	- add or update unit tests for all changed behavior
	- run relevant unit test suites for impacted modules
	- include test execution result summary in final response
	- if unit tests are intentionally not added, explain why and list risk
5. Report completion in final response:
	- list updated documentation files
	- list added/updated unit test files
	- summarize test results (passed/failed/skipped)
	- summarize what changed in docs
	- if no doc impact, include a one-line verification note

Completion gate:
- A task is not complete until documentation and unit test steps above are done.

## Task Completion Checklist

Use this checklist before marking work as done:

- [ ] Code changes implemented and self-reviewed
- [ ] Unit tests added/updated for changed behavior
- [ ] Relevant unit tests executed and passing
- [ ] `README.md` updated when usage/setup/API changed
- [ ] Files in `docs/` updated to match architecture/flow changes
- [ ] XML comments/public API descriptions updated when behavior changed
- [ ] Example config/sample payloads updated when fields/flows changed
- [ ] Final response includes: changed files, test summary, and doc summary
