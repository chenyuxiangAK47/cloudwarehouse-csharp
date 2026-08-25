# CloudWarehouse — Terraform IaC (Azure)
# Usage:
#   cd infra/terraform
#   terraform init
#   terraform plan -var="sql_admin_password=***"
#   terraform apply

terraform {
  required_version = ">= 1.5.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.110"
    }
  }
}

provider "azurerm" {
  features {}
}

variable "location" {
  type    = string
  default = "southeastasia"
}

variable "environment" {
  type    = string
  default = "demo"
}

variable "sql_admin_login" {
  type    = string
  default = "cwadmin"
}

variable "sql_admin_password" {
  type      = string
  sensitive = true
}

resource "azurerm_resource_group" "rg" {
  name     = "rg-cloudwarehouse-${var.environment}"
  location = var.location
}

resource "azurerm_application_insights" "ai" {
  name                = "ai-cloudwarehouse-${var.environment}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  application_type    = "web"
}

resource "azurerm_mssql_server" "sql" {
  name                         = "sql-cloudwarehouse-${var.environment}-${substr(md5(azurerm_resource_group.rg.id), 0, 6)}"
  resource_group_name          = azurerm_resource_group.rg.name
  location                     = azurerm_resource_group.rg.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_login
  administrator_login_password = var.sql_admin_password
  minimum_tls_version          = "1.2"
}

resource "azurerm_mssql_database" "db" {
  name      = "CloudWarehouse"
  server_id = azurerm_mssql_server.sql.id
  sku_name  = "Basic"
}

resource "azurerm_mssql_firewall_rule" "azure" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.sql.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

resource "azurerm_service_plan" "plan" {
  name                = "plan-cloudwarehouse-${var.environment}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  sku_name            = "B1"
}

resource "azurerm_linux_web_app" "app" {
  name                = "app-cloudwarehouse-${var.environment}-${substr(md5(azurerm_resource_group.rg.id), 0, 6)}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  service_plan_id     = azurerm_service_plan.plan.id
  https_only          = true

  site_config {
    always_on        = true
    application_stack {
      dotnet_version = "9.0"
    }
    ftps_state       = "Disabled"
    minimum_tls_version = "1.2"
  }

  app_settings = {
    APPINSIGHTS_INSTRUMENTATIONKEY             = azurerm_application_insights.ai.instrumentation_key
    APPLICATIONINSIGHTS_CONNECTION_STRING      = azurerm_application_insights.ai.connection_string
    ASPNETCORE_ENVIRONMENT                     = "Staging"
    ConnectionStrings__DefaultConnection       = "Server=tcp:${azurerm_mssql_server.sql.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.db.name};User ID=${var.sql_admin_login};Password=${var.sql_admin_password};Encrypt=True;TrustServerCertificate=False;"
  }
}

output "web_app_url" {
  value = "https://${azurerm_linux_web_app.app.default_hostname}"
}

output "sql_fqdn" {
  value = azurerm_mssql_server.sql.fully_qualified_domain_name
}
