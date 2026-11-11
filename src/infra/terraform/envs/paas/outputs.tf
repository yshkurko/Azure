output "resource_group_name" {
  value = azurerm_resource_group.this.name
}

output "sql_server_fqdn" {
  value = module.sql.server_fqdn
}

output "web_app_names" {
  value = { for k, app in azurerm_linux_web_app.apps : k => app.name }
}

output "web_app_urls" {
  value = { for k, app in azurerm_linux_web_app.apps : k => "https://${app.default_hostname}" }
}

output "static_web_app_url" {
  value = "https://${azurerm_static_web_app.web.default_host_name}"
}

output "static_web_app_deployment_token" {
  value     = azurerm_static_web_app.web.api_key
  sensitive = true
}

output "sql_create_users_script" {
  description = "Выполните в Query editor базы shopdb под своей учётной записью Entra ID."
  value       = join("\n", flatten([
    for k, app in azurerm_linux_web_app.apps : [
      "CREATE USER [${app.name}] FROM EXTERNAL PROVIDER;",
      "ALTER ROLE db_datareader ADD MEMBER [${app.name}];",
      "ALTER ROLE db_datawriter ADD MEMBER [${app.name}];",
      "ALTER ROLE db_ddladmin ADD MEMBER [${app.name}];"
    ]
  ]))
}
