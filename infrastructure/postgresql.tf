resource "random_password" "postgresql_user_password" {
  length  = 25
  special = false
}

resource "azurerm_postgresql_flexible_server" "traduire_app" {
  depends_on = [
    azurerm_private_dns_zone_virtual_network_link.privatelink_postgres_database_azure_com
  ]
  name                          = local.sql_name
  resource_group_name           = azurerm_resource_group.traduire_app.name
  location                      = azurerm_resource_group.traduire_app.location
  delegated_subnet_id           = azurerm_subnet.sql.id
  private_dns_zone_id           = azurerm_private_dns_zone.privatelink_postgres_database_azure_com.id
  version                       = "15"
  administrator_login           = var.postgresql_user_name
  administrator_password        = random_password.postgresql_user_password.result
  public_network_access_enabled = false
  storage_mb                    = 32768
  sku_name                      = "GP_Standard_D2ds_v4"
  zone                          = "2"
}

resource "azurerm_postgresql_flexible_server_database" "transcription" {
  name      = var.postgresql_database_name
  server_id = azurerm_postgresql_flexible_server.traduire_app.id
  collation = "en_US.utf8"
  charset   = "utf8"
}
