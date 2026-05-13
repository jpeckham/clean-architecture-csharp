locals {
  suffix               = "${var.name_prefix}-${var.environment}"
  storage_account_name = substr(lower(replace("st${var.name_prefix}${var.environment}media", "-", "")), 0, 24)
  tags = {
    application = "socialapp"
    environment = var.environment
  }
}

resource "azurerm_resource_group" "main" {
  name     = "rg-${local.suffix}"
  location = var.location
  tags     = local.tags
}

resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-${local.suffix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = local.tags
}

resource "azurerm_cosmosdb_account" "main" {
  name                 = "cosmos-${local.suffix}"
  location             = azurerm_resource_group.main.location
  resource_group_name  = azurerm_resource_group.main.name
  offer_type           = "Standard"
  kind                 = "MongoDB"
  mongo_server_version = var.cosmos_mongo_server_version
  tags                 = local.tags

  capabilities {
    name = "EnableMongo"
  }

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = azurerm_resource_group.main.location
    failover_priority = 0
  }
}

resource "azurerm_cosmosdb_mongo_database" "main" {
  name                = var.cosmos_database_name
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  throughput          = 400
}

resource "azurerm_cosmosdb_mongo_collection" "users" {
  name                = "users"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_mongo_database.main.name
  shard_key           = "_id"
}

resource "azurerm_cosmosdb_mongo_collection" "sessions" {
  name                = "sessions"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_mongo_database.main.name
  shard_key           = "_id"
}

resource "azurerm_cosmosdb_mongo_collection" "posts" {
  name                = "posts"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_mongo_database.main.name
  shard_key           = "_id"
}

resource "azurerm_cosmosdb_mongo_collection" "post_follows" {
  name                = "postFollows"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_mongo_database.main.name
  shard_key           = "_id"
}

resource "azurerm_cosmosdb_mongo_collection" "post_blocks" {
  name                = "postBlocks"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_mongo_database.main.name
  shard_key           = "_id"
}

resource "azurerm_cosmosdb_mongo_collection" "pending_registrations" {
  name                = "pendingRegistrations"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_mongo_database.main.name
  shard_key           = "_id"
}

resource "azurerm_cosmosdb_mongo_collection" "verification_codes" {
  name                = "verificationCodes"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_mongo_database.main.name
  shard_key           = "_id"
}

resource "azurerm_cosmosdb_mongo_collection" "remembered_devices" {
  name                = "rememberedDevices"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_mongo_database.main.name
  shard_key           = "_id"
}

resource "azurerm_cosmosdb_mongo_collection" "password_reset_tokens" {
  name                = "passwordResetTokens"
  resource_group_name = azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.main.name
  database_name       = azurerm_cosmosdb_mongo_database.main.name
  shard_key           = "_id"
}

resource "azurerm_storage_account" "media" {
  name                            = local.storage_account_name
  resource_group_name             = azurerm_resource_group.main.name
  location                        = azurerm_resource_group.main.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  allow_nested_items_to_be_public = false
  min_tls_version                 = "TLS1_2"
  tags                            = local.tags

  blob_properties {
    delete_retention_policy {
      days = var.media_blob_delete_retention_days
    }

    container_delete_retention_policy {
      days = var.media_blob_delete_retention_days
    }

    versioning_enabled = true
  }
}

resource "azurerm_storage_container" "profile_images" {
  name                  = var.profile_images_container_name
  storage_account_id    = azurerm_storage_account.media.id
  container_access_type = "private"
}

resource "azurerm_storage_container" "post_media" {
  name                  = var.post_media_container_name
  storage_account_id    = azurerm_storage_account.media.id
  container_access_type = "private"
}

resource "azurerm_communication_service" "main" {
  name                = "comm-${local.suffix}"
  resource_group_name = azurerm_resource_group.main.name
  data_location       = "United States"
  tags                = local.tags
}

resource "azurerm_email_communication_service" "main" {
  name                = "email-${local.suffix}"
  resource_group_name = azurerm_resource_group.main.name
  data_location       = "United States"
  tags                = local.tags
}

resource "azurerm_email_communication_service_domain" "main" {
  name              = "AzureManagedDomain"
  email_service_id  = azurerm_email_communication_service.main.id
  domain_management = "AzureManaged"
  tags              = local.tags
}

resource "azurerm_communication_service_email_domain_association" "main" {
  communication_service_id = azurerm_communication_service.main.id
  email_service_domain_id  = azurerm_email_communication_service_domain.main.id
}

resource "azurerm_container_app_environment" "main" {
  name                       = "cae-${local.suffix}"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
  tags                       = local.tags
}

resource "azurerm_container_app" "api" {
  name                         = "ca-api-${local.suffix}"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"
  tags                         = local.tags

  identity {
    type = "SystemAssigned"
  }

  secret {
    name  = "cosmos-mongo-connection-string"
    value = azurerm_cosmosdb_account.main.primary_mongodb_connection_string
  }

  secret {
    name  = "acs-connection-string"
    value = azurerm_communication_service.main.primary_connection_string
  }

  ingress {
    external_enabled = true
    target_port      = var.api_container_port

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    container {
      name   = "api"
      image  = var.api_container_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name        = "CosmosMongo__ConnectionString"
        secret_name = "cosmos-mongo-connection-string"
      }

      env {
        name  = "CosmosMongo__DatabaseName"
        value = azurerm_cosmosdb_mongo_database.main.name
      }

      env {
        name        = "AcsEmail__ConnectionString"
        secret_name = "acs-connection-string"
      }

      env {
        name  = "AcsEmail__SenderAddress"
        value = "donotreply@${azurerm_email_communication_service_domain.main.mail_from_sender_domain}"
      }

      env {
        name  = "Web__PasswordResetBaseUrl"
        value = var.password_reset_base_url
      }

      env {
        name  = "Media__Provider"
        value = "AzureBlob"
      }

      env {
        name  = "AzureBlobMedia__AccountUri"
        value = azurerm_storage_account.media.primary_blob_endpoint
      }

      env {
        name  = "AzureBlobMedia__ProfileImagesContainer"
        value = azurerm_storage_container.profile_images.name
      }

      env {
        name  = "AzureBlobMedia__PostMediaContainer"
        value = azurerm_storage_container.post_media.name
      }

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:${var.api_container_port}"
      }
    }

    min_replicas = 0
    max_replicas = 3
  }
}

resource "azurerm_role_assignment" "api_media_blob_contributor" {
  scope                = azurerm_storage_account.media.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_container_app.api.identity[0].principal_id
}

resource "azurerm_static_web_app" "web" {
  name                = "stapp-${local.suffix}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku_tier            = "Free"
  sku_size            = "Free"
  tags                = local.tags

  app_settings = {
    ApiBaseAddress = "https://${azurerm_container_app.api.latest_revision_fqdn}"
  }
}
