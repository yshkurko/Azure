resource "azurerm_storage_account" "this" {
  name                            = var.storage_account_name
  resource_group_name             = var.resource_group_name
  location                        = var.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  tags                            = var.tags
}

resource "azurerm_storage_queue" "orders" {
  name               = var.queue_name
  storage_account_id = azurerm_storage_account.this.id
}

resource "azurerm_storage_queue" "orders_poison" {
  name               = "${var.queue_name}-poison"
  storage_account_id = azurerm_storage_account.this.id
}

resource "azurerm_servicebus_namespace" "this" {
  count               = var.enable_service_bus ? 1 : 0
  name                = var.servicebus_namespace_name
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "Basic"
  # Только Entra ID / Managed Identity, без SAS-ключей.
  local_auth_enabled = false
  tags               = var.tags
}

resource "azurerm_servicebus_queue" "orders" {
  count                                = var.enable_service_bus ? 1 : 0
  name                                 = var.queue_name
  namespace_id                         = azurerm_servicebus_namespace.this[0].id
  max_delivery_count                   = 5
  dead_lettering_on_message_expiration = true
}
