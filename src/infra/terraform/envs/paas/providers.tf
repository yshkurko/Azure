terraform {
  required_version = ">= 1.9"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Remote state (урок 11): terraform init -backend-config=backend.hcl
  backend "azurerm" {
    key = "shop-paas.tfstate"
  }
}

provider "azurerm" {
  features {
    resource_group {
      # Учебный стенд: разрешаем удалять группу вместе с ресурсами, созданными вне Terraform.
      prevent_deletion_if_contains_resources = false
    }
  }
  subscription_id = var.subscription_id
}
