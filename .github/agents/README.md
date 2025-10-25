# GitHub Copilot Agents for Avatar Optimizer

This directory contains custom GitHub Copilot agent configurations to assist with development tasks in the Avatar Optimizer project.

## Available Agents

### Unity Test Runner Agent

**File**: `unity-test-runner.yml` / `unity-test-runner.md`

A specialized agent for running Unity EditMode tests on Avatar Optimizer code changes.

#### Purpose

This agent automates the Unity testing process based on the GameCI workflow configuration (`.github/workflows/gameci.yml`). It validates code changes by:

- Setting up a Unity 2022.3+ test environment
- Installing VRChat SDK and dependencies via VRC-Get
- Running EditMode tests (145+ test methods)
- Analyzing test results and providing actionable feedback

#### When to Use

Invoke this agent when:

- You've made changes to `Runtime/`, `Editor/`, or `API-Editor/` directories
- You want to validate your code changes before committing
- You need to run tests as part of PR review
- You want to verify that your changes don't break existing functionality

#### How to Invoke

You can trigger this agent by:

1. **Explicit Request**: 
   - "@workspace run Unity tests"
   - "@workspace validate my Unity changes"
   - "@workspace check if tests pass"

2. **Automatic Trigger**: The agent may be automatically suggested when:
   - You modify files in `Runtime/`, `Editor/`, or `API-Editor/`
   - You make changes to test files in `Test~/`
   - You request code validation before commit

#### What It Does

1. **Environment Setup**
   - Verifies Unity 2022.3+ is available
   - Checks VRC-Get is installed
   - Sets up VPM repositories and dependencies
   - Installs Avatar Optimizer package into test project

2. **Test Execution**
   - Runs Unity EditMode tests
   - Uses 60-minute timeout (tests take 30-45 minutes)
   - Captures test results and logs
   - Handles Unity compilation and domain reloads

3. **Result Analysis**
   - Parses test results (145+ test methods)
   - Identifies failed tests with stack traces
   - Checks Unity crash logs if needed
   - Provides detailed summary and recommendations

#### Expected Results

A successful run produces:

```
Test Results Summary:
  Total Tests:  145+
  Passed:       145
  Failed:       0
  
Status: ✅ All tests passed
```

Failed tests will include:
- Test method name and class
- Failure reason and stack trace
- Relevant log excerpts
- Suggested fixes (when possible)

#### Configuration

The agent is configured based on `.github/workflows/gameci.yml`:

- **Unity Version**: 2022
- **Test Mode**: EditMode (not PlayMode)
- **Coverage Options**: 
  - generateAdditionalMetrics
  - generateHtmlReport
  - generateBadgeReport
  - Assembly filters: `+com.anatawa12.avatar-optimizer.*,-*.test.*`
- **Timeout**: 60 minutes (minimum)

#### Helper Script

A bash helper script is provided at `.github/agents/run-unity-tests.sh` for manual or automated test execution:

```bash
# Basic usage
./.github/agents/run-unity-tests.sh

# With custom timeout
./.github/agents/run-unity-tests.sh --timeout 90

# Skip dependency setup
./.github/agents/run-unity-tests.sh --skip-dependencies

# Analyze existing results only
./.github/agents/run-unity-tests.sh --analyze-only

# Show help
./.github/agents/run-unity-tests.sh --help
```

#### Prerequisites

- **Unity 2022.3+**: Must be installed and licensed
- **VRC-Get CLI**: Download from https://github.com/vrc-get/vrc-get/releases
- **VRChat SDK**: Installed via VRC-Get (automated by agent)
- **NDMF**: Installed via VPM (automated by agent)

#### Important Notes

⚠️ **Critical Considerations**:

- This is a **UPM package**, NOT a Unity project
  - Never open this repository directly in Unity
  - Tests must run in a separate Unity project with this package installed

- **Timeout Requirements**:
  - NEVER cancel tests prematurely
  - Allow at least 30-45 minutes for completion
  - Unity compilation and domain reloads take significant time

- **Environment Requirements**:
  - Unity must be properly licensed (Unity Plus/Pro for CI)
  - Network access needed for VPM repository fetching
  - Sufficient disk space for Unity Library cache (~2-5 GB)

#### Troubleshooting

**"Unity License Required"**
- Ensure Unity is activated with a valid license
- GameCI workflow uses `UNITY_LICENSE_V3` secret
- Personal/free licenses may not work for automated testing

**"VPM Package Not Found"**
- Verify VRC-Get repositories are configured: `vrc-get repo list`
- Check network connectivity
- Try removing and re-adding repositories

**"Compilation Errors"**
- Verify Unity version (must be 2022.3+)
- Ensure all dependencies are installed
- Check for missing assembly references

**"Tests Timeout"**
- Increase timeout to 60-90 minutes
- Check Unity crash logs: `unity-editor.log`
- Verify sufficient system resources

**"Package Import Errors"**
- Ensure package is in `Packages/com.anatawa12.avatar-optimizer/`
- Verify `package.json` is valid
- Check Unity Console for import errors

#### References

- GameCI Workflow: `.github/workflows/gameci.yml`
- Package Manifest: `package.json`
- Development Instructions: `.github/copilot-instructions.md`
- Test Directory: `Test~/`

## Adding New Agents

To add a new custom agent:

1. Create agent configuration file: `.github/agents/your-agent-name.yml`
2. Define agent instructions in YAML or Markdown format
3. Specify triggers, capabilities, and requirements
4. Add documentation to this README
5. Test agent behavior with sample invocations

## Agent Best Practices

When creating or modifying agents:

- **Be Specific**: Clearly define the agent's role and responsibilities
- **Reference Source**: Link to authoritative configuration (e.g., workflows, scripts)
- **Include Examples**: Provide clear usage examples
- **Document Limitations**: Be explicit about what the agent cannot do
- **Set Expectations**: Define success criteria and typical execution time
- **Handle Errors**: Describe common issues and solutions

## Feedback and Improvements

If you have suggestions for improving these agents or want to request new agents:

1. Open an issue describing the use case
2. Reference specific workflows or tasks to automate
3. Provide examples of when the agent would be useful
4. Consider contributing agent configurations via PR

---

For more information about GitHub Copilot agents, see the [GitHub Copilot documentation](https://docs.github.com/en/copilot).
