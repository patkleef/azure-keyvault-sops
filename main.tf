resource "random_integer" "suffix" {
  min  = 100
  max  = 999
}

locals {
    location = "westeurope"
    tenant_id = ""
    subscription_id = ""
    resource_group_name = ""
    key_vault_name = ""
    key_name = ""
}

data "azurerm_client_config" "current" {
}

data "azurerm_role_definition" "keyvault_crypto_officer" {
  role_definition_id = "14b46e9e-c2b7-41b4-b07b-48a6ebf60603"
}

resource "azurerm_resource_group" "rg" {
  name     = local.resource_group_name
  location = local.location
}

resource "azurerm_key_vault" "kv" {
  name                        = local.key_vault_name
  location                    = local.location
  resource_group_name         = azurerm_resource_group.rg.name
  enable_rbac_authorization   = true
  tenant_id                   = local.tenant_id
  sku_name                    = "standard"

  network_acls {
    default_action = "Allow"
    bypass         = "AzureServices"
  }
}

resource "azurerm_role_assignment" "kv_crypto_officer" {
  lifecycle {
    ignore_changes = [role_definition_id]
  }
  scope              = azurerm_key_vault.kv.id
  role_definition_id = data.azurerm_role_definition.keyvault_crypto_officer.id
  principal_id       = data.azurerm_client_config.current.client_id
}

resource "azurerm_key_vault_key" "kv_sops_keys" {
  name         = local.key_name
  key_vault_id = azurerm_key_vault.kv.id
  key_type     = "RSA"
  key_size     = 2048

  key_opts = [
    "decrypt",
    "encrypt",
    "sign",
    "verify"
  ]
}
