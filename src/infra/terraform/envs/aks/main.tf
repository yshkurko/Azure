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
    environment = "aks"
    managed-by  = "terraform"
  }
}

resource "azurerm_resource_group" "this" {
  name     = "rg-${var.name_prefix}-aks"
  location = var.location
  tags     = local.tags
}

module "network" {
  source              = "../../modules/network"
  name_prefix         = "${var.name_prefix}-aks"
  location            = var.location
  resource_group_name = azurerm_resource_group.this.name
  tags                = local.tags
}

module "sql" {
  source                     = "../../modules/sql"
  server_name                = "sql-${var.name_prefix}-aks-${local.suffix}"
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
  storage_account_name      = "st${var.name_prefix}aks${local.suffix}"
  servicebus_namespace_name = "sb-${var.name_prefix}-aks-${local.suffix}"
  enable_service_bus        = true
  location                  = var.location
  resource_group_name       = azurerm_resource_group.this.name
  tags                      = local.tags
}

resource "azurerm_container_registry" "this" {
  name                = "acr${var.name_prefix}${local.suffix}"
  location            = var.location
  resource_group_name = azurerm_resource_group.this.name
  sku                 = "Basic"
  admin_enabled       = false
  tags                = local.tags
}

resource "azurerm_kubernetes_cluster" "this" {
  name                = "aks-${var.name_prefix}"
  location            = var.location
  resource_group_name = azurerm_resource_group.this.name
  dns_prefix          = "aks-${var.name_prefix}-${local.suffix}"
  # Free: control plane бесплатный (без SLA). Платим только за узлы, диски, LB и IP.
  sku_tier = "Free"

  default_node_pool {
    name                        = "system"
    vm_size                     = var.node_vm_size
    node_count                  = 1
    vnet_subnet_id              = module.network.aks_subnet_id
    os_disk_size_gb             = 30
    os_disk_type                = "Managed"
    temporary_name_for_rotation = "systemtmp"

    upgrade_settings {
      max_surge = "10%"
    }
  }

  identity {
    type = "SystemAssigned"
  }

  network_profile {
    network_plugin      = "azure"
    network_plugin_mode = "overlay"
    pod_cidr            = "192.168.0.0/16"
    service_cidr        = "172.16.0.0/16"
    dns_service_ip      = "172.16.0.10"
    load_balancer_sku   = "standard"
  }

  oidc_issuer_enabled       = true
  workload_identity_enabled = true

  tags = local.tags
}

# Кластеру нужно право управлять подсетью (Load Balancer, IP узлов).
resource "azurerm_role_assignment" "aks_network" {
  scope                = module.network.aks_subnet_id
  role_definition_name = "Network Contributor"
  principal_id         = azurerm_kubernetes_cluster.this.identity[0].principal_id
}

# Узлы скачивают образы из ACR без паролей.
resource "azurerm_role_assignment" "aks_acr_pull" {
  scope                            = azurerm_container_registry.this.id
  role_definition_name             = "AcrPull"
  principal_id                     = azurerm_kubernetes_cluster.this.kubelet_identity[0].object_id
  skip_service_principal_aad_check = true
}

# Identity, от имени которой поды ходят в SQL / Storage / Service Bus (Workload Identity).
resource "azurerm_user_assigned_identity" "workload" {
  name                = "id-${var.name_prefix}-workload"
  location            = var.location
  resource_group_name = azurerm_resource_group.this.name
  tags                = local.tags
}

resource "azurerm_federated_identity_credential" "workload" {
  name                = "aks-${var.kubernetes_namespace}-${var.kubernetes_service_account}"
  resource_group_name = azurerm_resource_group.this.name
  parent_id           = azurerm_user_assigned_identity.workload.id
  audience            = ["api://AzureADTokenExchange"]
  issuer              = azurerm_kubernetes_cluster.this.oidc_issuer_url
  subject             = "system:serviceaccount:${var.kubernetes_namespace}:${var.kubernetes_service_account}"
}

resource "azurerm_role_assignment" "workload_storage_queue" {
  scope                = module.messaging.storage_account_id
  role_definition_name = "Storage Queue Data Contributor"
  principal_id         = azurerm_user_assigned_identity.workload.principal_id
}

resource "azurerm_role_assignment" "workload_servicebus" {
  for_each = toset(["Azure Service Bus Data Sender", "Azure Service Bus Data Receiver"])

  scope                = module.messaging.servicebus_namespace_id
  role_definition_name = each.key
  principal_id         = azurerm_user_assigned_identity.workload.principal_id
}
