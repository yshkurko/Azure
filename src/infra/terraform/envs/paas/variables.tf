variable "subscription_id" {
  type        = string
  description = "ID подписки Azure (az account show --query id -o tsv)."
}

variable "location" {
  type    = string
  default = "swedencentral"
}

variable "static_web_app_location" {
  type        = string
  default     = "westeurope"
  description = "Static Web Apps доступен не во всех регионах (Sweden Central не поддерживается)."
}

variable "name_prefix" {
  type    = string
  default = "shop"
}

variable "sql_admin_login" {
  type        = string
  description = "Ваш email в Entra ID (az ad signed-in-user show --query userPrincipalName -o tsv)."
}

variable "client_ip" {
  type        = string
  default     = null
  description = "Ваш публичный IP для доступа к SQL (например, из https://ifconfig.me)."
}

variable "messaging_provider" {
  type    = string
  default = "StorageQueue"

  validation {
    condition     = contains(["StorageQueue", "ServiceBus"], var.messaging_provider)
    error_message = "messaging_provider must be StorageQueue or ServiceBus."
  }
}

variable "app_service_sku" {
  type        = string
  default     = "B1"
  description = "B1 — самый дешёвый план с VNet Integration и Always On."
}
