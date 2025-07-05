# Coverage Report Setup

This document describes how to set up automated coverage reporting using your licensed tool and GitHub Pages.

## Overview

The coverage workflow:
1. Runs tests with coverage collection using Coverlet
2. Generates HTML reports using your licensed tool
3. Deploys reports to GitHub Pages
4. Only triggers on main, develop, and feature branches

## Setup Steps

### 1. GitHub Pages Configuration

1. Go to your repository Settings
2. Navigate to Pages section
3. Set Source to "GitHub Actions"
4. Save the configuration

### 2. Configure Your Licensed Tool

Replace the placeholder in `.github/workflows/coverage.yml`:

```yaml
# Replace this section:
echo "Coverage data collected. Add your tool command here to generate HTML report."

# With your actual tool command, for example:
your-tool-command --input ./coverage-results/ --output ./coverage-report/ --format html
```

### 3. Tool Integration Examples

#### Example 1: ReportGenerator Pro
```yaml
- name: Install ReportGenerator Pro
  env:
    REPORTGENERATOR_LICENSE: ${{ secrets.REPORTGENERATOR_LICENSE }}
  run: |
    # Install ReportGenerator Pro based on your license
    dotnet tool install -g ReportGenerator.Pro

- name: Generate coverage report
  run: |
    reportgenerator-pro --reports:"./coverage-results/**/coverage.cobertura.xml" --targetdir:"./coverage-report" --reporttypes:Html --verbosity:Info
```

#### Example 2: ReportGenerator (Free Version)
```yaml
- name: Install ReportGenerator
  run: dotnet tool install -g dotnet-reportgenerator-globaltool

- name: Generate coverage report
  run: |
    reportgenerator -reports:"./coverage-results/**/coverage.cobertura.xml" -targetdir:"./coverage-report" -reporttypes:Html
```

#### Example 2: Custom Tool
```yaml
- name: Generate coverage report
  run: |
    # Download your tool if needed
    # wget https://your-tool-url/tool.zip
    # unzip tool.zip
    
    # Run your tool
    ./your-tool --coverage-file ./coverage-results/coverage.cobertura.xml --output-dir ./coverage-report/
```

#### Example 3: Tool with License
```yaml
- name: Setup tool license
  run: |
    # Set up your tool license
    echo "${{ secrets.TOOL_LICENSE_KEY }}" > license.key
    
- name: Generate coverage report
  run: |
    your-tool --license license.key --input ./coverage-results/ --output ./coverage-report/
```

### 4. Environment Variables (if needed)

If your tool requires environment variables or secrets:

1. Go to repository Settings → Secrets and variables → Actions
2. Add your secrets (e.g., `TOOL_LICENSE_KEY`)
3. Reference them in the workflow:

```yaml
- name: Generate coverage report
  env:
    TOOL_LICENSE: ${{ secrets.TOOL_LICENSE_KEY }}
  run: |
    your-tool --license $TOOL_LICENSE --input ./coverage-results/ --output ./coverage-report/
```

## Workflow Behavior

### Branches
- **main**: Generates and deploys coverage report
- **develop**: Generates and deploys coverage report  
- **feature/logging-setup**: Generates coverage report (for testing)
- **Other branches**: No coverage report generated

### Triggers
- Push to main, develop, or feature/logging-setup
- Pull requests to main or develop

### Artifacts
- Coverage results: Available for 30 days
- Coverage report: Available for 30 days
- GitHub Pages: Live at `https://yourusername.github.io/yourrepo/`

## Cleanup Process

After merging your feature branch:

1. The `cleanup-coverage.yml` workflow will automatically run
2. It removes `feature/logging-setup` from the coverage workflow
3. The workflow will only run on main and develop going forward

## Troubleshooting

### Coverage Data Not Generated
- Check that coverlet.collector is installed in test project
- Verify test execution is successful
- Check coverage results directory exists

### Tool Command Fails
- Verify tool command syntax
- Check if tool is available in GitHub Actions environment
- Ensure license/credentials are properly configured

### GitHub Pages Not Updated
- Check GitHub Pages is enabled and configured for GitHub Actions
- Verify the workflow completed successfully
- Check Pages deployment logs in repository Actions tab

### Feature Branch Not Removed
- The cleanup workflow only runs on merge commits
- Manually remove the branch from coverage.yml if needed

## Customization

### Add More Branches
Edit `.github/workflows/coverage.yml`:
```yaml
on:
  push:
    branches: [ main, develop, feature/logging-setup, feature/another-branch ]
```

### Change Report Format
Modify your tool command to generate different formats:
```yaml
your-tool --input ./coverage-results/ --output ./coverage-report/ --format html,json,xml
```

### Add Coverage Thresholds
Add coverage validation:
```yaml
- name: Check coverage threshold
  run: |
    # Add your coverage threshold check here
    your-tool --check-threshold 80 --input ./coverage-results/
```

## Security Considerations

- Store tool licenses as GitHub Secrets
- Don't commit license files to repository
- Use least-privilege access for GitHub Actions
- Review workflow permissions in repository settings 