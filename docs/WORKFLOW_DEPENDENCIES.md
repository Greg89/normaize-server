# Workflow Dependencies

## Overview

The project now uses SonarQube as a gatekeeper workflow that must pass before any other workflows can run. This ensures code quality standards are met before proceeding with CI/CD processes.

## Workflow Hierarchy

```
SonarQube Analysis (Gatekeeper)
├── CI/CD Pipeline (main/develop)
│   └── Version Bump (main only)
└── Pull Request Check (PRs)
```

## Workflow Details

### 1. SonarQube Analysis (`.github/workflows/sonar-qube.yml`)
- **Trigger**: Push to main/develop, Pull requests
- **Purpose**: Code quality analysis, security scanning, technical debt assessment
- **Must Pass**: All other workflows depend on this
- **Tools**: SonarCloud integration

### 2. CI/CD Pipeline (`.github/workflows/ci.yml`)
- **Trigger**: After successful SonarQube analysis
- **Purpose**: Build, test, coverage, deployment
- **Dependencies**: SonarQube Analysis
- **Branches**: main, develop

### 3. Version Bump (`.github/workflows/version-bump.yml`)
- **Trigger**: After successful CI/CD on main branch
- **Purpose**: Automatic version management
- **Dependencies**: CI/CD Pipeline (which depends on SonarQube)
- **Branches**: main only

### 4. Pull Request Check (`.github/workflows/pr-check.yml`)
- **Trigger**: After successful SonarQube analysis on PRs
- **Purpose**: PR validation and testing
- **Dependencies**: SonarQube Analysis
- **Branches**: PRs to main/develop

## Execution Flow

### For Main/Develop Commits:
1. **SonarQube Analysis** runs first
2. If SonarQube passes → **CI/CD Pipeline** runs
3. If CI/CD passes on main → **Version Bump** runs

### For Pull Requests:
1. **SonarQube Analysis** runs first
2. If SonarQube passes → **Pull Request Check** runs

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