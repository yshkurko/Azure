variable "storage_account_name" {
  type        = string
  description = "3-24 символа, только строчные буквы и цифры, глобально уникально."
}

variable "servicebus_namespace_name" {
  type = string
}

variable "enable_service_bus" {
  type    = bool
  default = true
}

variable "queue_name" {
  type    = string
  default = "orders"
}

variable "location" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "tags" {
  type    = map(string)
  default = {}
}
