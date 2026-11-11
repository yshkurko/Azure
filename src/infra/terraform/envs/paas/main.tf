data "azurerm_client_config" "current" {}

resource "random_string" "suffix" {
  length  = 5
  special = false
  upper   = false
}

locals {
  suffix = random_string.suffix.result
  tags = {
    course      = "azure-learning"
    environment = "paas"
    managed-by  = "terraform"
  }

  sql_connection_string = "Server=tcp:${module.sql.server_fqdn},1433;Database=${module.sql.database_name};Authentication=Active Directory Managed Identity;Encrypt=True"

  messaging_settings = {
    Messaging__Provider                            = var.messaging_provider
    Messaging__StorageQueue__ServiceUri            = module.messaging.queue_service_uri
    Messaging__ServiceBus__FullyQualifiedNamespace = module.messaging.servicebus_fqdn
  }

  common_settings = {
    APPLICATIONINSIGHTS_CONNECTION_STRING = module.monitoring.app_insights_connection_string
    ConnectionStrings__ShopDb             = local.sql_connection_string
  }
}

resource "azurerm_resource_group" "this" {
  name     = "rg-${var.name_prefix}-paas"
  location = var.location
  tags     = local.tags
}

module "network" {
  source              = "../../modules/network"
  name_prefix         = "${var.name_prefix}-paas"
  location            = var.location
  resource_group_name = azurerm_resource_group.this.name
  tags                = local.tags
}

module "sql" {
  source                     = "../../modules/sql"
  server_name                = "sql-${var.name_prefix}-${local.suffix}"
  location                   = var.location
  resource_group_name        = azurerm_resource_group.this.name
  admin_login                = var.sql_admin_login
  admin_object_id            = data.azurerm_client_config.current.object_id
  client_ip                  = var.client_ip
  private_endpoint_subnet_id = module.network.private_endpoints_subnet_id
  vnet_id                    = module.network.vnet_id
  tags                       = local.tags
}

module "messaging" {
  source                    = "../../modules/messaging"
  storage_account_name      = "st${var.name_prefix}${local.suffix}"
  servicebus_namespace_name = "sb-${var.name_prefix}-${local.suffix}"
  enable_service_bus        = var.messaging_provider == "ServiceBus"
  location                  = var.location
  resource_group_name       = azurerm_resource_group.this.name
  tags                      = local.tags
}

module "monitoring" {
  source              = "../../modules/monitoring"
  name_prefix         = "${var.name_prefix}-paas"
  location            = var.location
  resource_group_name = azurerm_resource_group.this.name
  tags                = local.tags
}

resource "azurerm_service_plan" "this" {
  name                = "asp-${var.name_prefix}"
  location            = var.location
  resource_group_name = azurerm_resource_group.this.name
  os_type             = "Linux"
  sku_name            = var.app_service_sku
  tags                = local.tags
}

resource "azurerm_static_web_app" "web" {
  name                = "stapp-${var.name_prefix}-${local.suffix}"
  location            = var.static_web_app_location
  resource_group_name = azurerm_resource_group.this.name
  sku_tier            = "Free"
  sku_size            = "Free"
  tags                = local.tags
}

locals {
  web_apps = {
    catalog = {
      settings = {
        Cors__AllowedOrigins__0 = "https://${azurerm_static_web_app.web.default_host_name}"
      }
    }
    orders = {
      settings = merge(local.messaging_settings, {
        Cors__AllowedOrigins__0 = "https://${azurerm_static_web_app.web.default_host_name}"
        Services__CatalogApiUrl = "https://app-${var.name_prefix}-catalog-${local.suffix}.azurewebsites.net/"
      })
    }
    processor = {
      settings = local.messaging_settings
    }
  }
}

resource "azurerm_linux_web_app" "apps" {
  for_each = local.web_apps

  name                      = "app-${var.name_prefix}-${each.key}-${local.suffix}"
  location                  = var.location
  resource_group_name       = azurerm_resource_group.this.name
  service_plan_id           = azurerm_service_plan.this.id
  https_only                = true
  virtual_network_subnet_id = module.network.app_subnet_id

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on                         = true
    ftps_state                        = "Disabled"
    minimum_tls_version               = "1.2"
    health_check_path                 = "/health"
    health_check_eviction_time_in_min = 10
    vnet_route_all_enabled            = true

    application_stack {
      dotnet_version = "10.0"
    }
  }

  app_settings = merge(local.common_settings, { for k, v in each.value.settings : k => v if v != null })

  tags = local.tags
}

# Доступ приложений к очередям по Managed Identity (без ключей в настройках).
resource "azurerm_role_assignment" "storage_queue" {
  for_each = toset(["orders", "processor"])

  scope                = module.messaging.storage_account_id
  role_definition_name = "Storage Queue Data Contributor"
  principal_id         = azurerm_linux_web_app.apps[each.key].identity[0].principal_id
}

resource "azurerm_role_assignment" "servicebus_sender" {
  count = var.messaging_provider == "ServiceBus" ? 1 : 0

  scope                = module.messaging.servicebus_namespace_id
  role_definition_name = "Azure Service Bus Data Sender"
  principal_id         = azurerm_linux_web_app.apps["orders"].identity[0].principal_id
}

resource "azurerm_role_assignment" "servicebus_receiver" {
  count = var.messaging_provider == "ServiceBus" ? 1 : 0

  scope                = module.messaging.servicebus_namespace_id
  role_definition_name = "Azure Service Bus Data Receiver"
  principal_id         = azurerm_linux_web_app.apps["processor"].identity[0].principal_id
}
