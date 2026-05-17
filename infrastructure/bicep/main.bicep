targetScope = 'resourceGroup'

@description('Azure region for resources that require regional placement.')
param location string = 'eastus'

@description('Deployment environment name.')
@minLength(2)
param environmentName string = 'prod'

@description('Short lowercase app name used in Azure resource names.')
@minLength(2)
param appName string = 'cleansocial'

@description('Existing Azure Key Vault name.')
param keyVaultName string = 'kv-jdpeckham'

@description('Existing Azure DNS zone name.')
param dnsZoneName string = 'azure.jdpeckham.com'

@description('Custom host name for the Static Web App.')
param webHostName string = 'cleansocial.azure.jdpeckham.com'

@description('GitHub repository owner used for OIDC federation.')
param githubRepositoryOwner string = 'jpeckham'

@description('GitHub repository name used for OIDC federation.')
param githubRepositoryName string = 'clean-architecture-csharp'

@description('GitHub branch allowed to deploy production.')
param githubBranch string = 'main'

@description('GitHub environment allowed to deploy production.')
param githubEnvironmentName string = 'production'

@description('Initial API container image. The deployment workflow updates this to the current ACR image.')
param apiContainerImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@description('Container port exposed by SocialApp.Api.')
param apiContainerPort int = 8080

@description('Cosmos DB Mongo database name.')
param cosmosDatabaseName string = 'socialapp'

@description('Cosmos DB Mongo API server version.')
param cosmosMongoServerVersion string = '7.0'

@description('Static Web Apps SKU. Free keeps MVP hosting cost minimized.')
@allowed([
  'Free'
  'Standard'
])
param staticWebAppSku string = 'Free'

@description('Create Azure RBAC role assignments. Use true for local bootstrap and false for GitHub redeployments because the GitHub identity intentionally cannot assign roles.')
param manageRoleAssignments bool = true

var suffix = '${appName}-${environmentName}'
var compactSuffix = take(toLower(replace(suffix, '-', '')), 16)
var acrName = 'acrcleansocialprod'
var tags = {
  application: appName
  environment: environmentName
  managedBy: 'bicep'
}
var githubRepo = '${githubRepositoryOwner}/${githubRepositoryName}'
var dnsRelativeRecordName = replace(webHostName, '.${dnsZoneName}', '')
var mongoCollections = [
  'users'
  'sessions'
  'posts'
  'postFollows'
  'postBlocks'
  'pendingRegistrations'
  'verificationCodes'
  'rememberedDevices'
  'passwordResetTokens'
]

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource dnsZone 'Microsoft.Network/dnsZones@2018-05-01' existing = {
  name: dnsZoneName
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${suffix}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: 'cosmos-${suffix}'
  location: location
  tags: tags
  kind: 'MongoDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableFreeTier: true
    apiProperties: {
      serverVersion: cosmosMongoServerVersion
    }
    capabilities: [
      {
        name: 'EnableMongo'
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    backupPolicy: {
      type: 'Periodic'
      periodicModeProperties: {
        backupIntervalInMinutes: 240
        backupRetentionIntervalInHours: 8
      }
    }
  }
}

resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases@2024-05-15' = {
  parent: cosmos
  name: cosmosDatabaseName
  properties: {
    resource: {
      id: cosmosDatabaseName
    }
    options: {
      throughput: 400
    }
  }
}

resource cosmosCollections 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases/collections@2024-05-15' = [for collectionName in mongoCollections: {
  parent: cosmosDatabase
  name: collectionName
  properties: {
    resource: {
      id: collectionName
      shardKey: {
        _id: 'Hash'
      }
      indexes: [
        {
          key: {
            keys: [
              '_id'
            ]
          }
        }
      ]
    }
    options: {}
  }
}]

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'st${compactSuffix}media'
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

resource profileImages 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'profile-images'
  properties: {
    publicAccess: 'None'
  }
}

resource postImages 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'post-images'
  properties: {
    publicAccess: 'None'
  }
}

resource postVideos 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'post-videos'
  properties: {
    publicAccess: 'None'
  }
}

resource githubDeployIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-github-${suffix}'
  location: location
  tags: tags
}

resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-api-${suffix}'
  location: location
  tags: tags
}

resource githubBranchFederatedCredential 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: githubDeployIdentity
  name: 'github-${githubBranch}'
  properties: {
    audiences: [
      'api://AzureADTokenExchange'
    ]
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${githubRepo}:ref:refs/heads/${githubBranch}'
  }
}

resource githubEnvironmentFederatedCredential 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: githubDeployIdentity
  name: 'github-${githubEnvironmentName}'
  dependsOn: [
    githubBranchFederatedCredential
  ]
  properties: {
    audiences: [
      'api://AzureADTokenExchange'
    ]
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${githubRepo}:environment:${githubEnvironmentName}'
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${suffix}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: 'stapp-${suffix}'
  location: location
  tags: tags
  sku: {
    name: staticWebAppSku
    tier: staticWebAppSku
  }
  properties: {
    allowConfigFileUpdates: true
    stagingEnvironmentPolicy: 'Disabled'
  }
}

resource webCname 'Microsoft.Network/dnsZones/CNAME@2018-05-01' = {
  parent: dnsZone
  name: dnsRelativeRecordName
  properties: {
    TTL: 300
    CNAMERecord: {
      cname: staticWebApp.properties.defaultHostname
    }
  }
}

resource cosmosConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'cleansocial-${environmentName}-cosmos-mongo-connection-string'
  properties: {
    value: cosmos.listConnectionStrings().connectionStrings[0].connectionString
  }
}

resource passwordResetBaseUrlSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'cleansocial-${environmentName}-password-reset-base-url'
  properties: {
    value: 'https://${webHostName}/reset-password'
  }
}

var keyVaultSecretsUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
var storageBlobDataContributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var acrPullRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
var acrPushRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8311e382-0749-4cb8-b61a-304f252e45ec')
var contributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')

resource apiKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (manageRoleAssignments) {
  name: guid(keyVault.id, apiIdentity.id, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: keyVaultSecretsUserRoleId
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource apiKeyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: apiIdentity.properties.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

resource apiBlobAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (manageRoleAssignments) {
  name: guid(storage.id, apiIdentity.id, storageBlobDataContributorRoleId)
  scope: storage
  properties: {
    roleDefinitionId: storageBlobDataContributorRoleId
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource apiAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (manageRoleAssignments) {
  name: guid(acr.id, apiIdentity.id, acrPullRoleId)
  scope: acr
  properties: {
    roleDefinitionId: acrPullRoleId
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource githubAcrPush 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (manageRoleAssignments) {
  name: guid(acr.id, githubDeployIdentity.id, acrPushRoleId)
  scope: acr
  properties: {
    roleDefinitionId: acrPushRoleId
    principalId: githubDeployIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource githubResourceGroupContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (manageRoleAssignments) {
  name: guid(resourceGroup().id, githubDeployIdentity.id, contributorRoleId)
  properties: {
    roleDefinitionId: contributorRoleId
    principalId: githubDeployIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ca-api-${suffix}'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${apiIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        allowInsecure: false
        targetPort: apiContainerPort
        transport: 'auto'
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      registries: [
        {
          server: acr.properties.loginServer
          identity: apiIdentity.id
        }
      ]
      secrets: [
        {
          name: 'cosmos-mongo-connection-string'
          keyVaultUrl: cosmosConnectionSecret.properties.secretUri
          identity: apiIdentity.id
        }
        {
          name: 'password-reset-base-url'
          keyVaultUrl: passwordResetBaseUrlSecret.properties.secretUri
          identity: apiIdentity.id
        }
      ]
    }
    template: {
      scale: {
        minReplicas: 0
        maxReplicas: 3
      }
      containers: [
        {
          name: 'api'
          image: apiContainerImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:${apiContainerPort}'
            }
            {
              name: 'CosmosMongo__ConnectionString'
              secretRef: 'cosmos-mongo-connection-string'
            }
            {
              name: 'CosmosMongo__DatabaseName'
              value: cosmosDatabaseName
            }
            {
              name: 'Web__PasswordResetBaseUrl'
              secretRef: 'password-reset-base-url'
            }
            {
              name: 'Media__Provider'
              value: 'AzureBlob'
            }
            {
              name: 'AzureBlobMedia__AccountUri'
              value: storage.properties.primaryEndpoints.blob
            }
            {
              name: 'AzureBlobMedia__ProfileImagesContainer'
              value: profileImages.name
            }
            {
              name: 'AzureBlobMedia__PostMediaContainer'
              value: postImages.name
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: apiContainerPort
              }
              initialDelaySeconds: 20
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: apiContainerPort
              }
              initialDelaySeconds: 10
              periodSeconds: 15
            }
          ]
        }
      ]
    }
  }
  dependsOn: [
    apiKeyVaultAccessPolicy
  ]
}

output apiUrl string = 'https://${apiApp.properties.configuration.ingress.fqdn}'
output apiContainerAppName string = apiApp.name
output acrLoginServer string = acr.properties.loginServer
output acrName string = acr.name
output staticWebAppName string = staticWebApp.name
output staticWebAppDefaultHostName string = staticWebApp.properties.defaultHostname
output webHostName string = webHostName
output githubDeployClientId string = githubDeployIdentity.properties.clientId
output githubDeployPrincipalId string = githubDeployIdentity.properties.principalId
output mediaStorageAccountName string = storage.name
output cosmosAccountName string = cosmos.name
