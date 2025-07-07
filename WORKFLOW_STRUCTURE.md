# Workflow Structure

This repository has been updated to use separate build and deploy workflows:

## Build Workflow (`build.yml`)

**Triggers:**

- Push to `main` or `develop` branches
- Pull requests to `main` branch
- Manual dispatch

**What it does:**

1. **Build & Test**: Compiles the .NET solution and runs tests
2. **Lint**: Validates Bicep infrastructure files (if changed)
3. **Package**: Creates deployment packages for the Azure Function
4. **Release** (main branch only): Uses semantic-release to create GitHub releases with attached assets
5. **Test Deployment** (PRs only): Deploys to test environment for validation

## Release Deploy Workflow (`release-deploy.yml`)

**Triggers:**

- GitHub release published

**What it does:**

1. **Download Assets**: Downloads function app and Bicep template assets from the GitHub release
2. **Deploy to Test**: Deploys to test environment first
3. **Deploy to Production**: Deploys to production environment after test deployment succeeds

## Key Benefits

1. **Separation of Concerns**: Build/test logic is separate from deployment logic
2. **Release-based Deployments**: Production deployments only happen when you create a GitHub release
3. **Automated Versioning**: Semantic-release handles version bumping and changelog generation
4. **PR Testing**: Pull requests are automatically deployed to test environment
5. **Asset Management**: Build artifacts are attached to GitHub releases for traceability

## Semantic Release

The project uses semantic-release for automated versioning and release management:

- Follows conventional commits for version determination
- Automatically creates GitHub releases with changelogs
- Attaches build artifacts to releases
- Updates version in package.json and creates CHANGELOG.md

## Migration Notes

The original `ci-cd.yml` has been split into:

- `build.yml` - Handles CI/CD for feature development and testing
- `release-deploy.yml` - Handles production deployments triggered by releases

The `deploy.yml` workflow remains unchanged and is used by both new workflows.
