output "postgresql_password" {
  value     = random_password.postgresql_user_password.result
  sensitive = true
}

output "cognitive_services_key" {
  value     = azurerm_cognitive_account.traduire_app.primary_access_key
  sensitive = true
}
