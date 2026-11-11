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
    key = "shop-aks.tfstate"
  }
}

provider "azurerm" {
  features {
    resource_group {
      # AKS создаёт ресурсы (LB, public IP) вне Terraform — разрешаем удалять группу целиком.
      prevent_deletion_if_contains_resources = false
    }
  }
  subscription_id = var.subscription_id
}
