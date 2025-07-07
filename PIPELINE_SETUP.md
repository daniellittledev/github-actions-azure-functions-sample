# GitHub Actions CI/CD Pipeline Setup Guide

This document provides a setup guide for the GitHub Actions CI/CD pipeline implementing a **release-based deployment workflow** with separate build and deploy workflows.

## 🚀 Workflow Overview

The pipeline has been split into two main workflows:

1. **Build Workflow** (`build.yml`): Handles CI/CD for feature development and testing
2. **Release Deploy Workflow** (`release-deploy.yml`): Handles production deployments triggered by GitHub releases
3. **Deploy Workflow** (`deploy.yml`): Reusable deployment workflow used by both

### Key Features

- **Separation of Concerns**: Build/test logic separate from deployment logic
- **Release-based Deployments**: Production deployments only happen via GitHub releases
- **Automated Versioning**: Semantic-release handles version bumping and release creation
- **Prerelease Support**: Prerelease versions only deploy to test environment
- **Asset Management**: Build artifacts attached to GitHub releases for traceability

## 🚀 First Time Setup

### 1. Azure Prerequisites

#### Create Azure AD App Registrations

Create separate app registrations for test and production environments:

```pwsh
# Create app registration for test environment
az ad app create --display-name "github-actions-test"

# Create app registration for production environment
az ad app create --display-name "github-actions-prod"
```

#### Configure Federated Credentials

Set up OIDC federated credentials for GitHub Actions (replace placeholders with your values):

```pwsh
# For test environment - allows any ref for test deployments
az ad app federated-credential create `
  --id <TEST_APP_ID> `
  --parameters '{
    "name": "github-actions-test",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<YOUR_GITHUB_ORG>/<YOUR_REPO>:environment:test",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# For production environment - only releases can deploy to prod
az ad app federated-credential create `
  --id <PROD_APP_ID> `
  --parameters '{
    "name": "github-actions-prod",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<YOUR_GITHUB_ORG>/<YOUR_REPO>:environment:prod",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

#### Assign Azure Permissions

Grant the service principals Contributor access to their respective resource groups:

```pwsh
# Get service principal IDs
$TestSpId = az ad sp list --filter "appId eq '<TEST_APP_ID>'" --query '[0].id' -o tsv
$ProdSpId = az ad sp list --filter "appId eq '<PROD_APP_ID>'" --query '[0].id' -o tsv

# Assign Contributor role to test resource group
az role assignment create `
  --assignee $TestSpId `
  --role "Contributor" `
  --scope "/subscriptions/<TEST_SUBSCRIPTION_ID>/resourceGroups/<TEST_RESOURCE_GROUP>"

# Assign Contributor role to production resource group
az role assignment create `
  --assignee $ProdSpId `
  --role "Contributor" `
  --scope "/subscriptions/<PROD_SUBSCRIPTION_ID>/resourceGroups/<PROD_RESOURCE_GROUP>"
```

### 2. GitHub Repository Configuration

#### Required GitHub Secrets

Configure these secrets in your GitHub repository settings:

**Test Environment:**

- `AZURE_CLIENT_ID_TEST`: Azure AD App Registration Client ID for test
- `AZURE_SUBSCRIPTION_ID_TEST`: Azure subscription ID for test environment
- `AZURE_RESOURCE_GROUP_TEST`: Resource group name for test environment
- `AZURE_FUNCTIONAPP_NAME_TEST`: Function app name for test environment

**Production Environment:**

- `AZURE_CLIENT_ID_PROD`: Azure AD App Registration Client ID for production
- `AZURE_SUBSCRIPTION_ID_PROD`: Azure subscription ID for production environment
- `AZURE_RESOURCE_GROUP_PROD`: Resource group name for production environment
- `AZURE_FUNCTIONAPP_NAME_PROD`: Function app name for production environment

**Shared:**

- `AZURE_TENANT_ID`: Azure AD tenant ID (shared across environments)

### 3. Required Files

Ensure these workflow files exist in your repository:

1. **`.github/workflows/build.yml`**: Build and test pipeline
2. **`.github/workflows/release-deploy.yml`**: Release-triggered deployment workflow
3. **`.github/workflows/deploy.yml`**: Reusable deployment workflow
4. **`infra/main.bicep`**: Infrastructure template
5. **`package.json`**: Node.js dependencies for semantic-release
6. **`.releaserc.json`**: Semantic-release configuration

### 4. Semantic Release Setup

The project uses semantic-release for automated versioning. Ensure your commits follow conventional commit format:

- `feat:` - New features (minor version bump)
- `fix:` - Bug fixes (patch version bump)
- `BREAKING CHANGE:` - Breaking changes (major version bump)
- `chore:`, `docs:`, `style:` - No version bump

## 📖 Usage

### Triggering Deployments

#### Build Workflow Triggers

- **Push to `main`**: Builds, tests, and creates GitHub release with semantic-release
- **Push to `develop`**: Builds and tests (no release)
- **Pull Requests to `main`**: Builds and tests (no deployment)

#### Release Deploy Workflow Triggers

- **GitHub Release Published**: Automatically deploys to test, then production (if not prerelease)

### Deployment Process

1. **Build Stage**: Developer pushes to `main` → Build workflow runs → Creates GitHub release
2. **Test Deployment**: Release published → Downloads assets → Deploys to test environment
3. **Production Deployment**: If release is NOT a prerelease → Deploys to production

### Creating Releases

#### Automatic Releases (Recommended)

Push commits to `main` with conventional commit messages:

```bash
git commit -m "feat: add new user authentication"
git commit -m "fix: resolve memory leak in data processing"
git commit -m "feat!: change API response format

BREAKING CHANGE: Response format changed from XML to JSON"
```

#### Manual Releases

Create releases manually in GitHub UI, ensuring build artifacts are attached.

### Prerelease Handling

- **Prerelease versions** (e.g., `1.2.0-beta.1`) only deploy to test environment
- **Production releases** (e.g., `1.2.0`) deploy to both test and production
- Use `develop` branch with semantic-release to create prereleases

### Monitoring Deployments

- Check the **Actions** tab in GitHub for deployment status
- Review staging slot health before production promotion
- Monitor Azure Function logs for any issues

## Architecture

### Pipeline Overview

The CI/CD pipeline consists of three main workflows:

1. **Build Workflow** (`build.yml`): Code compilation, testing, and release creation
2. **Release Deploy Workflow** (`release-deploy.yml`): Release-triggered deployments
3. **Deploy Workflow** (calls `deploy.yml`): Reusable deployment logic

### Build Workflow Features

- **NuGet Package Caching**: Caches packages for faster builds
- **Infrastructure Change Detection**: Only processes infrastructure when files change
- **Bicep Linting**: Validates infrastructure templates
- **Build & Test**: Compiles code and runs unit tests with coverage
- **Semantic Release**: Automatically creates GitHub releases with version bumping
- **Asset Attachment**: Attaches function app and Bicep templates to releases

### Release Deploy Workflow Features

- **Asset Download**: Downloads build artifacts from GitHub release
- **Environment Detection**: Determines deployment targets based on release type
- **Prerelease Handling**: Skips production for prerelease versions
- **Artifact Management**: Re-uploads assets as workflow artifacts for deployment

### Deployment Workflow Features

- **Azure OIDC Authentication**: Secure, passwordless authentication
- **Blue-Green Deployment**: Zero-downtime deployments using staging slots
- **Conditional Infrastructure**: Only deploys infrastructure when changed
- **Storage Connection Management**: Automatically handles storage connections
- **Health Validation**: Checks deployment health before promoting to production

### Key Technical Decisions

#### Release-Based Deployment Strategy

- **GitHub Releases** act as the deployment trigger for production
- **Build artifacts** are attached to releases for complete traceability
- **Semantic versioning** ensures predictable version management
- **Prerelease support** allows testing of release candidates

#### Smart Infrastructure Deployment

- **Bicep templates** only deploy when `infra/**` files change
- **App settings** managed separately from infrastructure for flexibility
- **Storage connections** extracted from Bicep outputs or queried from existing resources

#### Staging Slot Strategy

1. Deploy to staging slot first
2. Run health checks on staging
3. Swap to production only after validation
4. Quick rollback capability through slot swap

#### Security Model

- **OIDC Authentication**: No stored credentials, uses federated identity
- **Environment Isolation**: Separate service principals for test/production
- **Least Privilege**: Minimal required permissions for each environment

#### Workflow Separation Benefits

- **Build Independence**: CI/CD can run without triggering deployments
- **Release Control**: Explicit control over when production deployments occur
- **Asset Versioning**: Build artifacts are versioned and stored with releases
- **Rollback Capability**: Easy to redeploy previous versions from releases
