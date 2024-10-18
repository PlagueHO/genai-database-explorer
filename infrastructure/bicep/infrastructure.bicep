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
param location string = 'EastUS'

@description('The base name that will prefixed to all Azure resources deployed to ensure they are unique.')
param baseResourceName string

@description('The name of the resource group that will contain all the resources.')
param resourceGroupName string

@description('The SQL logical server administrator username.')
param sqlServerUsername string = 'sqladmin'

@description('The SQL logical server administrator password.')
@secure()
param sqlServerPassword string

var logAnalyticsWorkspaceName = '${baseResourceName}-law'
var applicationInsightsName = '${baseResourceName}-appinsights'
var openAiServiceName = '${baseResourceName}-openai'
// var aiSearchName = '${baseResourceName}-aisearch'

var openAiModelDeployments = [
  {
    name: 'gpt-4o'
    modelName: 'gpt-4o'
    sku: 'Global-Standard'
    capacity: 80
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
}

module monitoring './modules/monitoring.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    location: location
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    applicationInsightsName: applicationInsightsName
  }
}

module openAiService './modules/openAiService.bicep' = {
  name: 'openAiService'
  scope: rg
  dependsOn: [
    monitoring
  ]
  params: {
    location: location
    openAiServiceName: openAiServiceName
    openAiModeldeployments: openAiModelDeployments
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
  }
}

module sqlDatabase './modules/sqlDatabase.bicep' = {
  name: 'sqlDatabase'
  scope: rg
  params: {
    location: location
    sqlLogicalServerName: '${baseResourceName}'
    sqlDatabaseName: 'AdventureWorksLT'
    sqlServerUsername: sqlServerUsername
    sqlServerPassword: sqlServerPassword
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
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

output openAiServiceEndpoint string = openAiService.outputs.openAiServiceEndpoint
