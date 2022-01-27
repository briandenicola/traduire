data "azurerm_virtual_network" "traduire_core" {
  name                = var.vnet_name
  resource_group_name = var.vnet_rg
}

data "azurerm_subnet" "private-endpoints" {
  name                  = "private-endpoints"
  resource_group_name   = data.azurerm_virtual_network.traduire_core.resource_group_name
  virtual_network_name  = data.azurerm_virtual_network.traduire_core.name
}

data "azurerm_private_dns_zone" "privatelink_blob_core_windows_net" {
  name                      = "privatelink.blob.core.windows.net"
  resource_group_name       = var.dns_rg
  provider                  = azurerm.core
}

data "azurerm_private_dns_zone" "privatelink_vaultcore_azure_net" {
  name                      = "privatelink.vaultcore.azure.net"
  resource_group_name       = var.dns_rg
  provider                  = azurerm.core
}

data "azurerm_private_dns_zone" "privatelink_postgres_database_azure_com" {
  name                      = "privatelink.postgres.database.azure.com"
  resource_group_name       = var.dns_rg
  provider                  = azurerm.core
}

data "azurerm_private_dns_zone" "privatelink_servicebus_windows_net" {
  name                      = "privatelink.servicebus.windows.net"
  resource_group_name       = var.dns_rg
  provider                  = azurerm.core
}
