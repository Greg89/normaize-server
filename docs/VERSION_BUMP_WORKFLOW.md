# Version Bump Workflow

## Overview

The version bump workflow automatically creates pull requests for version updates after successful CI/CD pipeline completion. This approach respects branch protection rules and prevents infinite CI loops.

## How It Works

### 1. Trigger
- **When**: After successful completion of the "CI/CD Pipeline" workflow on the `main` branch
- **Condition**: Only runs if the CI/CD pipeline was successful

### 2. Version Analysis
- **Current Version**: Extracted from `.csproj` files
- **Bump Type**: Determined by analyzing recent commit messages:
  - **Major**: `breaking`, `major`, `!:`
  - **Minor**: `feat`, `feature`, `new`
  - **Patch**: Default for other changes

### 3. PR Creation Process
1. **Branch Creation**: Creates a new branch `version-bump/X.Y.Z`
2. **Version Update**: Updates all `.csproj` files and `AssemblyInfo.cs` (if present)
3. **Commit**: Commits changes with `[skip ci]` in the message
4. **Push**: Pushes the branch to origin
5. **PR Creation**: Creates a pull request using `peter-evans/create-pull-request@v6`

### 4. CI Loop Prevention
The workflow includes multiple protections to prevent infinite CI loops:

#### Commit Message Protection
```yaml
if: !contains(github.event.head_commit.message, '[skip ci]')
```

#### Branch Name Protection
```yaml
if: !startsWith(github.ref, 'refs/heads/version-bump/')
```

#### Combined Protection
```yaml
if: |
  !contains(github.event.head_commit.message, '[skip ci]') &&
  !startsWith(github.ref, 'refs/heads/version-bump/')
```

## Workflow Steps

### 1. Setup and Analysis
- **Checkout**: Gets the latest code from main branch
- **Version Detection**: Extracts current version from project files
- **Bump Type Analysis**: Analyzes commit messages to determine version increment type

### 2. Version Calculation
- **Parsing**: Parses current version (e.g., `1.2.3`)
- **Increment**: Calculates new version based on bump type:
  - Major: `1.2.3` → `2.0.0`
  - Minor: `1.2.3` → `1.3.0`
  - Patch: `1.2.3` → `1.2.4`

### 3. File Updates
- **Project Files**: Updates `<Version>` tags in all `.csproj` files
- **Assembly Info**: Updates `AssemblyVersion` in `AssemblyInfo.cs` (if present)

### 4. Branch and PR Creation
- **Branch**: Creates `version-bump/X.Y.Z` branch
- **Commit**: Commits with `[skip ci]` message
- **Push**: Pushes branch to origin
- **PR**: Creates pull request with detailed description

### 5. Release Creation
- **Trigger**: After PR is merged
- **Tag**: Creates Git tag `vX.Y.Z`
- **Release**: Creates GitHub release with changelog

## Pull Request Details

### PR Title
```
chore: bump version to X.Y.Z
```

### PR Body
- **Version Details**: Previous and new version numbers
- **Bump Type**: Major/Minor/Patch classification
- **Files Changed**: List of updated files
- **Notes**: Explanation of `[skip ci]` usage

### PR Labels
- `automated`: Indicates this is an automated PR
- `version-bump`: Identifies version bump PRs

### PR Assignees/Reviewers
- Automatically assigned to the workflow trigger user

## Branch Protection Compatibility

### Protected Branch Rules
This workflow is designed to work with protected branches that require:
- **Pull Request Reviews**: Version bumps go through PR review process
- **Status Checks**: PRs can be configured to require CI checks (which will be skipped)
- **No Direct Pushes**: Prevents direct commits to main branch

### Required Permissions
The workflow requires these permissions:
- `contents: write`: To create branches and commits
- `actions: read`: To read workflow run information
- `pull-requests: write`: To create pull requests

## Example Workflow

### Scenario: Feature Addition
1. **Developer**: Pushes feature to `develop` branch
2. **CI/CD**: Runs tests and builds successfully
3. **Merge**: Feature is merged to `main`
4. **CI/CD**: Runs on `main` branch successfully
5. **Version Bump**: Workflow analyzes commits, detects minor bump
6. **PR Creation**: Creates `version-bump/1.1.0` PR
7. **Review**: Team reviews and approves PR
8. **Merge**: PR is merged to `main`
9. **Release**: GitHub release `v1.1.0` is created

### Scenario: Breaking Change
1. **Developer**: Pushes breaking change with `!:` prefix
2. **CI/CD**: Runs successfully
3. **Version Bump**: Workflow detects major bump
4. **PR Creation**: Creates `version-bump/2.0.0` PR
5. **Review**: Team reviews breaking changes
6. **Merge**: PR is merged to `main`
7. **Release**: GitHub release `v2.0.0` is created

## Troubleshooting

### Common Issues

#### PR Not Created
- **Cause**: No version changes detected
- **Solution**: Check if version was already bumped or no relevant changes exist

#### CI Still Running on Version Bump
- **Cause**: Branch protection or workflow configuration issue
- **Solution**: Verify `[skip ci]` is in commit message and branch name starts with `version-bump/`

#### Permission Errors
- **Cause**: Insufficient GitHub token permissions
- **Solution**: Ensure workflow has `contents: write` and `pull-requests: write` permissions

### Debugging

#### Enable Debug Logging
Add this to your workflow for more detailed output:
```yaml
- name: Debug Information
  run: |
    echo "Current version: ${{ steps.current-version.outputs.version }}"
    echo "Bump type: ${{ steps.bump-type.outputs.bump-type }}"
    echo "New version: ${{ steps.new-version.outputs.new-version }}"
    echo "Has changes: ${{ steps.check-changes.outputs.has-changes }}"
```

#### Check Workflow Conditions
Verify the workflow conditions are met:
- CI/CD pipeline completed successfully
- Changes were detected in version files
- Branch protection allows PR creation

## Best Practices

### 1. Commit Message Conventions
Use conventional commit messages to help with version bump detection:
- `feat: new feature` → Minor bump
- `fix: bug fix` → Patch bump
- `feat!: breaking change` → Major bump

### 2. PR Review Process
- Review version bump PRs before merging
- Verify version increment is appropriate
- Check that all project files are updated

### 3. Release Management
- Monitor automated releases
- Add manual release notes if needed
- Tag releases appropriately

### 4. Branch Cleanup
- Consider automatic deletion of version-bump branches after merge
- Monitor for stale version-bump branches

## Configuration

### Environment Variables
No additional environment variables are required beyond standard GitHub Actions setup.

### Secrets
- `GITHUB_TOKEN`: Automatically provided by GitHub Actions
- No additional secrets required

### Customization
You can customize the workflow by modifying:
- **Version detection logic**: Change how current version is extracted
- **Bump type detection**: Modify commit message analysis
- **PR template**: Update PR body and labels
- **Release notes**: Customize release description format 