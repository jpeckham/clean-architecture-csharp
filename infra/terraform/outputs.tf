output "api_url" {
  description = "Public URL for the API container app."
  value       = "https://${azurerm_container_app.api.latest_revision_fqdn}"
}

output "static_web_app_default_host_name" {
  description = "Default host name for the Blazor Static Web App."
  value       = azurerm_static_web_app.web.default_host_name
}

output "cosmos_account_name" {
  description = "Cosmos DB account name."
  value       = azurerm_cosmosdb_account.main.name
}

output "acs_email_sender_address" {
  description = "Sender address configured for ACS Email."
  value       = "donotreply@${azurerm_email_communication_service_domain.main.mail_from_sender_domain}"
}

output "media_storage_account_name" {
  description = "Storage account used for SocialApp media blobs."
  value       = azurerm_storage_account.media.name
}
