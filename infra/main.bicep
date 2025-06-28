targetScope = 'subscription'
extension microsoftGraphV1

@sys.description('Name of the the environment which is used to generate a short unique hash used in all resources.')
@minLength(1)
@maxLength(40)
param environmentName string

@sys.description('Location for all resources')
@minLength(1)
@metadata({
  azd: {
    type: 'location'
  }
})
param location string

@sys.description('The Azure resource group where new resources will be deployed.')
@metadata({
  azd: {
    type: 'resourceGroup'
  }
})
param resourceGroupName string = 'rg-${environmentName}'

@sys.description('Id of the user or app to assign application roles.')
param principalId string

@sys.description('Type of the principal referenced by principalId.')
@allowed([
  'User'
  'ServicePrincipal'
])
param principalIdType string = 'User'

@sys.description('The SQL logical server administrator username.')
param sqlServerUsername string

@sys.description('The SQL logical server administrator password.')
@secure()
param sqlServerPassword string

@sys.description('Whether to deploy Azure AI Search service.')
param azureAiSearchDeploy bool = false

@sys.description('Whether to deploy Cosmos DB.')
param cosmosDbDeploy bool = false

@sys.description('Whether to deploy Storage Account.')
param storageAccountDeploy bool = false

var abbrs = loadJsonContent('./abbreviations.json')
var openAiModels = loadJsonContent('./azure-openai-models.json')

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
var aiFoundryName = '${abbrs.aiFoundryAccounts}${environmentName}'
var aiFoundryCustomSubDomainName = toLower(replace(environmentName, '-', ''))
var aiSearchName = '${abbrs.aiSearchSearchServices}${environmentName}'
var cosmosDbAccountName = '${abbrs.cosmosDBAccounts}${environmentName}'
var storageAccountName = '${abbrs.storageStorageAccounts}${toLower(replace(environmentName, '-', ''))}'

// Use the OpenAI models directly from JSON - they're already in the correct format for the AVM module
var openAiModelDeployments = openAiModels

// The application resources that are deployed into the application resource group
module rg 'br/public:avm/res/resources/resource-group:0.4.1' = {
  name: 'resourceGroup'
  params: {
    name: resourceGroupName
    location: location
    tags: tags
  }
}

// --------- MONITORING RESOURCES ---------
module logAnalyticsWorkspace 'br/public:avm/res/operational-insights/workspace:0.11.2' = {
  name: 'logAnalyticsWorkspace'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: logAnalyticsWorkspaceName
    location: location
    tags: tags
  }

}

module applicationInsights 'br/public:avm/res/insights/component:0.6.0' = {
  name: 'applicationInsights'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: applicationInsightsName
    location: location
    tags: tags
    workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
  }
}

// --------- AI FOUNDRY ---------
module aiFoundryAccount 'br/public:avm/res/cognitive-services/account:0.11.0' = {
  name: 'ai-foundry-account-deployment'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    kind: 'AIServices'
    name: aiFoundryName
    location: location
    customSubDomainName: aiFoundryCustomSubDomainName
    diagnosticSettings: [
      {
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
      }
    ]
    managedIdentities: {
      systemAssigned: true
    }
    sku: 'S0'
    deployments: openAiModelDeployments
    tags: tags
    roleAssignments: [
      {
        roleDefinitionIdOrName: 'Cognitive Services OpenAI Contributor'
        principalType: principalIdType
        principalId: principalId
      }
      {
        roleDefinitionIdOrName: 'Cognitive Services OpenAI Service User'
        principalType: principalIdType
        principalId: principalId
      }
    ]
  }
}

// --------- SQL DATABASE ---------
module sqlServer 'br/public:avm/res/sql/server:0.19.1' = {
  name: 'sql-server-deployment'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: '${abbrs.sqlServers}${environmentName}'
    location: location
    administratorLogin: sqlServerUsername
    administratorLoginPassword: sqlServerPassword
    databases: [
      {
        name: 'AdventureWorksLT'
        availabilityZone: -1
        sku: {
          name: 'GP_S_Gen5_2'
          tier: 'GeneralPurpose'
        }
        collation: 'SQL_Latin1_General_CP1_CI_AS'
        maxSizeBytes: 34359738368
        sampleName: 'AdventureWorksLT'
        zoneRedundant: false
        readScale: 'Disabled'
        highAvailabilityReplicaCount: 0
        minCapacity: '0.5'
        autoPauseDelay: 60
        requestedBackupStorageRedundancy: 'Local'
        isLedgerOn: false
      }
    ]
    managedIdentities: {
      systemAssigned: true
    }
    publicNetworkAccess: 'Enabled'
    tags: tags
    roleAssignments: [
      {
        roleDefinitionIdOrName: 'SQL DB Contributor'
        principalType: principalIdType
        principalId: principalId
      }
      {
        roleDefinitionIdOrName: 'SQL Security Manager'
        principalType: principalIdType
        principalId: principalId
      }
    ]
  }
}

// --------- COSMOS DB ---------
module cosmosDbAccount 'br/public:avm/res/document-db/database-account:0.15.0' = if (cosmosDbDeploy) {
  name: 'cosmos-db-account-deployment'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: cosmosDbAccountName
    location: location
    enableFreeTier: true
    sqlDatabases: [
      {
        name: 'genaidbexp'
        throughput: 400
        containers: [
          {
            name: 'default-container'
            paths: ['/id']
            throughput: 400
          }
        ]
      }
    ]
    diagnosticSettings: [
      {
        name: 'cosmos-db-diagnostics'
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
        logCategoriesAndGroups: [
          {
            categoryGroup: 'allLogs'
            enabled: true
          }
        ]
        metricCategories: [
          {
            category: 'AllMetrics'
            enabled: true
          }
        ]
      }
    ]
    managedIdentities: {
      systemAssigned: true
    }
    tags: tags
    roleAssignments: [
      {
        roleDefinitionIdOrName: 'Cosmos DB Account Reader Role'
        principalType: principalIdType
        principalId: principalId
      }
      {
        roleDefinitionIdOrName: 'Cosmos DB Data Contributor'
        principalType: principalIdType
        principalId: principalId
      }
    ]
  }
}

// --------- STORAGE ACCOUNT ---------
module storageAccount 'br/public:avm/res/storage/storage-account:0.23.0' = if (storageAccountDeploy) {
  name: 'storage-account-deployment'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: storageAccountName
    location: location
    kind: 'StorageV2'
    skuName: 'Standard_LRS'
    allowBlobPublicAccess: false
    blobServices: {
      containers: [
        {
          name: 'genaidbexp'
          publicAccess: 'None'
        }
      ]
    }
    diagnosticSettings: [
      {
        name: 'default-diagnostic-setting'
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
        logCategoriesAndGroups: [
          {
            categoryGroup: 'allLogs'
          }
        ]
        metricCategories: [
          {
            category: 'AllMetrics'
          }
        ]
      }
    ]
    managedIdentities: {
      systemAssigned: true
    }
    tags: tags
    roleAssignments: [
      {
        roleDefinitionIdOrName: 'Storage Blob Data Contributor'
        principalType: principalIdType
        principalId: principalId
      }
    ]
  }
}

// --------- AI SEARCH (OPTIONAL) ---------
module aiSearchService 'br/public:avm/res/search/search-service:0.10.0' = if (azureAiSearchDeploy) {
  name: 'ai-search-service-deployment'
  scope: resourceGroup(resourceGroupName)
  dependsOn: [
    rg
  ]
  params: {
    name: aiSearchName
    location: location
    sku: 'basic'
    diagnosticSettings: [
      {
        name: 'default-diagnostic-setting'
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
        logCategoriesAndGroups: [
          {
            categoryGroup: 'allLogs'
          }
        ]
        metricCategories: [
          {
            category: 'AllMetrics'
          }
        ]
      }
    ]
    disableLocalAuth: false
    managedIdentities: {
      systemAssigned: true
    }
    publicNetworkAccess: 'Enabled'
    semanticSearch: 'standard'
    tags: tags
    roleAssignments: [
      {
        roleDefinitionIdOrName: 'Search Service Contributor'
        principalType: principalIdType
        principalId: principalId
      }
      {
        roleDefinitionIdOrName: 'Search Index Data Contributor'
        principalType: principalIdType
        principalId: principalId
      }
    ]
  }
}


output AZURE_RESOURCE_GROUP string = rg.outputs.name
output AZURE_PRINCIPAL_ID string = principalId
output AZURE_PRINCIPAL_ID_TYPE string = principalIdType

// Output the monitoring resources
output LOG_ANALYTICS_WORKSPACE_NAME string = logAnalyticsWorkspace.outputs.name
output LOG_ANALYTICS_RESOURCE_ID string = logAnalyticsWorkspace.outputs.resourceId
output LOG_ANALYTICS_WORKSPACE_ID string = logAnalyticsWorkspace.outputs.logAnalyticsWorkspaceId
output APPLICATION_INSIGHTS_NAME string = applicationInsights.outputs.name
output APPLICATION_INSIGHTS_RESOURCE_ID string = applicationInsights.outputs.resourceId
output APPLICATION_INSIGHTS_INSTRUMENTATION_KEY string = applicationInsights.outputs.instrumentationKey

// Output the AI Services resources
output AZURE_AI_SEARCH_NAME string = azureAiSearchDeploy ? aiSearchService.outputs.name : ''
output AZURE_AI_SEARCH_ID   string = azureAiSearchDeploy ? aiSearchService.outputs.resourceId : ''
output AZURE_AI_SERVICES_NAME string = aiFoundryAccount.outputs.name
output AZURE_AI_SERVICES_ID string = aiFoundryAccount.outputs.resourceId
output AZURE_AI_SERVICES_ENDPOINT string = aiFoundryAccount.outputs.endpoint
output AZURE_AI_SERVICES_RESOURCE_ID string = aiFoundryAccount.outputs.resourceId

output SQL_SERVER_NAME string = sqlServer.outputs.name
output SQL_SERVER_RESOURCE_ID string = sqlServer.outputs.resourceId

// Output the Cosmos DB resources
output COSMOS_DB_ACCOUNT_NAME string = cosmosDbDeploy ? cosmosDbAccount.outputs.name : ''
output COSMOS_DB_ACCOUNT_RESOURCE_ID string = cosmosDbDeploy ? cosmosDbAccount.outputs.resourceId : ''
output COSMOS_DB_ACCOUNT_ENDPOINT string = cosmosDbDeploy ? cosmosDbAccount.outputs.endpoint : ''

// Output the Storage Account resources
output STORAGE_ACCOUNT_NAME string = storageAccountDeploy ? storageAccount.outputs.name : ''
output STORAGE_ACCOUNT_RESOURCE_ID string = storageAccountDeploy ? storageAccount.outputs.resourceId : ''
output STORAGE_ACCOUNT_BLOB_ENDPOINT string = storageAccountDeploy ? storageAccount.outputs.primaryBlobEndpoint : ''
