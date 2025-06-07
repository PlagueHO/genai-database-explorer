targetScope = 'subscription'

// This template uses Azure Verified Modules (AVM) from https://aka.ms/avm
// All core Azure resources are deployed using Microsoft-verified AVM modules
// for improved security, maintainability, and compliance.

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

@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
@minLength(1)
@maxLength(40)
param environmentName string

@description('The name of the resource group that will contain all the resources.')
param resourceGroupName string = 'rg-${environmentName}'

@description('The SQL logical server administrator username.')
param sqlServerUsername string

@description('The SQL logical server administrator password.')
@secure()
param sqlServerPassword string

@description('Whether to deploy Azure AI Search service.')
param azureAiSearchDeploy bool = false

var abbrs = loadJsonContent('./abbreviations.json')
var openAiModels = loadJsonContent('./sample-openai-models.json')

// tags that should be applied to all resources.
var tags = {
  // Tag all resources with the environment name.
  'azd-env-name': environmentName
  project: 'genai-database-explorer'
}

// Generate a unique token to be used in naming resources.
// Remove linter suppression after using.
#disable-next-line no-unused-vars
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

var logAnalyticsWorkspaceName = '${abbrs.operationalInsightsWorkspaces}${environmentName}'
var applicationInsightsName = '${abbrs.insightsComponents}${environmentName}'
var openAiServiceName = '${abbrs.aiServicesAccounts}${environmentName}'
var aiSearchName = '${abbrs.aiSearchSearchServices}${environmentName}'

// Transform the loaded models into the format expected by the AVM module
var openAiModelDeployments = [for model in openAiModels: {
  name: model.name
  modelName: model.model.name
  version: model.model.version
  sku: model.sku.name
  capacity: model.sku.capacity
}]

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
    name: '${abbrs.sqlServers}${environmentName}'
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

// --------- AI SEARCH (OPTIONAL) ---------
module aiSearchService 'br/public:avm/res/search/search-service:0.10.0' = if (azureAiSearchDeploy) {
  name: 'ai-search-service-deployment'
  scope: rg
  params: {
    name: aiSearchName
    location: location
    sku: 'basic'
    diagnosticSettings: [
      {
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
      }
    ]
    disableLocalAuth: false
    managedIdentities: {
      systemAssigned: true
    }
    publicNetworkAccess: 'Enabled'
    semanticSearch: 'standard'
    tags: tags
  }
}

// Outputs
output openAiServiceEndpoint string = aiServicesAccount.outputs.endpoint
output openAiServiceName string = aiServicesAccount.outputs.name
output openAiServiceId string = aiServicesAccount.outputs.resourceId

output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.outputs.name
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.outputs.resourceId
output logAnalyticsWorkspaceCustomerId string = logAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId

output applicationInsightsName string = applicationInsights.outputs.name
output applicationInsightsInstrumentationKey string = applicationInsights.outputs.instrumentationKey
output applicationInsightsConnectionString string = applicationInsights.outputs.connectionString

output sqlServerName string = sqlServer.outputs.name
output sqlServerId string = sqlServer.outputs.resourceId

// Optional AI Search outputs (only when deployed)
output aiSearchServiceName string = azureAiSearchDeploy ? aiSearchService.outputs.name : ''
output aiSearchServiceId string = azureAiSearchDeploy ? aiSearchService.outputs.resourceId : ''
output aiSearchServiceEndpoint string = azureAiSearchDeploy ? 'https://${aiSearchService.outputs.name}.search.windows.net' : ''
