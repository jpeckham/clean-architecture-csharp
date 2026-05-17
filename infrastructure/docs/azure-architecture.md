# Clean Social Azure Architecture

Clean Social is hosted with a small serverless Azure footprint:

- Azure Static Web Apps serves the Blazor WebAssembly frontend at `cleansocial.azure.jdpeckham.com`.
- Azure Container Apps runs `SocialApp.Api` as a Linux container with public HTTPS ingress and scale-to-zero.
- Azure Container Registry Basic stores API images.
- Azure Cosmos DB Mongo API stores users, sessions, posts, follows, blocks, registrations, device codes, and password reset tokens.
- Azure Blob Storage stores private media blobs.
- Azure Key Vault stores runtime secrets such as the Cosmos Mongo connection string.
- Azure DNS holds the `cleansocial` CNAME record under `azure.jdpeckham.com`.

The business projects remain independent of Azure. Azure dependencies stay in infrastructure adapters and deployment configuration.

## Runtime Flow

1. Browser loads Blazor files from Static Web Apps.
2. Blazor reads `wwwroot/appsettings.json`, generated during deployment, for the Container Apps API URL.
3. The API reads Cosmos configuration, media configuration, and password reset URL from environment variables.
4. Container Apps resolves secret-backed environment variables from Key Vault using the API managed identity.
5. The API uses managed identity to access private Blob containers.

## Media Containers

The stack creates:

- `profile-images`
- `post-images`
- `post-videos`

The current API has one `AzureBlobMedia:PostMediaContainer` setting, so production points that setting to `post-images`. `post-videos` is reserved for splitting post image/video storage when the application gains separate container routing.

## DNS and HTTPS

The Bicep deployment creates a CNAME record:

```text
cleansocial.azure.jdpeckham.com -> Static Web Apps default hostname from the Bicep output
```

Run `infrastructure/scripts/configure-static-web-domain.ps1` after the first deployment to bind the custom hostname and let Static Web Apps issue the managed HTTPS certificate.
