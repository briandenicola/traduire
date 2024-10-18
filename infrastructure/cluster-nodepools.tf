resource "azurerm_kubernetes_cluster_node_pool" "traduire_app_node_pool" {
  depends_on = [
    azapi_update_resource.cluster_updates
  ]

  lifecycle {
    ignore_changes = [
      node_count
    ]
  }

  name                  = "traduire"
  kubernetes_cluster_id = azurerm_kubernetes_cluster.traduire_app.id
  vnet_subnet_id        = azurerm_subnet.kubernetes.id
  vm_size               = "Standard_B4ms"
  auto_scaling_enabled  = true
  mode                  = "User"
  os_sku                = "Mariner"
  os_disk_type          = "Ephemeral"
  os_disk_size_gb       = 30
  node_count            = 3
  min_count             = 3
  max_count             = 6

  upgrade_settings {
    max_surge = "25%"
  }

  node_taints = ["app=traduire:NoSchedule"]
}
