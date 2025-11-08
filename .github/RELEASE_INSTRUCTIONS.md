# Release Instructions for Aeon

This document describes how to create releases for the Aeon x86 Emulator using the automated GitHub Actions workflow.

## Overview

The repository has two GitHub Actions workflows:

1. **`.github/workflows/dotnet-ci.yml`** - Continuous Integration
   - Runs on every push to main, develop, and copilot/** branches
   - Runs on every pull request to main and develop
   - Builds and tests on Ubuntu, Windows, and macOS
   - Publishes artifacts for development testing

2. **`.github/workflows/release.yml`** - Release Automation
   - Runs when a version tag is pushed (e.g., `v1.0.0`)
   - Can also be triggered manually via GitHub UI
   - Creates a GitHub Release with changelog
   - Builds and uploads platform-specific binaries
   - Includes both framework-dependent and self-contained builds

## Creating a Release

### Method 1: Push a Tag (Recommended)

1. Ensure your code is ready for release on the main branch

2. Create and push a version tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

3. The release workflow will automatically:
   - Create a GitHub Release
   - Generate a changelog from commits since the last tag
   - Build cross-platform binaries
   - Upload artifacts to the release

### Method 2: Manual Trigger via GitHub UI

1. Go to the [Actions tab](../../actions) in the GitHub repository

2. Click on "Release" workflow in the left sidebar

3. Click the "Run workflow" button

4. Enter the tag name (e.g., `v1.0.0`)

5. Click "Run workflow"

## Version Tag Format

Tags must follow the semantic versioning format: `v<major>.<minor>.<patch>`

Examples:
- `v1.0.0` - Production release
- `v1.0.1` - Patch release
- `v1.1.0` - Minor version release
- `v2.0.0` - Major version release

Pre-release versions are also supported:
- `v1.0.0-alpha` - Alpha release (marked as pre-release)
- `v1.0.0-beta.1` - Beta release (marked as pre-release)
- `v1.0.0-rc.1` - Release candidate (marked as pre-release)

## Release Artifacts

Each release includes six artifacts:

### Framework-Dependent Builds (Require .NET 9 Runtime)
- `aeon-windows-x64.zip` - Windows x64
- `aeon-linux-x64.tar.gz` - Linux x64
- `aeon-macos-x64.tar.gz` - macOS x64

### Self-Contained Builds (Include .NET Runtime)
- `aeon-windows-x64-standalone.zip` - Windows x64
- `aeon-linux-x64-standalone.tar.gz` - Linux x64
- `aeon-macos-x64-standalone.tar.gz` - macOS x64

Each artifact includes a `VERSION.txt` file with build information.

## Release Notes

The release workflow automatically generates release notes that include:
- List of commits since the previous release
- Installation instructions
- Requirements and download links

You can edit the release notes after creation via the GitHub web interface.

## Troubleshooting

### Release Failed to Create
- Check that the tag follows the correct format (`v*.*.*`)
- Ensure you have push access to the repository
- Check the Actions tab for error logs

### Build Failed
- Verify the code builds successfully locally
- Check that all tests pass
- Review the build logs in the Actions tab

### Artifacts Not Uploaded
- Ensure the build completed successfully
- Check the workflow has `contents: write` permission
- Verify `GITHUB_TOKEN` has appropriate permissions

## Cleanup

To delete a release and its tag:

```bash
# Delete the remote tag
git push --delete origin v1.0.0

# Delete the local tag
git tag -d v1.0.0

# Manually delete the GitHub Release via the web interface
```

## Best Practices

1. **Test before releasing**: Always ensure the build passes on all platforms before creating a release tag
2. **Use semantic versioning**: Follow [semver](https://semver.org/) conventions
3. **Write good commit messages**: They will appear in the auto-generated changelog
4. **Update documentation**: Keep README.md and other docs up to date before releasing
5. **Review the release**: Check the generated release notes and artifacts before announcing
