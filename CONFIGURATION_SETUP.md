# Configuration Setup Guide

This Azure Function project uses a comprehensive configuration system that supports different sources based on the environment:

## Configuration Sources (in order of precedence)

1. **Base configuration files** (`appsettings.json`, `appsettings.{Environment}.json`)
2. **User Secrets** (Development environment only)
3. **Environment Variables**
4. **Azure Key Vault** (Production environment only)

## Development Setup

### 1. User Secrets Setup

For development, sensitive configuration values should be stored in User Secrets instead of configuration files.

#### Initialize User Secrets

```bash
# Navigate to the AzureFunction project directory
cd AzureFunction

# Initialize user secrets (already configured with UserSecretsId in .csproj)
dotnet user-secrets init

# Add sensitive configuration values
dotnet user-secrets set "AppSettings:Database:ConnectionString" "Server=(localdb)\\MSSQLLocalDB;Database=AzureFunctionDev;Trusted_Connection=true"
dotnet user-secrets set "AppSettings:ExternalApi:ApiKey" "your-dev-api-key-here"
dotnet user-secrets set "AppSettings:Security:JwtSecret" "your-development-jwt-secret-min-32-chars"
```

#### Alternative: Manual User Secrets Setup

You can also manually edit the secrets file:

```bash
# Open the secrets file for editing
dotnet user-secrets list --project AzureFunction
```

Add this JSON content:

```json
{
  "AppSettings:Database:ConnectionString": "Server=(localdb)\\MSSQLLocalDB;Database=AzureFunctionDev;Trusted_Connection=true",
  "AppSettings:ExternalApi:ApiKey": "your-dev-api-key-here",
  "AppSettings:Security:JwtSecret": "your-development-jwt-secret-min-32-chars"
}
```

### 2. Environment Variables

You can also override any configuration value using environment variables:

```bash
# PowerShell
$env:AppSettings__Database__ConnectionString = "your-connection-string"
$env:AppSettings__ExternalApi__ApiKey = "your-api-key"

# Command Prompt
set AppSettings__Database__ConnectionString=your-connection-string
set AppSettings__ExternalApi__ApiKey=your-api-key
```

Note: Use double underscores (`__`) to represent nested configuration sections in environment variables.

## Production Setup

### Azure Key Vault Configuration

For production deployments, sensitive values should be stored in Azure Key Vault:

1. **Create an Azure Key Vault**
2. **Update `appsettings.Production.json`** with your Key Vault URL:

   ```json
   {
     "KeyVault": {
       "Url": "https://your-keyvault-name.vault.azure.net/"
     }
   }
   ```

3. **Add secrets to Key Vault** with these names:

   - `AppSettings--Database--ConnectionString`
   - `AppSettings--ExternalApi--ApiKey`
   - `AppSettings--Security--JwtSecret`

4. **Configure Azure Function App** with Managed Identity and appropriate Key Vault access policies.

### Environment Variables in Azure

You can also set configuration values as Application Settings in your Azure Function App:

- `AppSettings__Database__ConnectionString`
- `AppSettings__ExternalApi__ApiKey`
- `AppSettings__Security__JwtSecret`

## Configuration Validation

The application includes automatic configuration validation on startup. If required configuration values are missing or invalid, the application will fail to start with detailed error messages.

## Testing Configuration

Use the `/config` endpoint to verify your configuration is working correctly:

```bash
curl http://localhost:7071/config
```

This will return information about your current configuration (without exposing sensitive values).

## Configuration Structure

```csharp
public class AppSettings
{
    public string Environment { get; set; }
    public string ApplicationInsightsConnectionString { get; set; }
    public DatabaseSettings Database { get; set; }
    public ExternalApiSettings ExternalApi { get; set; }
    public SecuritySettings Security { get; set; }
}
```

## Security Best Practices

1. **Never commit sensitive values** to source control
2. **Use User Secrets** for local development
3. **Use Azure Key Vault** for production secrets
4. **Use environment variables** for non-sensitive configuration overrides
5. **Validate configuration** on application startup
6. **Rotate secrets regularly** in production
