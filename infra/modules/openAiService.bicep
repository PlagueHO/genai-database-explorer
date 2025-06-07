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
param openAiServiceName string
param openAiModeldeployments array
param logAnalyticsWorkspaceId string
param logAnalyticsWorkspaceName string

resource openAiService 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAiServiceName
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'OpenAI'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    customSubDomainName: openAiServiceName
  }
}

// Loop through the list of models and create a deployment for each
@batchSize(1)
resource openAiServiceDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = [for (model, i) in openAiModeldeployments: {
  name: model.name
  parent: openAiService
  sku: {
    name: model.sku
    capacity: model.capacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: model.modelName
      version: model.version
    }
  }
}]

// Add the diagnostic settings to send logs and metrics to Log Analytics
resource openAiServiceDiagnosticSetting 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'send-to-${logAnalyticsWorkspaceName}'
  scope: openAiService
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'Audit'
        enabled: true
        retentionPolicy: {
          days: 0
          enabled: false 
        }
      }
      {
        category: 'RequestResponse'
        enabled: true
        retentionPolicy: {
          days: 0
          enabled: false 
        }
      }
      {
        category: 'Trace'
        enabled: true
        retentionPolicy: {
          days: 0
          enabled: false 
        }
      }
    ]
    metrics:[
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

output openAiServiceName string = openAiService.name
output openAiServiceId string = openAiService.id
output openAiServiceEndpoint string = openAiService.properties.endpoint
