
resource "azurerm_monitor_alert_prometheus_rule_group" "prometheus_node_recording_rule_group" {
  name                = "${local.resource_name}-NodeRecordingRuleGroup"
  location            = azurerm_resource_group.traduire_app.location
  resource_group_name = azurerm_resource_group.traduire_app.name
  interval            = "PT1M"
  scopes              = [azurerm_monitor_workspace.this.id]
  cluster_name        = local.aks_name
  rule_group_enabled  = true

  rule {
    enabled    = true    
    record     = "instance:node_num_cpu:sum"
    expression = "count without (cpu, mode) (node_cpu_seconds_total{job=\"node\",mode=\"idle\"})"
  }
  rule {
    enabled    = true    
    record     = "instance:node_cpu_utilisation:rate5m"
    expression = "1 - avg without (cpu) (sum without (mode) (rate(node_cpu_seconds_total{job=\"node\", mode=~\"idle|iowait|steal\"}[5m])))"
  }
  rule {
    enabled    = true    
    record     = "instance:node_load1_per_cpu:ratio"
    expression = "(  node_load1{job=\"node\"}/  instance:node_num_cpu:sum{job=\"node\"})"
  }
  rule {
    enabled    = true    
    record     = "instance:node_memory_utilisation:ratio"
    expression = "1 - ((node_memory_MemAvailable_bytes{job=\"node\"} or (node_memory_Buffers_bytes{job=\"node\"} + node_memory_Cached_bytes{job=\"node\"} + node_memory_MemFree_bytes{job=\"node\"} + node_memory_Slab_bytes{job=\"node\"}))/  node_memory_MemTotal_bytes{job=\"node\"})"
  }
  rule {
    enabled    = true
    record     = "instance:node_vmstat_pgmajfault:rate5m"
    expression = "rate(node_vmstat_pgmajfault{job=\"node\"}[5m])"
  }
  rule {
    enabled    = true    
    record     = "instance_device:node_disk_io_time_seconds:rate5m"
    expression = "rate(node_disk_io_time_seconds_total{job=\"node\", device!=\"\"}[5m])"
  }
  rule {
    enabled    = true
    record     = "instance_device:node_disk_io_time_weighted_seconds:rate5m"
    expression = "rate(node_disk_io_time_weighted_seconds_total{job=\"node\", device!=\"\"}[5m])"
  }
  rule {
    enabled    = true
    record     = "instance:node_network_receive_bytes_excluding_lo:rate5m"
    expression = "sum without (device) (  rate(node_network_receive_bytes_total{job=\"node\", device!=\"lo\"}[5m]))"
  }
  rule {
    enabled    = true
    record     = "instance:node_network_transmit_bytes_excluding_lo:rate5m"
    expression = "sum without (device) (  rate(node_network_transmit_bytes_total{job=\"node\", device!=\"lo\"}[5m]))"
  }
  rule {
    enabled    = true
    record     = "instance:node_network_receive_drop_excluding_lo:rate5m"
    expression = "sum without (device) (  rate(node_network_receive_drop_total{job=\"node\", device!=\"lo\"}[5m]))"
  }
  rule {
    enabled    = true
    record     = "instance:node_network_transmit_drop_excluding_lo:rate5m"
    expression = "sum without (device) (  rate(node_network_transmit_drop_total{job=\"node\", device!=\"lo\"}[5m]))"
  }
}
