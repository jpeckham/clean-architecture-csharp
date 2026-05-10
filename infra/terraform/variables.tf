variable "environment" {
  description = "Deployment environment name."
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Azure region."
  type        = string
  default     = "eastus"
}

variable "name_prefix" {
  description = "Lowercase prefix used for Azure resource names."
  type        = string
  default     = "socialapp"
}

variable "api_container_image" {
  description = "Container image for SocialApp.Api."
  type        = string
}

variable "api_container_port" {
  description = "Port exposed by the API container."
  type        = number
  default     = 8080
}

variable "cosmos_mongo_server_version" {
  description = "Cosmos DB MongoDB API server version."
  type        = string
  default     = "7.0"
}

variable "cosmos_database_name" {
  description = "Mongo database name."
  type        = string
  default     = "socialapp"
}

variable "password_reset_base_url" {
  description = "Public Blazor reset-password URL used in emailed reset links."
  type        = string
  default     = "https://localhost:7278/reset-password"
}
