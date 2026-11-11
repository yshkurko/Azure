variable "server_name" {
  type = string
}

variable "database_name" {
  type    = string
  default = "shopdb"
}

variable "location" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "admin_login" {
  type        = string
  description = "Отображаемое имя администратора Entra ID (например, ваш email)."
}

variable "admin_object_id" {
  type        = string
  description = "Object ID пользователя/группы Entra ID — администратора сервера."
}

variable "sku_name" {
  type        = string
  default     = "Basic"
  description = "Basic ~5 USD/мес при постоянной работе. Учебный стенд удаляется после занятия."
}

variable "client_ip" {
  type        = string
  default     = null
  description = "Ваш публичный IP для доступа к SQL с компьютера. null — без правила."
}

variable "enable_private_endpoint" {
  type    = bool
  default = true
}

variable "private_endpoint_subnet_id" {
  type    = string
  default = null
}

variable "vnet_id" {
  type    = string
  default = null
}

variable "tags" {
  type    = map(string)
  default = {}
}
