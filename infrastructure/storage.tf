resource "azurerm_storage_account" "traduire_app" {
  name                       = local.mp3sa_name
  resource_group_name        = azurerm_resource_group.traduire_app.name
  location                   = azurerm_resource_group.traduire_app.location
  account_tier               = "Standard"
  account_replication_type   = "LRS"
  account_kind               = "StorageV2"
  https_traffic_only_enabled = true
  min_tls_version            = "TLS1_2"
}

resource "azurerm_storage_container" "mp3" {
  name                  = "mp3files"
  storage_account_id    = azurerm_storage_account.traduire_app.id
  container_access_type = "private"
}

resource "azurerm_private_endpoint" "storage_account" {
  name                = "${local.mp3sa_name}-ep"
  resource_group_name = azurerm_resource_group.traduire_app.name
  location            = azurerm_resource_group.traduire_app.location
  subnet_id           = azurerm_subnet.private-endpoints.id

  private_service_connection {
    name                           = "${local.mp3sa_name}-ep"
    private_connection_resource_id = azurerm_storage_account.traduire_app.id
    subresource_names              = ["blob"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = azurerm_private_dns_zone.privatelink_blob_core_windows_net.name
    private_dns_zone_ids = [azurerm_private_dns_zone.privatelink_blob_core_windows_net.id]
  }
}
