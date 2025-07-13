# Automatic Versioning System

## Overview

The project uses an automatic versioning system that bumps version numbers based on commit messages and only triggers after successful CI/CD pipeline completion on the main branch.

## How It Works

### 1. **Trigger Conditions**
- Only runs after successful CI/CD pipeline completion
- Only triggers on `main` branch
- Skips version bump commits to prevent infinite loops

### 2. **Version Bump Logic**
The system analyzes commit messages to determine the appropriate version increment:

#### **Major Version (X.0.0)**
- **Keywords**: `breaking`, `major`, `!:`, `BREAKING CHANGE`
- **Use case**: Breaking changes, incompatible API changes
- **Example**: `feat!: remove deprecated API endpoint`

#### **Minor Version (0.X.0)**
- **Keywords**: `feat`, `feature`, `new`, `enhancement`
- **Use case**: New features in a backwards compatible manner
- **Example**: `feat: add new data analysis endpoint`

#### **Patch Version (0.0.X)**
- **Keywords**: `fix`, `bug`, `patch`, `docs`, `style`, `refactor`, `test`, `chore`
- **Use case**: Bug fixes and minor improvements
- **Example**: `fix: resolve memory leak in data processing`

### 3. **Workflow Steps**
1. **CI/CD Pipeline** runs on main branch
2. **Version Bump Workflow** triggers only if CI/CD succeeds
3. **Commit Analysis** determines bump type
4. **Version Update** modifies all `.csproj` files
5. **GitHub Release** created automatically
6. **Version Commit** pushed with `[skip ci]` to prevent loops

## Files Updated

The versioning system updates these files:
- `Normaize.API/Normaize.API.csproj`
- `Normaize.Core/Normaize.Core.csproj`
- `Normaize.Data/Normaize.Data.csproj`
- `Normaize.Tests/Normaize.Tests.csproj`

## Commit Message Guidelines

### **Conventional Commits**
Use conventional commit format for automatic version detection:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### **Types that affect versioning:**
- `feat:` - New feature (minor version)
- `fix:` - Bug fix (patch version)
- `docs:` - Documentation (patch version)
- `style:` - Code style changes (patch version)
- `refactor:` - Code refactoring (patch version)
- `test:` - Adding tests (patch version)
- `chore:` - Maintenance tasks (patch version)

### **Breaking Changes:**
- Add `!:` after type: `feat!: breaking change`
- Include `BREAKING CHANGE:` in commit body
- Use `major` keyword in commit message

## Manual Version Management

### **Check Current Version**
```bash
./scripts/version-helper.ps1 show
```

### **Analyze Version Bump**
```bash
./scripts/version-helper.ps1 analyze
```

### **Manual Version Update**
```bash
./scripts/version-helper.ps1 update 1.2.3
```

## GitHub Actions Workflows

### **Main CI/CD Pipeline** (`.github/workflows/ci.yml`)
- Runs tests, builds, and coverage
- Skips execution for version bump commits (`[skip ci]`)
- Triggers version bump workflow on success

### **Version Bump Workflow** (`.github/workflows/version-bump.yml`)
- Runs only after successful CI/CD
- Analyzes commit messages
- Updates version numbers
- Creates GitHub release
- Pushes version commit

## Configuration

### **Version Rules** (`version-config.json`)
```json
{
  "versionBumpRules": {
    "major": {
      "keywords": ["breaking", "major", "!:", "BREAKING CHANGE"]
    },
    "minor": {
      "keywords": ["feat", "feature", "new", "enhancement"]
    },
    "patch": {
      "keywords": ["fix", "bug", "patch", "docs", "style", "refactor", "test", "chore"]
    }
  }
}
```

## Benefits

1. **Automatic**: No manual version management required
2. **Semantic**: Version bumps based on actual changes
3. **Safe**: Only triggers after successful CI/CD
4. **Consistent**: All project files updated together
5. **Traceable**: GitHub releases created automatically
6. **Loop Prevention**: Version commits don't trigger CI/CD

## Troubleshooting

### **Version Not Bumping**
- Check if CI/CD pipeline succeeded
- Verify commit messages contain version keywords
- Ensure workflow has proper permissions

### **Wrong Version Bump Type**
- Review commit messages for appropriate keywords
- Check `version-config.json` for keyword definitions
- Use conventional commit format

### **Infinite Loop**
- Version bump commits include `[skip ci]`
- CI/CD workflow skips these commits
- Check workflow conditions if issues persist

## Examples

### **Patch Version (1.0.0 → 1.0.1)**
```
fix: resolve null reference in data processing
docs: update API documentation
style: format code according to guidelines
```

### **Minor Version (1.0.0 → 1.1.0)**
```
feat: add new data visualization endpoint
feat: implement user authentication
feature: add export functionality
```

### **Major Version (1.0.0 → 2.0.0)**
```
feat!: remove deprecated API endpoints
BREAKING CHANGE: change authentication method
major: rewrite data processing engine
``` 