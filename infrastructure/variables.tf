variable "location" {
  description = "The location for this application deployment"
  default     = "southcentralus"
}

variable "namespace" {
  description = "The namespace Trauduire will be deployed to"
  default     = "traduire"
}

variable "vm_sku" {
  description = "The VM type for the system node pool"
  default     = "Standard_D4ads_v5"
}

variable "zones" {
  description = "The availability zones for the AKS cluster"
  default     = ["2"]
}

variable "postgresql_user_name" {
  description = "Azure PostgreSQL User Name"
  type        = string
  default     = "manager"
}

variable "postgresql_database_name" {
  description = "PostgreSQL Database Name"
  type        = string
  default     = "transcriptsdb"
}

variable "service_bus_secret_name" {
  description = "Key Vault Secret Name for Service Bus Connection String"
  type        = string
  default     = "sbconnection"
}

variable "storage_secret_name" {
  description = "Key Vault Secret Name for Azure Storage Access Key"
  type        = string
  default     = "storagekey"
}

variable "postgresql_secret_name" {
  description = "Key Vault Secret Name for PostgreSQL Connection String"
  type        = string
  default     = "postgresqlconnection"
}

variable "cognitive_services_secret_name" {
  description = "Key Vault Secret Name for Cognitive Secret Primary Key"
  type        = string
  default     = "speech2textkey"
}

variable "pubsub_secret_name" {
  description = "Key Vault Secret Name for Web PubSub Primary Key"
  type        = string
  default     = "pubsubkey"
}

variable "github_repo_branch" {
  description = "The branched used for InfrastructureGitOps"
  default     = "main"
}
