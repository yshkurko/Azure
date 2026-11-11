output "resource_group_name" {
  value = azurerm_resource_group.this.name
}

output "aks_name" {
  value = azurerm_kubernetes_cluster.this.name
}

output "acr_name" {
  value = azurerm_container_registry.this.name
}

output "acr_login_server" {
  value = azurerm_container_registry.this.login_server
}

output "sql_server_fqdn" {
  value = module.sql.server_fqdn
}

output "storage_account_name" {
  value = module.messaging.storage_account_name
}

output "servicebus_fqdn" {
  value = module.messaging.servicebus_fqdn
}

output "workload_identity_client_id" {
  value = azurerm_user_assigned_identity.workload.client_id
}

output "sql_create_users_script" {
  description = "Выполните в Query editor базы shopdb под своей учётной записью Entra ID."
  value       = <<-EOT
    CREATE USER [${azurerm_user_assigned_identity.workload.name}] FROM EXTERNAL PROVIDER;
    ALTER ROLE db_datareader ADD MEMBER [${azurerm_user_assigned_identity.workload.name}];
    ALTER ROLE db_datawriter ADD MEMBER [${azurerm_user_assigned_identity.workload.name}];
    ALTER ROLE db_ddladmin ADD MEMBER [${azurerm_user_assigned_identity.workload.name}];
  EOT
}
