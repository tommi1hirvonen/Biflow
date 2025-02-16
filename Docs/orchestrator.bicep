param tenantId string = '00000000-0000-0000-0000-000000000000'
param region string = 'northeurope'
param envId string = 'prod'
param shortEnvId string = 'p' // max 1 character
param appId string = 'buzz' // max 5 characters
param adminUser string = 'andy.admin@contoso.com'
param adminUserObjectId string = '00000000-0000-0000-0000-000000000000'
param firewallWhitelistIp string = '123.123.123.123'
@description('Expects TZ identifier. See https://en.wikipedia.org/wiki/List_of_tz_database_time_zones')
param websiteTimeZone string = 'Europe/Helsinki'
@secure()
param serviceApiKey string = '8bcf8e04-043c-4c01-ba6f-58e7704683aa'
param virtualMachineAdminUsername string = ''
@secure() 
param virtualMachineAdminPassword string = ''

param uiAdminUsername string = 'admin'
@secure()
param uiAdminPassword string = ''


// Application database
var sqlServerName = 'sql-${appId}-orchestration-${envId}'
var sqlDbName = 'sqldb-${appId}-orchestration-${envId}'

// Web apps
var appServicePlanName = 'asp-${appId}-orchestration-${envId}'
var uiWebAppName = 'app-${appId}-orchestration-ui-${envId}'
var schedulerWebAppName = 'app-${appId}-orchestration-scheduler-${envId}'
var apiWebAppName = 'app-${appId}-orchestration-api-${envId}'

var privateEndpointSchedulerName = 'pep-${appId}-orchestration-scheduler-${envId}'

// Execution service VM
var virtualMachineName = 'vm-${appId}-orchestration-${envId}'
var networkInterfaceName = 'vm-${appId}-orchestration-prod-nic'
var networkSecurityGroupName = 'vm-${appId}-orchestration-${envId}-nsg'
var publicIpAddressName = 'vm-${appId}-orchestration-${envId}-pip'
var osDiskName = 'vm-${appId}-orchestration-${envId}-osdisk'

// Log analytics

var logAnalyticsWorkspaceName = 'log-${appId}-orchestration-${envId}'
var schedulerAppInsightsName = 'appi-${appId}-orchestration-scheduler-${envId}'
var uiAppInsightsName = 'appi-${appId}-orchestration-ui-${envId}'
var apiAppInsightsName = 'appi-${appId}-orchestration-api-${envId}'

// Miscellaneous
var keyVaultName = 'kv-${appId}-orchestration-${shortEnvId}' // NOTE: Key Vault names can only be max 24 characters long.
var managedIdentityName = 'mi-${appId}-orchestration-${envId}'
var virtualNetworkName = 'vnet-${appId}-orchestration-${envId}'
var privateDnsZoneName = 'privatelink.azurewebsites.net'




resource keyVaultResource 'Microsoft.KeyVault/vaults@2024-04-01-preview' = {
  name: keyVaultName
  location: region
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    networkAcls: {
      bypass: 'AzureServices' // needed to be able to deploy secret values directly from bicep
      defaultAction: 'Deny'
      ipRules: [
        {
          value: firewallWhitelistIp
        }
      ]
      virtualNetworkRules: [
        {
          id: backendSubnet.id
        }
        {
          id: defaultSubnet.id
        }
      ]
    }
    accessPolicies: []
    enabledForDeployment: true
    enabledForDiskEncryption: true
    enabledForTemplateDeployment: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enableRbacAuthorization: true
    provisioningState: 'Succeeded'
    publicNetworkAccess: 'Enabled'
  }
}

resource managedIdentityResource 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' = {
  name: managedIdentityName
  location: region
}




// Virtual network

resource virtualNetworkResource 'Microsoft.Network/virtualNetworks@2024-01-01' = {
  name: virtualNetworkName
  location: region
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'backend'
        properties: {
          addressPrefix: '10.0.1.0/24'
          serviceEndpoints: [
            {
              service: 'Microsoft.KeyVault'
              locations: [
                '*'
              ]
            }
            {
              service: 'Microsoft.Web'
              locations: [
                '*'
              ]
            }
            {
              service: 'Microsoft.Sql'
              locations: [
                region
              ]
            }
            {
              service: 'Microsoft.AzureActiveDirectory'
              locations: [
                '*'
              ]
            }
          ]
          delegations: [
            {
              name: 'Microsoft.Web.serverFarms'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
              type: 'Microsoft.Network/virtualNetworks/subnets/delegations'
            }
          ]
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
        type: 'Microsoft.Network/virtualNetworks/subnets'
      }
      {
        name: 'default'
        properties: {
          addressPrefix: '10.0.0.0/24'
          serviceEndpoints: [
            {
              service: 'Microsoft.KeyVault'
              locations: [
                '*'
              ]
            }
            {
              service: 'Microsoft.Sql'
              locations: [
                region
              ]
            }
          ]
          delegations: []
          privateEndpointNetworkPolicies: 'Disabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
        type: 'Microsoft.Network/virtualNetworks/subnets'
      }
    ]
    virtualNetworkPeerings: []
    enableDdosProtection: false
  }
}

resource backendSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-01-01' existing = {
  parent: virtualNetworkResource
  name: 'backend'
}

resource defaultSubnet 'Microsoft.Network/virtualNetworks/subnets@2024-01-01' existing = {
  parent: virtualNetworkResource
  name: 'default'
}

// Application database

resource sqlServerResource 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: region
  properties: {
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'User'
      login: adminUser
      sid: adminUserObjectId
      tenantId: tenantId
      azureADOnlyAuthentication: true
    }
  }
}

resource sqlDbResource 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServerResource
  name: sqlDbName
  location: region
  sku: {
    name: 'S0'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 268435456000
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Geo'
    isLedgerOn: false
    preferredEnclaveType: 'VBS'
    availabilityZone: 'NoPreference'
  }
}

resource sqlServerFirewallRuleVpn 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServerResource
  name: 'Allow VPN'
  properties: {
    startIpAddress: firewallWhitelistIp
    endIpAddress: firewallWhitelistIp
  }
}

resource sqlServerVnetRuleBackend 'Microsoft.Sql/servers/virtualNetworkRules@2023-08-01-preview' = {
  parent: sqlServerResource
  name: 'allow_backend_vnet'
  properties: {
    virtualNetworkSubnetId: backendSubnet.id
    ignoreMissingVnetServiceEndpoint: false
  }
}

resource sqlServerVnetRuleService 'Microsoft.Sql/servers/virtualNetworkRules@2023-08-01-preview' = {
  parent: sqlServerResource
  name: 'allow_service_vnet'
  properties: {
    virtualNetworkSubnetId: defaultSubnet.id
    ignoreMissingVnetServiceEndpoint: false
  }
}




// Execution service VM

var vmPrivateIpAddress = '10.0.0.4'

resource publicIpAddressResource 'Microsoft.Network/publicIPAddresses@2024-01-01' = {
  name: publicIpAddressName
  location: region
  sku: {
    name: 'Standard'
    tier: 'Regional'
  }
  zones: [
    '1'
    '2'
    '3'
  ]
  properties: {
    publicIPAddressVersion: 'IPv4'
    publicIPAllocationMethod: 'Static'
    idleTimeoutInMinutes: 4
    ipTags: []
    ddosSettings: {
      protectionMode: 'VirtualNetworkInherited'
    }
  }
}

resource networkSecurityGroupResource 'Microsoft.Network/networkSecurityGroups@2024-01-01' = {
  name: networkSecurityGroupName
  location: region
  properties: {
    securityRules: [
      {
        name: 'SSH'
        type: 'Microsoft.Network/networkSecurityGroups/securityRules'
        properties: {
          protocol: 'TCP'
          sourcePortRange: '*'
          destinationPortRange: '22'
          sourceAddressPrefix: firewallWhitelistIp
          destinationAddressPrefix: '*'
          access: 'Allow'
          priority: 300
          direction: 'Inbound'
          sourcePortRanges: []
          destinationPortRanges: []
          sourceAddressPrefixes: []
          destinationAddressPrefixes: []
        }
      }
    ]
  }
}

resource networkInterfaceResource 'Microsoft.Network/networkInterfaces@2024-01-01' = {
  name: networkInterfaceName
  location: region
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig1'
        type: 'Microsoft.Network/networkInterfaces/ipConfigurations'
        properties: {
          privateIPAddress: vmPrivateIpAddress
          privateIPAllocationMethod: 'Dynamic'
          publicIPAddress: {
            id: publicIpAddressResource.id
            properties: {
              deleteOption: 'Detach'
            }
          }
          subnet: {
            id: defaultSubnet.id
          }
          primary: true
          privateIPAddressVersion: 'IPv4'
        }
      }
    ]
    dnsSettings: {
      dnsServers: []
    }
    enableAcceleratedNetworking: false
    enableIPForwarding: false
    disableTcpStateTracking: false
    networkSecurityGroup: {
      id: networkSecurityGroupResource.id
    }
    nicType: 'Standard'
    auxiliaryMode: 'None'
    auxiliarySku: 'None'
  }
}

resource virtualMachineResource 'Microsoft.Compute/virtualMachines@2024-07-01' = {
  name: virtualMachineName
  location: region
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResource.id}': {}
    }
  }
  properties: {
    hardwareProfile: {
      vmSize: 'Standard_B1s'
    }
    additionalCapabilities: {
      hibernationEnabled: false
    }
    storageProfile: {
      imageReference: {
        publisher: 'Canonical'
        offer: '0001-com-ubuntu-server-jammy'
        sku: '22_04-lts-gen2'
        version: 'latest'
      }
      osDisk: {
        osType: 'Linux'
        name: osDiskName
        createOption: 'FromImage'
        caching: 'ReadWrite'
        managedDisk: {
          storageAccountType: 'Premium_LRS'
        }
        deleteOption: 'Delete'
        diskSizeGB: 30
      }
      dataDisks: []
      diskControllerType: 'SCSI'
    }
    osProfile: {
      computerName: virtualMachineName
      adminUsername: virtualMachineAdminUsername
      adminPassword: virtualMachineAdminPassword
      linuxConfiguration: {
        disablePasswordAuthentication: false
        provisionVMAgent: true
        patchSettings: {
          patchMode: 'AutomaticByPlatform'
          automaticByPlatformSettings: {
            rebootSetting: 'Never'
            bypassPlatformSafetyChecksOnUserSchedule: false
          }
          assessmentMode: 'ImageDefault'
        }
        enableVMAgentPlatformUpdates: false
      }
      secrets: []
      allowExtensionOperations: true
    }
    securityProfile: {
      uefiSettings: {
        secureBootEnabled: true
        vTpmEnabled: true
      }
      securityType: 'TrustedLaunch'
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: networkInterfaceResource.id
          properties: {
            deleteOption: 'Detach'
          }
        }
      ]
    }
    diagnosticsProfile: {
      bootDiagnostics: {
        enabled: true
      }
    }
  }
}



// Applications

resource appServicePlanResource 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: region
  sku: {
    name: 'B2'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}


resource schedulerWebAppResource 'Microsoft.Web/sites@2023-12-01' = {
  name: schedulerWebAppName
  location: region
  kind: 'app,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResource.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlanResource.id
    siteConfig: {
      numberOfWorkers: 1
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: true
      publicNetworkAccess: 'Enabled'
      ipSecurityRestrictions: [
        {
          ipAddress: '${firewallWhitelistIp}/32'
          action: 'Allow'
          tag: 'Default'
          priority: 100
          name: 'Allow VPN'
        }
        {
          ipAddress: 'Any'
          action: 'Deny'
          priority: 2147483647
          name: 'Deny all'
          description: 'Deny all access'
        }
      ]
      ipSecurityRestrictionsDefaultAction: 'Deny'
      websiteTimeZone: websiteTimeZone
    }
    virtualNetworkSubnetId: backendSubnet.id
    keyVaultReferenceIdentity: managedIdentityResource.id
  }
}

resource uiWebAppResource 'Microsoft.Web/sites@2023-12-01' = {
  name: uiWebAppName
  location: region
  kind: 'app,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResource.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlanResource.id
    siteConfig: {
      numberOfWorkers: 1
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: true
      publicNetworkAccess: 'Enabled'
      ipSecurityRestrictions: [
        {
          ipAddress: '${firewallWhitelistIp}/32'
          action: 'Allow'
          tag: 'Default'
          priority: 100
          name: 'Allow VPN'
        }
        {
          ipAddress: 'Any'
          action: 'Deny'
          priority: 2147483647
          name: 'Deny all'
          description: 'Deny all access'
        }
      ]
      ipSecurityRestrictionsDefaultAction: 'Deny'
      websiteTimeZone: websiteTimeZone
    }
    httpsOnly: true
    virtualNetworkSubnetId: backendSubnet.id
    keyVaultReferenceIdentity: managedIdentityResource.id
  }
}

resource apiWebAppResource 'Microsoft.Web/sites@2023-12-01' = {
  name: apiWebAppName
  location: region
  kind: 'app,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResource.id}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlanResource.id
    siteConfig: {
      numberOfWorkers: 1
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: true
      publicNetworkAccess: 'Enabled'
      ipSecurityRestrictions: [
        {
          ipAddress: '${firewallWhitelistIp}/32'
          action: 'Allow'
          tag: 'Default'
          priority: 100
          name: 'Allow VPN'
        }
        {
          ipAddress: 'Any'
          action: 'Deny'
          priority: 2147483647
          name: 'Deny all'
          description: 'Deny all access'
        }
      ]
      ipSecurityRestrictionsDefaultAction: 'Deny'
      websiteTimeZone: websiteTimeZone
    }
    httpsOnly: true
    virtualNetworkSubnetId: backendSubnet.id
    keyVaultReferenceIdentity: managedIdentityResource.id
  }
}

var schedulerPrivateIpAddress = '10.0.0.5'

resource privateEndpointSchedulerResource 'Microsoft.Network/privateEndpoints@2024-01-01' = {
  name: privateEndpointSchedulerName
  location: region
  properties: {
    privateLinkServiceConnections: [
      {
        name: '${privateEndpointSchedulerName}-conn'
        properties: {
          privateLinkServiceId: schedulerWebAppResource.id
          groupIds: [
            'sites'
          ]
          privateLinkServiceConnectionState: {
            status: 'Approved'
            actionsRequired: 'None'
          }
        }
      }
    ]
    ipConfigurations: [
      {
        name: '${privateEndpointSchedulerName}-ip'
        properties: {
          groupId: 'sites'
          memberName: 'sites'
          privateIPAddress: schedulerPrivateIpAddress
        }
      }
    ]
    subnet: {
      id: defaultSubnet.id
    }
  }
}

resource privateDnsZoneResource 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: privateDnsZoneName
  location: 'global'
  properties: {}
}

resource privateDnsZoneVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: privateDnsZoneResource
  name: 'e1a286d85632'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: virtualNetworkResource.id
    }
  }
}

resource privateDnsZoneARecord 'Microsoft.Network/privateDnsZones/A@2024-06-01' = {
  parent: privateDnsZoneResource
  name: schedulerWebAppName
  properties: {
    ttl: 10
    aRecords: [
      {
        ipv4Address: schedulerPrivateIpAddress
      }
    ]
  }
}

// Log analytics

resource logAnalyticsWorkspaceResource 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: region
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: -1
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource schedulerAppInsightsResource 'Microsoft.Insights/components@2020-02-02' = {
  name: schedulerAppInsightsName
  location: region
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceResource.id
  }
}

resource uiAppInsightsResource 'Microsoft.Insights/components@2020-02-02' = {
  name: uiAppInsightsName
  location: region
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceResource.id
  }
}

resource apiAppInsightsResource 'Microsoft.Insights/components@2020-02-02' = {
  name: apiAppInsightsName
  location: region
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceResource.id
  }
}


// Role assignments

@description('This is the built-in Key Vault Administrator role. See See https://docs.microsoft.com/azure/role-based-access-control/built-in-roles')
resource keyVaultAdminRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '00482a5a-887f-4fb3-b363-3b7fe8e74483'
}

@description('This is the built-in Key Vault Crypto User role. See See https://docs.microsoft.com/azure/role-based-access-control/built-in-roles')
resource keyVaultCryptoUserRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '12338af0-0e69-4776-bea7-57ae8d297424'
}

@description('This is the built-in Key Vault Secrets User role. See See https://docs.microsoft.com/azure/role-based-access-control/built-in-roles')
resource keyVaultSecretsUserRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: subscription()
  name: '4633458b-17de-408a-b874-0445c86b69e6'
}

resource keyVaultAdmin 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVaultResource
  name: guid(keyVaultResource.id, adminUserObjectId, keyVaultAdminRoleDefinition.id)
  properties: {
    roleDefinitionId: keyVaultAdminRoleDefinition.id
    principalId: adminUserObjectId
    principalType: 'User'
  }
}

// Crypto user is needed if the SQL Server Always Encrypted encryption keys are stored in the Key Vault.
resource keyVaultCryptoUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVaultResource
  name: guid(keyVaultResource.id, managedIdentityResource.id, keyVaultCryptoUserRoleDefinition.id)
  properties: {
    roleDefinitionId: keyVaultCryptoUserRoleDefinition.id
    principalId: managedIdentityResource.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVaultResource
  name: guid(keyVaultResource.id, managedIdentityResource.id, keyVaultSecretsUserRoleDefinition.id)
  properties: {
    roleDefinitionId: keyVaultSecretsUserRoleDefinition.id
    principalId: managedIdentityResource.properties.principalId
    principalType: 'ServicePrincipal'
  }
}


// Key Vault Secrets

var apiKeySecretName = 'orchestrator-api-key'
var uiAdminPasswordSecretName = 'ui-admin-password'

resource serviceApiKeyResource 'Microsoft.KeyVault/vaults/secrets@2024-04-01-preview' = {
  parent: keyVaultResource
  name: apiKeySecretName
  properties: {
    attributes: {
      enabled: true
    }
    contentType: 'api key'
    value: serviceApiKey
  }
}

resource uiAdminPasswordResource 'Microsoft.KeyVault/vaults/secrets@2024-04-01-preview' = {
  parent: keyVaultResource
  name: uiAdminPasswordSecretName
  properties: {
    attributes: {
      enabled: true
    }
    contentType: 'password'
    value: uiAdminPassword
  }
}

resource vmAdminPasswordResource 'Microsoft.KeyVault/vaults/secrets@2024-04-01-preview' = {
  parent: keyVaultResource
  name: 'vm-admin-password'
  properties: {
    attributes: {
      enabled: true
    }
    contentType: 'password'
    value: virtualMachineAdminPassword
  }
}

// Web App appsettings

var apiKeyReference = '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${apiKeySecretName})'
var uiAdminPasswordReference = '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=${uiAdminPasswordSecretName})'

var appDbConnectionString = 'Server=tcp:${sqlServerResource.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDbName};Authentication=Active Directory Managed Identity;User Id=${managedIdentityResource.properties.clientId};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

var executionServiceUrl = 'http://${vmPrivateIpAddress}:4321'

resource schedulerAppSettings 'Microsoft.Web/sites/config@2022-09-01' = {
  name: 'appsettings'
  kind: 'string'
  parent: schedulerWebAppResource
  properties: {
    Authentication__ApiKey: apiKeyReference
    ConnectionStrings__AppDbContext: appDbConnectionString
    Executor__Type: 'WebApp'
    Executor__WebApp__ApiKey: apiKeyReference
    Executor__WebApp__Url: executionServiceUrl
    APPINSIGHTS_INSTRUMENTATIONKEY: schedulerAppInsightsResource.properties.InstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: schedulerAppInsightsResource.properties.ConnectionString
  }
  dependsOn: [
    serviceApiKeyResource
  ]
}

resource uiAppSettings 'Microsoft.Web/sites/config@2022-09-01' = {
  name: 'appsettings'
  kind: 'string'
  parent: uiWebAppResource
  properties: {
    AdminUser__Username: uiAdminUsername
    AdminUser__Password: uiAdminPasswordReference
    Authentication: 'BuiltIn'
    ConnectionStrings__AppDbContext: appDbConnectionString
    EnvironmentName: 'Environment Placeholder'
    Executor__SelfHosted__PollingIntervalMs: '5000'
    Executor__Type: 'WebApp'
    Executor__WebApp__ApiKey: apiKeyReference
    Executor__WebApp__Url: executionServiceUrl
    Scheduler__Type: 'WebApp'
    Scheduler__WebApp__ApiKey: apiKeyReference
    Scheduler__WebApp__Url: 'https://${schedulerWebAppResource.properties.defaultHostName}'
    APPINSIGHTS_INSTRUMENTATIONKEY: uiAppInsightsResource.properties.InstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: uiAppInsightsResource.properties.ConnectionString
  }
  dependsOn: [
    serviceApiKeyResource
  ]
}

resource apiAppSettings 'Microsoft.Web/sites/config@2022-09-01' = {
  name: 'appsettings'
  kind: 'string'
  parent: apiWebAppResource
  properties: {
    UserAuthentication: 'BuiltIn'
    ConnectionStrings__AppDbContext: appDbConnectionString
    Executor__SelfHosted__PollingIntervalMs: '5000'
    Executor__Type: 'WebApp'
    Executor__WebApp__ApiKey: apiKeyReference
    Executor__WebApp__Url: executionServiceUrl
    Scheduler__Type: 'WebApp'
    Scheduler__WebApp__ApiKey: apiKeyReference
    Scheduler__WebApp__Url: 'https://${schedulerWebAppResource.properties.defaultHostName}'
    APPINSIGHTS_INSTRUMENTATIONKEY: apiAppInsightsResource.properties.InstrumentationKey
    APPLICATIONINSIGHTS_CONNECTION_STRING: apiAppInsightsResource.properties.ConnectionString
  }
  dependsOn: [
    serviceApiKeyResource
  ]
}
