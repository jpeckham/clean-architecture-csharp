# Environment Configuration

## API Production Settings

Container Apps sets:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
CosmosMongo__ConnectionString=secretref:cosmos-mongo-connection-string
CosmosMongo__DatabaseName=socialapp
Web__PasswordResetBaseUrl=secretref:password-reset-base-url
Media__Provider=AzureBlob
AzureBlobMedia__AccountUri=The primary Blob endpoint of the media storage account
AzureBlobMedia__ProfileImagesContainer=profile-images
AzureBlobMedia__PostMediaContainer=post-images
```

The Cosmos connection string and password reset URL are stored in Key Vault.

## Web Production Settings

The production workflow writes this file before publishing Blazor:

```json
{
  "ApiBaseAddress": "The apiUrl Bicep output, for example https://ca-api-cleansocial-prod.example.azurecontainerapps.io"
}
```

## Key Vault Secrets

Bicep creates:

```text
cleansocial-prod-cosmos-mongo-connection-string
cleansocial-prod-password-reset-base-url
```

## GitHub Environment Secrets

See `infrastructure/github/production-secrets.example.md`.
