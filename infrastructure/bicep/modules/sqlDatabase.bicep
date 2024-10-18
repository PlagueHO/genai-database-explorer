param location string
param sqlLogicalServerName string
param sqlDatabaseName string
param sqlServerUsername string

@secure()
param sqlServerPassword string

param logAnalyticsWorkspaceId string
param logAnalyticsWorkspaceName string

resource sqlLogicalServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlLogicalServerName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    version: '12.0'
    administratorLogin: sqlServerUsername
    administratorLoginPassword: sqlServerPassword
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-08-01-preview' = {
  name: sqlDatabaseName
  parent: sqlLogicalServer
  location: location
  sku: {
    name: 'GP_S_Gen5_2'
    tier: 'GeneralPurpose'
  }
  properties: {
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
