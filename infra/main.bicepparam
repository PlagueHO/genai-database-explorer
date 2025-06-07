using './main.bicep'

// Required parameters
param location = readEnvironmentVariable('AZURE_LOCATION', 'EastUS')
param environmentName = readEnvironmentVariable('ENVIRONMENT_NAME', 'genaidbexp')
param resourceGroupName = readEnvironmentVariable('RESOURCE_GROUP_NAME', 'rg-genaidbexp')

// User or service principal deploying the resources
param principalId = readEnvironmentVariable('AZURE_PRINCIPAL_ID', '')
param principalIdType = toLower(readEnvironmentVariable('AZURE_PRINCIPAL_ID_TYPE', 'user')) == 'serviceprincipal' ? 'ServicePrincipal' : 'User'

// SQL Server parameters
param sqlServerUsername = readEnvironmentVariable('SQL_SERVER_USERNAME', 'sqladmin')
param sqlServerPassword = readEnvironmentVariable('SQL_SERVER_PASSWORD', '')

// AI Search parameter
param azureAiSearchDeploy = bool(readEnvironmentVariable('AZURE_AI_SEARCH_DEPLOY', 'false'))
