terraform {
  required_version = ">= 1.0"
  required_providers {
    azurerm = "~> 3.3"
  }
}

provider "azurerm" {
  features  {}
}

provider "azurerm" {
  alias           = "core"
  features        {}
  subscription_id = var.core_subscription
}

resource "azurerm_resource_group" "traduire_app" {
  name                  = "App03_traduire_app_rg"
  location              = var.region
  tags                  = {
    Application         = var.application_name
    Tier                = "App Components"
  }
}