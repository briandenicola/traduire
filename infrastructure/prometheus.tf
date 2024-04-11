resource "azurerm_monitor_workspace" "this" {
  name                = "${local.resource_name}-workspace"
  resource_group_name = azurerm_resource_group.traduire_app.name
  location            = azurerm_resource_group.traduire_app.location
}

resource "azurerm_monitor_data_collection_endpoint" "this" {
  name                          = "${local.resource_name}-ama-datacollection-ep"
  resource_group_name           = azurerm_resource_group.traduire_app.name
  location                      = azurerm_resource_group.traduire_app.location
  kind                          = "Linux"
  public_network_access_enabled = true
}

resource "azurerm_monitor_data_collection_rule" "azuremonitor" {
  name                = "${local.resource_name}-ama-datacollection-rules"
  resource_group_name = azurerm_resource_group.traduire_app.name
  location            = azurerm_resource_group.traduire_app.location
  depends_on = [
    azurerm_monitor_workspace.this,
    azurerm_monitor_data_collection_endpoint.this
  ]
  kind                        = "Linux"
  data_collection_endpoint_id = azurerm_monitor_data_collection_endpoint.this.id

  destinations {
    monitor_account {
      monitor_account_id = azurerm_monitor_workspace.this.id
      name               = "MonitoringAccount"
    }
  }

  data_flow {
    destinations = ["MonitoringAccount"]
    streams      = ["Microsoft-PrometheusMetrics"]
  }

  data_sources {
    prometheus_forwarder {
      name    = "PrometheusDataSource"
      streams = ["Microsoft-PrometheusMetrics"]
    }
  }
}

resource "azurerm_monitor_data_collection_rule_association" "this" {
  name                    = "${local.resource_name}-ama-datacollection-rules-association"
  target_resource_id      = azurerm_kubernetes_cluster.traduire_app.id
  data_collection_rule_id = azurerm_monitor_data_collection_rule.azuremonitor.id
}
