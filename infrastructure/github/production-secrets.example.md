# GitHub Production Secrets

Configure these repository or environment secrets for the `production` GitHub environment.

```text
AZURE_CLIENT_ID=Use the githubDeployClientId Bicep output
AZURE_TENANT_ID=7f4e14a4-417c-496f-82d9-9fb7940c3d17
AZURE_SUBSCRIPTION_ID=6d88cea2-aec5-4d58-88c4-4830a867b3cd
AZURE_STATIC_WEB_APPS_API_TOKEN=Use the token returned by az staticwebapp secrets list
```

`AZURE_CLIENT_ID` uses GitHub OIDC through the user-assigned identity created by Bicep. `AZURE_STATIC_WEB_APPS_API_TOKEN` is the one static secret because Azure Static Web Apps deployment uses its deployment token.
