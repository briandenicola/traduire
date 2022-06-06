data "azurerm_client_config" "current" {}

resource "random_password" "postgresql_user_password" {
  length           = 25
  special          = false
}

resource "tls_private_key" "k8s" {
  algorithm = "RSA"
  rsa_bits  = 4096
}

resource "azurerm_postgresql_flexible_server" "traduire_app" {
  name                   = var.postgresql_name
  resource_group_name    = azurerm_resource_group.traduire_app.name
  location               = azurerm_resource_group.traduire_app.location
  delegated_subnet_id    = data.azurerm_subnet.sql.id
  private_dns_zone_id    = data.azurerm_private_dns_zone.privatelink_postgres_database_azure_com.id
  version                = "12"
  administrator_login    = var.postgresql_user_name
  administrator_password = random_password.postgresql_user_password.result
  storage_mb             = 32768
  sku_name               = "GP_Standard_D2ds_v4"
}

resource "azurerm_postgresql_flexible_server_database" "transcription" {
  name                = var.postgresql_database_name
  server_id           = azurerm_postgresql_flexible_server.traduire_app.id
  collation           = "en_US.utf8"
  charset             = "utf8"
}

resource "azurerm_servicebus_namespace" "traduire_app" {
  name                      = var.service_bus_namespace_name
  location                  = azurerm_resource_group.traduire_app.location
  resource_group_name       = azurerm_resource_group.traduire_app.name
  sku                       = "Premium"
  capacity                  = 2
}

resource "azurerm_private_endpoint" "servicebus_namespace" {
  name                      = "${var.service_bus_namespace_name}-ep"
  resource_group_name       = azurerm_resource_group.traduire_app.name
  location                  = azurerm_resource_group.traduire_app.location
  subnet_id                 = data.azurerm_subnet.private-endpoints.id

  private_service_connection {
    name                           = "${var.service_bus_namespace_name}-ep"
    private_connection_resource_id = azurerm_servicebus_namespace.traduire_app.id
    subresource_names              = [ "namespace" ]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                          = data.azurerm_private_dns_zone.privatelink_servicebus_windows_net.name
    private_dns_zone_ids          = [ data.azurerm_private_dns_zone.privatelink_servicebus_windows_net.id ]
  }
}

resource "azurerm_cognitive_account" "traduire_app" {
  name                = "${var.application_name}-cogs01"
  resource_group_name = azurerm_resource_group.traduire_app.name
  location            = azurerm_resource_group.traduire_app.location
  kind                = "SpeechServices"

  sku_name            = "S0"
}

resource "azurerm_user_assigned_identity" "dapr_reader" {
  resource_group_name = azurerm_resource_group.traduire_app.name
  location            = azurerm_resource_group.traduire_app.location
  name                = "${var.application_name}-dapr-reader"
}

resource "azurerm_storage_account" "traduire_app" {
  name                      = var.mp3_storage_name
  resource_group_name       = azurerm_resource_group.traduire_app.name
  location                  = azurerm_resource_group.traduire_app.location
  account_tier              = "Standard"
  account_replication_type  = "LRS"
  account_kind              = "StorageV2"
  enable_https_traffic_only = true
  min_tls_version           = "TLS1_2"
}

resource "azurerm_storage_container" "mp3" {
  name                  = "mp3files"
  storage_account_name  = azurerm_storage_account.traduire_app.name
  container_access_type = "private"
}

resource "azurerm_role_assignment" "dapr_storage_data_reader" {
  scope                     = azurerm_storage_account.traduire_app.id
  role_definition_name      = "Storage Blob Data Reader" 
  principal_id              = azurerm_user_assigned_identity.dapr_reader.principal_id
  skip_service_principal_aad_check = true
}

resource "azurerm_key_vault" "traduire_app" {
  name                        = var.keyvault_name
  resource_group_name         = azurerm_resource_group.traduire_app.name
  location                    = azurerm_resource_group.traduire_app.location
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days  = 7
  purge_protection_enabled    = false

  sku_name = "standard"

  network_acls {
    bypass                    = "AzureServices"
    default_action            = "Deny"
    ip_rules                  = [ var.api_server_authorized_ip_ranges ]
  }

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = azurerm_user_assigned_identity.dapr_reader.principal_id 

    secret_permissions = [
      "List",
      "Get"
    ]
  }

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id 

    secret_permissions = [
      "Set",
      "Get",
      "Delete",
      "List"
    ]
  }
}

resource "azurerm_private_endpoint" "key_vault" {
  name                      = "${var.keyvault_name}-ep"
  resource_group_name       = azurerm_resource_group.traduire_app.name
  location                  = azurerm_resource_group.traduire_app.location
  subnet_id                 = data.azurerm_subnet.private-endpoints.id

  private_service_connection {
    name                           = "${var.keyvault_name}-ep"
    private_connection_resource_id = azurerm_key_vault.traduire_app.id
    subresource_names              = [ "vault" ]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                          = data.azurerm_private_dns_zone.privatelink_vaultcore_azure_net.name
    private_dns_zone_ids          = [ data.azurerm_private_dns_zone.privatelink_vaultcore_azure_net.id ]
  }
}
 
resource "azurerm_key_vault_secret" "service_bus_connection_string" {
  name         = var.service_bus_secret_name
  value        = azurerm_servicebus_namespace.traduire_app.default_primary_connection_string
  key_vault_id = azurerm_key_vault.traduire_app.id
}

resource "azurerm_key_vault_secret" "storage_secret_name" {
  name         = var.storage_secret_name
  value        = azurerm_storage_account.traduire_app.primary_access_key 
  key_vault_id = azurerm_key_vault.traduire_app.id
}
 
resource "azurerm_key_vault_secret" "postgresql_connection_string" {
  name         = var.postgresql_secret_name
  value        = "host=${var.postgresql_name}.postgres.database.azure.com user=${var.postgresql_user_name} password=${random_password.postgresql_user_password.result} port=5432 dbname=${var.postgresql_database_name} sslmode=require"
  key_vault_id = azurerm_key_vault.traduire_app.id
}

resource "azurerm_key_vault_secret" "azurerm_cognitive_account_key" {
  name         = var.cognitive_services_secret_name
  value        = azurerm_cognitive_account.traduire_app.primary_access_key
  key_vault_id = azurerm_key_vault.traduire_app.id
}

resource "azurerm_user_assigned_identity" "keda_sb_user" {
  resource_group_name = azurerm_resource_group.traduire_app.name
  location            = azurerm_resource_group.traduire_app.location
  name                = "${var.application_name}-keda-sb-owner"
}

resource "azurerm_role_assignment" "keda_sb_data_owner" {
  scope                     = azurerm_servicebus_namespace.traduire_app.id
  role_definition_name      = "Azure Service Bus Data Owner" 
  principal_id              = azurerm_user_assigned_identity.keda_sb_user.principal_id
  skip_service_principal_aad_check = true
}

resource "azurerm_web_pubsub" "traduire_app" {
  name                = var.pubsub_name
  location            = azurerm_resource_group.traduire_app.location
  resource_group_name = azurerm_resource_group.traduire_app.name

  sku      = "Free_F1"
  capacity = 1

}

resource "azurerm_key_vault_secret" "pub_sub_connection_string" {
  name         = var.pubsub_secret_name
  value        = azurerm_web_pubsub.traduire_app.primary_connection_string
  key_vault_id = azurerm_key_vault.traduire_app.id
}