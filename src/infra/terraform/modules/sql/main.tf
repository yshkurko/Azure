resource "azurerm_mssql_server" "this" {
  name                          = var.server_name
  resource_group_name           = var.resource_group_name
  location                      = var.location
  version                       = "12.0"
  minimum_tls_version           = "1.2"
  public_network_access_enabled = true

  # Только вход через Entra ID — без SQL-логинов и паролей.
  azuread_administrator {
    login_username              = var.admin_login
    object_id                   = var.admin_object_id
    azuread_authentication_only = true
  }

  tags = var.tags
}

resource "azurerm_mssql_database" "this" {
  name        = var.database_name
  server_id   = azurerm_mssql_server.this.id
  sku_name    = var.sku_name
  max_size_gb = 2
  tags        = var.tags
}

# Доступ с вашего компьютера (Query editor, sqlcmd) — только с одного IP.
resource "azurerm_mssql_firewall_rule" "client" {
  count            = var.client_ip == null ? 0 : 1
  name             = "client-ip"
  server_id        = azurerm_mssql_server.this.id
  start_ip_address = var.client_ip
  end_ip_address   = var.client_ip
}

resource "azurerm_private_dns_zone" "sql" {
  count               = var.enable_private_endpoint ? 1 : 0
  name                = "privatelink.database.windows.net"
  resource_group_name = var.resource_group_name
  tags                = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "sql" {
  count                 = var.enable_private_endpoint ? 1 : 0
  name                  = "sql-dns-link"
  resource_group_name   = var.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.sql[0].name
  virtual_network_id    = var.vnet_id
  tags                  = var.tags
}

resource "azurerm_private_endpoint" "sql" {
  count               = var.enable_private_endpoint ? 1 : 0
  name                = "pe-${var.server_name}"
  location            = var.location
  resource_group_name = var.resource_group_name
  subnet_id           = var.private_endpoint_subnet_id

  private_service_connection {
    name                           = "sql"
    private_connection_resource_id = azurerm_mssql_server.this.id
    subresource_names              = ["sqlServer"]
    is_manual_connection           = false
  }

  private_dns_zone_group {
    name                 = "sql"
    private_dns_zone_ids = [azurerm_private_dns_zone.sql[0].id]
  }

  tags = var.tags
}
