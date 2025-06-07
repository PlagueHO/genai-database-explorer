targetScope = 'subscription'

@description('The location to deploy the resources into.')
@allowed([
  'AustraliaEast'
  'CentralUS'
  'EastUS'
  'EastUS2'
  'FranceCentral'
  'JapanEast'
  'NorthCentralUS'
  'NorwayEast'
  'SouthCentralUS'
  'SwedenCentral'
  'SwitzerlandNorth'
  'UKSouth'
  'WestEurope'
  'WestUS'
  'WestUS3'
])
param location string

@description('The base name that will prefixed to all Azure resources deployed to ensure they are unique.')
param baseResourceName string

@description('The name of the resource group that will contain all the resources.')
param resourceGroupName string

@description('The SQL logical server administrator username.')
param sqlServerUsername string

@description('The SQL logical server administrator password.')
@secure()
param sqlServerPassword string

// tags that should be applied to all resources.
var tags = {
  // Tag all resources with the environment name.
  project: 'genai-database-explorer'
  baseResourceName: baseResourceName
}

var logAnalyticsWorkspaceName = '${baseResourceName}-law'
var applicationInsightsName = '${baseResourceName}-appinsights'
var openAiServiceName = '${baseResourceName}-openai'
// var aiSearchName = '${baseResourceName}-aisearch'

var openAiModelDeployments = [
  {
    name: 'gpt-4.1'
    modelName: 'gpt-4.1'
    version: '2025-04-14'
    sku: 'GlobalStandard'
    capacity: 50
  }
  {
    name: 'gpt-4.1-mini'
    modelName: 'gpt-4.1-mini'
    version: '2025-04-14'
    sku: 'GlobalStandard'
    capacity: 200
  }
  {
    name: 'embedding'
    modelName: 'text-embedding-3-large'
    version: '1'
    sku: 'Standard'
    capacity: 120
  }
]

// The application resources that are deployed into the application resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// --------- MONITORING RESOURCES ---------
module logAnalyticsWorkspace 'br/public:avm/res/operational-insights/workspace:0.11.1' = {
  name: 'logAnalyticsWorkspace'
  scope: rg
  params: {
    name: logAnalyticsWorkspaceName
    location: location
    tags: tags
  }
}

module applicationInsights 'br/public:avm/res/insights/component:0.6.0' = {
  name: 'applicationInsights'
  scope: rg
  params: {
    name: applicationInsightsName
    location: location
    tags: tags
    workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
  }
}

// --------- AI SERVICES ---------
module aiServicesAccount 'br/public:avm/res/cognitive-services/account:0.10.2' = {
  name: 'ai-services-account-deployment'
  scope: rg
  params: {
    kind: 'OpenAI'
    name: openAiServiceName
    location: location
    customSubDomainName: openAiServiceName
    disableLocalAuth: false
    diagnosticSettings: [
      {
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
      }
    ]
    managedIdentities: {
      systemAssigned: true
    }
    publicNetworkAccess: 'Enabled'
    sku: 'S0'
    deployments: openAiModelDeployments
    tags: tags
  }
}

// --------- SQL DATABASE ---------
module sqlServer 'br/public:avm/res/sql/server:0.9.0' = {
  name: 'sql-server-deployment'
  scope: rg
  params: {
    name: baseResourceName
    location: location
    administratorLogin: sqlServerUsername
    administratorLoginPassword: sqlServerPassword
    databases: [
      {
        name: 'AdventureWorksLT'
        skuName: 'GP_S_Gen5_2'
        skuTier: 'GeneralPurpose'
        collation: 'SQL_Latin1_General_CP1_CI_AS'
        maxSizeBytes: 34359738368
        sampleName: 'AdventureWorksLT'
        zoneRedundant: false
        readScale: 'Disabled'
        highAvailabilityReplicaCount: 0
        minCapacity: json('0.5')
        autoPauseDelay: 60
        requestedBackupStorageRedundancy: 'Local'
        isLedgerOn: false
        availabilityZone: 'NoPreference'
      }
    ]
    diagnosticSettings: [
      {
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
      }
    ]
    managedIdentities: {
      systemAssigned: true
    }
    publicNetworkAccess: 'Enabled'
    version: '12.0'
    tags: tags
  }
}

/*
module aiSearch './modules/aiSearch.bicep' = {
  name: 'aiSearch'
  scope: rg
  dependsOn: [
    monitoring
  ]
  params: {
    location: location
    aiSearchName: aiSearchName
    sku: 'basic'
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
  }
}
*/

output openAiServiceEndpoint string = aiServicesAccount.outputs.endpoint
