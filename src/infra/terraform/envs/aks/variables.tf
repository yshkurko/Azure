variable "subscription_id" {
  type        = string
  description = "ID подписки Azure (az account show --query id -o tsv)."
}

variable "location" {
  type    = string
  default = "swedencentral"
}

variable "name_prefix" {
  type    = string
  default = "shop"
}

variable "sql_admin_login" {
  type        = string
  description = "Ваш email в Entra ID."
}

variable "client_ip" {
  type        = string
  default     = null
  description = "Ваш публичный IP для доступа к SQL с компьютера."
}

variable "node_vm_size" {
  type        = string
  default     = "Standard_B2s"
  description = "Самый дешёвый размер, подходящий для системного пула (2 vCPU, 4 GiB)."
}

variable "kubernetes_namespace" {
  type    = string
  default = "shop"
}

variable "kubernetes_service_account" {
  type    = string
  default = "shop-app"
}
