resource "azurerm_dashboard_grafana" "this" {
  depends_on = [
    azurerm_monitor_workspace.this
  ]

  name                              = "${local.resource_name}-grafana"
  resource_group_name               = azurerm_resource_group.traduire_app.name
  location                          = azurerm_resource_group.traduire_app.location
  sku                               = "Standard"
  zone_redundancy_enabled           = local.grafana_az_support 
  api_key_enabled                   = true
  deterministic_outbound_ip_enabled = true
  public_network_access_enabled     = true
  grafana_major_version             = 10

  identity {
    type = "SystemAssigned"
  }

  azure_monitor_workspace_integrations {
    resource_id = azurerm_monitor_workspace.this.id
  }
}
