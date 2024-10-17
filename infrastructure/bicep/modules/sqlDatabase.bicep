param location string
param sqlLogicalServerName string
param sqlDatabaseName string
param sqlServerUsername string

@secure()
param sqlServerPassword string

param logAnalyticsWorkspaceId string
param logAnalyticsWorkspaceName string

// Azure SQL logical server
resource sqlLogicalServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlLogicalServerName
  location: location
  properties: {
    version: '12.0'
    administratorLogin: sqlServerUsername
    administratorLoginPassword: sqlServerPassword
    publicNetworkAccess: 'Enabled'
  }
}

// Azure SQL Database - Serverless
resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  name: sqlDatabaseName
  parent: sqlLogicalServer
  location: location
  sku: {
    tier: 'Serverless'
    family: 'Gen5'
  }
}

// Add the diagnostic settings to send logs and metrics to Log Analytics
resource sqlDatabaseDiagnosticSetting 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'send-to-${logAnalyticsWorkspaceName}'
  scope: sqlDatabase
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'OperationLogs'
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
