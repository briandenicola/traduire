resource "azurerm_servicebus_namespace" "traduire_app" {
  name                         = local.sb_name
  location                     = azurerm_resource_group.traduire_app.location
  resource_group_name          = azurerm_resource_group.traduire_app.name
  sku                          = "Premium"
  premium_messaging_partitions = 1
  capacity                     = 1
}

resource "azurerm_private_endpoint" "servicebus_namespace" {
  name                = "${local.sb_name}-ep"
  resource_group_name = azurerm_resource_group.traduire_app.name
  location            = azurerm_resource_group.traduire_app.location
  subnet_id           = azurerm_subnet.private-endpoints.id

  private_service_connection {
    name                           = "${local.sb_name}-ep"
    private_connection_resource_id = azurerm_servicebus_namespace.traduire_app.id
    subresource_names              = ["namespace"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = azurerm_private_dns_zone.privatelink_servicebus_windows_net.name
    private_dns_zone_ids = [azurerm_private_dns_zone.privatelink_servicebus_windows_net.id]
  }
}
