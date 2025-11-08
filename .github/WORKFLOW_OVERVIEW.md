# GitHub Actions Workflow Overview

This document provides an overview of all GitHub Actions workflows in the Aeon repository.

## Workflows

### 1. CI Workflow (`.github/workflows/dotnet-ci.yml`)

**Purpose**: Continuous Integration for development

**Triggers**:
- Push to `main`, `develop`, or `copilot/**` branches
- Pull requests to `main` or `develop`

**Jobs**:
1. **Build and Test** (3 platforms in parallel)
   - Ubuntu (linux-x64)
   - Windows (win-x64)
   - macOS (osx-x64)
   
   Each platform:
   - Restores dependencies
   - Builds in Release configuration
   - Runs full test suite
   - Uploads test results

2. **Code Quality Checks** (Ubuntu only)
   - Code formatting verification
   - Build with warnings analysis

3. **Publish Build Artifacts** (Ubuntu only, on push to main/develop)
   - Creates framework-dependent builds for all platforms
   - Uploads artifacts to GitHub Actions (30-day retention)

**Outcome**: Validates that code builds and tests pass on all platforms

---

### 2. Release Workflow (`.github/workflows/release.yml`)

**Purpose**: Continuous Deployment to GitHub Releases

**Triggers**:
- Push of version tags matching `v*.*.*` (e.g., `v1.0.0`, `v2.1.3`)
- Manual workflow dispatch via GitHub UI

**Jobs**:
1. **Create Release**
   - Generates changelog from git commits since last tag
   - Creates GitHub Release with release notes
   - Detects pre-release versions (alpha, beta, rc)

2. **Build and Upload Assets** (3 platforms in parallel)
   - Builds framework-dependent binaries
   - Runs tests before building
   - Creates platform-specific archives:
     - Windows: `.zip`
     - Linux: `.tar.gz`
     - macOS: `.tar.gz`
   - Uploads artifacts to GitHub Release

3. **Build Self-Contained Builds** (3 platforms in parallel)
   - Builds self-contained binaries (includes .NET runtime)
   - Creates standalone versions that don't require .NET installation
   - Uploads as separate artifacts with `-standalone` suffix

**Artifacts Produced**:
- `aeon-windows-x64.zip` (framework-dependent)
- `aeon-windows-x64-standalone.zip` (self-contained)
- `aeon-linux-x64.tar.gz` (framework-dependent)
- `aeon-linux-x64-standalone.tar.gz` (self-contained)
- `aeon-macos-x64.tar.gz` (framework-dependent)
- `aeon-macos-x64-standalone.tar.gz` (self-contained)

**Outcome**: Automated releases with cross-platform binaries

---

## Workflow Diagram

```
Push to branch     →  CI Workflow  →  Build & Test on all platforms
      ↓
   [if main/develop]
      ↓
   Publish dev artifacts

Push version tag   →  Release Workflow  →  Create GitHub Release
(e.g., v1.0.0)              ↓
                    Build 6 release artifacts
                            ↓
                    Upload to GitHub Release
```

## Common Operations

### Running CI
CI runs automatically on every push and PR. No manual action needed.

### Creating a Release
```bash
# Create and push a tag
git tag v1.0.0
git push origin v1.0.0
```

### Triggering Release Manually
1. Go to Actions tab in GitHub
2. Select "Release" workflow
3. Click "Run workflow"
4. Enter tag name (e.g., `v1.0.0`)
5. Click "Run workflow" button

### Viewing Workflow Results
1. Go to Actions tab in GitHub
2. Select the workflow run
3. View logs for each job
4. Download artifacts (for CI) or check GitHub Releases (for releases)

## Permissions

Both workflows use:
- `GITHUB_TOKEN` (automatically provided by GitHub)
- `contents: write` permission (for creating releases)

## Maintenance

### Updating .NET Version
Both workflows use `.NET 9.0.x`. To update:
1. Change `dotnet-version: '9.0.x'` in both workflow files
2. Test locally with the new version first
3. Commit and push changes

### Adding New Platforms
To add a new platform (e.g., ARM64):
1. Add to matrix in both workflows
2. Add corresponding publish steps
3. Test the builds locally first

### Modifying Release Notes
Release notes are auto-generated from commits. To customize:
1. Edit the "Generate changelog" step in `release.yml`
2. Modify the template in the heredoc section

## Troubleshooting

### Workflow Not Triggering
- **CI**: Check branch names match the trigger patterns
- **Release**: Ensure tag follows `v*.*.*` format

### Build Failures
1. Check the workflow logs in Actions tab
2. Verify the code builds locally: `dotnet build src/Aeon.sln -c Release`
3. Ensure all tests pass: `dotnet test src/Aeon.sln -c Release`

### Release Not Created
- Verify you have push access to the repository
- Check that `GITHUB_TOKEN` has `contents: write` permission
- Ensure tag was pushed to the remote repository

### Missing Artifacts
- Check that all jobs completed successfully
- Verify the publish paths in the workflow are correct
- Check GitHub Actions artifact retention settings

## Best Practices

1. **Always test locally before pushing tags**
2. **Use semantic versioning for tags** (MAJOR.MINOR.PATCH)
3. **Write descriptive commit messages** (they appear in changelogs)
4. **Monitor workflow runs** after pushing changes
5. **Keep workflows up to date** with latest GitHub Actions versions

## Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Semantic Versioning](https://semver.org/)
- [.NET CLI Documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/)
