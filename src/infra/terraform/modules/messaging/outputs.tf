output "storage_account_id" {
  value = azurerm_storage_account.this.id
}

output "storage_account_name" {
  value = azurerm_storage_account.this.name
}

output "queue_service_uri" {
  value = azurerm_storage_account.this.primary_queue_endpoint
}

output "servicebus_namespace_id" {
  value = var.enable_service_bus ? azurerm_servicebus_namespace.this[0].id : null
}

output "servicebus_fqdn" {
  value = var.enable_service_bus ? "${azurerm_servicebus_namespace.this[0].name}.servicebus.windows.net" : null
}
