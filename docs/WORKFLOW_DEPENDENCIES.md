# Workflow Dependencies

## Overview

The project now uses SonarQube as a gatekeeper workflow that must pass before any other workflows can run. This ensures code quality standards are met before proceeding with CI/CD processes.

## Workflow Hierarchy

```
CI/CD Pipeline (main/develop) ← Railway watches this
├── SonarQube Analysis (integrated)
├── Tests & Coverage
└── Version Bump (main only)

Pull Request Check (PRs - runs in parallel)
```

## Workflow Details

### 1. CI/CD Pipeline (`.github/workflows/ci.yml`)
- **Trigger**: Push to main/develop, Pull requests
- **Purpose**: Build, test, coverage, SonarQube analysis, deployment
- **Dependencies**: None (self-contained)
- **Branches**: main, develop
- **Components**: SonarQube analysis, tests, coverage, security scan

### 2. Version Bump (`.github/workflows/version-bump.yml`)
- **Trigger**: After successful CI/CD on main branch
- **Purpose**: Automatic version management
- **Dependencies**: CI/CD Pipeline
- **Branches**: main only

### 3. Pull Request Check (`.github/workflows/pr-check.yml`)
- **Trigger**: Pull requests to main/develop
- **Purpose**: PR validation and testing
- **Dependencies**: Runs in parallel with SonarQube Analysis
- **Branches**: PRs to main/develop

## Execution Flow

### For Main/Develop Commits:
1. **CI/CD Pipeline** runs (includes SonarQube analysis, tests, coverage)
2. Railway watches this workflow completion
3. If CI/CD passes on main → **Version Bump** runs

### For Pull Requests:
1. **CI/CD Pipeline** and **Pull Request Check** run in parallel
2. Both must pass for PR to be mergeable

## Benefits

1. **Quality Gate**: Ensures code meets quality standards before any other processing
2. **Security**: Security vulnerabilities are caught early
3. **Efficiency**: Prevents unnecessary CI/CD runs on low-quality code
4. **Consistency**: All code paths go through the same quality checks

## Configuration

### SonarQube Setup
- **Project Key**: `Greg89_normaize-server`
- **Organization**: `greg89`
- **Platform**: SonarCloud
- **Token**: `SONAR_TOKEN` secret

### Required Secrets
```bash
SONAR_TOKEN=your-sonarcloud-token
```

## Railway Configuration

### Workflow Dependency
Railway's "Wait for CI" setting should watch the **"CI/CD Pipeline"** workflow, which includes SonarQube analysis, tests, and coverage.

### Railway Dashboard Setup
1. Ensure "Wait for CI" is enabled in Railway project settings
2. Railway will automatically detect the "CI/CD Pipeline" workflow
3. The workflow includes SonarQube analysis, tests, coverage, and security scan

### How It Works
1. CI/CD Pipeline runs (includes SonarQube analysis, tests, coverage)
2. Railway sees CI/CD Pipeline complete successfully and deploys
3. Version Bump runs after deployment is triggered

## Troubleshooting

### SonarQube Fails
- Check SonarCloud dashboard for detailed analysis
- Review code quality issues
- Fix violations before other workflows can run

### Workflow Not Triggering
- Ensure SonarQube analysis completed successfully
- Check workflow dependency conditions
- Verify branch names match expected values

### Performance Issues
- SonarQube analysis may take time
- Consider adjusting analysis settings
- Monitor SonarCloud usage limits

## Best Practices

1. **Fix SonarQube Issues First**: Address quality issues before other concerns
2. **Monitor Quality Gates**: Keep track of SonarQube quality gate status
3. **Regular Reviews**: Periodically review SonarQube configuration
4. **Team Awareness**: Ensure team understands the dependency structure 