data "azurerm_client_config" "current" {}

resource "random_password" "postgresql_user_password" {
  length           = 25
  special          = false
}

resource "azurerm_postgresql_flexible_server" "traduire_app" {
  name                   = var.postgresql_name
  resource_group_name    = azurerm_resource_group.traduire_app.name
  location               = azurerm_resource_group.traduire_app.location
  delegated_subnet_id    = azurerm_subnet.sql.id
  private_dns_zone_id    = azurerm_private_dns_zone.privatelink_postgres_database_azure_com.id
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
  subnet_id                 = azurerm_subnet.private-endpoints.id

  private_service_connection {
    name                           = "${var.service_bus_namespace_name}-ep"
    private_connection_resource_id = azurerm_servicebus_namespace.traduire_app.id
    subresource_names              = [ "namespace" ]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                          = azurerm_private_dns_zone.privatelink_servicebus_windows_net.name
    private_dns_zone_ids          = [ azurerm_private_dns_zone.privatelink_servicebus_windows_net.id ]
  }
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

resource "azurerm_private_endpoint" "storage_account" {
  name                      = "${var.mp3_storage_name}-ep"
  resource_group_name       = azurerm_resource_group.traduire_app.name
  location                  = azurerm_resource_group.traduire_app.location
  subnet_id                 = azurerm_subnet.private-endpoints.id

  private_service_connection {
    name                           = "${var.mp3_storage_name}-ep"
    private_connection_resource_id = azurerm_storage_account.traduire_app.id
    subresource_names              = [ "blob" ]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                          = azurerm_private_dns_zone.privatelink_blob_core_windows_net.name
    private_dns_zone_ids          = [ azurerm_private_dns_zone.privatelink_blob_core_windows_net.id ]
  }
}

resource "azurerm_kubernetes_cluster" "traduire_app" {
  name                      = var.aks_name
  resource_group_name       = azurerm_resource_group.traduire_app.name
  location                  = azurerm_resource_group.traduire_app.location
  node_resource_group       = "${azurerm_resource_group.traduire_app.name}_k8s_nodes"
  dns_prefix                = var.aks_name
  sku_tier                  = "Paid"
  api_server_authorized_ip_ranges = [ var.api_server_authorized_ip_ranges ]

  identity {
    type                    = "SystemAssigned"
  }

  default_node_pool  {
    name                    = "default"
    node_count              = 3
    vm_size                = "Standard_DS2_v2"
    os_disk_size_gb         = 30
    vnet_subnet_id          = azurerm_subnet.kubernetes.id
    type                    = "VirtualMachineScaleSets"
    enable_auto_scaling     = true
    min_count               = 3
    max_count               = 10
    max_pods                = 40
  }

  network_profile {
    dns_service_ip          = "10.190.0.10"
    service_cidr            = "10.190.0.0/16"
    docker_bridge_cidr      = "172.17.0.1/16"
    network_plugin          = "azure"
    load_balancer_sku       = "standard"
  }

  oms_agent {
    log_analytics_workspace_id = azurerm_log_analytics_workspace.traduire_logs.id
  }

  microsoft_defender {
    log_analytics_workspace_id = azurerm_log_analytics_workspace.traduire_logs.id
  }
}

resource "azurerm_kubernetes_cluster_node_pool" "traduire_app_node_pool" {
  name                  = "traduire"
  kubernetes_cluster_id = azurerm_kubernetes_cluster.traduire_app.id
  vm_size               = "Standard_B4ms"
  enable_auto_scaling   = true
  node_count            = 3
  min_count             = 3
  max_count             = 10

  node_taints           = [ "app=traduire:NoSchedule" ]
}

resource "azurerm_role_assignment" "acr_pullrole_node" {
  scope                     = azurerm_container_registry.traduire_acr.id
  role_definition_name      = "AcrPull"
  principal_id              = azurerm_kubernetes_cluster.traduire_app.kubelet_identity.0.object_id 
  skip_service_principal_aad_check = true
}

resource "azurerm_role_assignment" "acr_pullrole_cluster" {
  scope                     = azurerm_container_registry.traduire_acr.id
  role_definition_name      = "AcrPull"
  principal_id              = azurerm_kubernetes_cluster.traduire_app.identity.0.principal_id
  skip_service_principal_aad_check = true
}

resource "azurerm_role_assignment" "network_contributor_cluster" {
  scope                     = azurerm_resource_group.traduire_core.id
  role_definition_name      = "Network Contributor"
  principal_id              = azurerm_kubernetes_cluster.traduire_app.kubelet_identity.0.object_id 
  skip_service_principal_aad_check = true
}

resource "azurerm_role_assignment" "msi_operator_cluster_node_rg" {
  scope                     = "/subscriptions/${data.azurerm_client_config.current.subscription_id}/resourceGroups/${azurerm_kubernetes_cluster.traduire_app.node_resource_group}"
  role_definition_name      = "Managed Identity Operator"
  principal_id              = azurerm_kubernetes_cluster.traduire_app.kubelet_identity.0.object_id
  skip_service_principal_aad_check = true
}

resource "azurerm_role_assignment" "msi_operator_cluster_msi_rg" {
  scope                     = azurerm_resource_group.traduire_app.id
  role_definition_name      = "Managed Identity Operator"
  principal_id              = azurerm_kubernetes_cluster.traduire_app.kubelet_identity.0.object_id
  skip_service_principal_aad_check = true
}

resource "azurerm_role_assignment" "vm_contributor_cluster" {
  scope                     = "/subscriptions/${data.azurerm_client_config.current.subscription_id}/resourceGroups/${azurerm_kubernetes_cluster.traduire_app.node_resource_group}"
  role_definition_name      = "Virtual Machine Contributor" 
  principal_id              = azurerm_kubernetes_cluster.traduire_app.kubelet_identity.0.object_id
  skip_service_principal_aad_check = true
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
  subnet_id                 = azurerm_subnet.private-endpoints.id

  private_service_connection {
    name                           = "${var.keyvault_name}-ep"
    private_connection_resource_id = azurerm_key_vault.traduire_app.id
    subresource_names              = [ "vault" ]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                          = azurerm_private_dns_zone.privatelink_vaultcore_azure_net.name
    private_dns_zone_ids          = [ azurerm_private_dns_zone.privatelink_vaultcore_azure_net.id ]
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