// LMS Demo environment. Run at resource-group scope:
//   az deployment group create -g rg-lms-demo -f main.bicep -p sqlAdminPassword=<from a vault> suffix=<unique>
targetScope = 'resourceGroup'

@description('Short unique text so global names do not clash, e.g. your initials + digits')
@minLength(3)
@maxLength(8)
param suffix string

@description('Region for SQL and the API')
param location string = resourceGroup().location

@description('Static Web Apps only exist in a few regions')
param swaLocation string = 'eastasia'

param sqlAdminLogin string = 'lmsadmin'
@secure()
param sqlAdminPassword string

@secure()
param devSigningKey string

var apiName = 'lms-api-${suffix}'
var sqlName = 'lms-sql-${suffix}'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'plan-lms-demo'
  location: location
  kind: 'linux'
  sku: { name: 'F1', tier: 'Free' }
  properties: { reserved: true }
}

resource sql 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
  }
}

resource db 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sql
  name: 'LeaveManagement'
  location: location
  sku: { name: 'GP_S_Gen5', tier: 'GeneralPurpose', family: 'Gen5', capacity: 2 }
  properties: {
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
  }
}

resource allowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sql
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}

resource swa 'Microsoft.Web/staticSites@2023-12-01' = {
  name: 'lms-web-${suffix}'
  location: swaLocation
  sku: { name: 'Free', tier: 'Free' }
  properties: {}
}

resource api 'Microsoft.Web/sites@2023-12-01' = {
  name: apiName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: false          // F1 does not allow Always On
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Demo' }
        { name: 'Auth__Mode', value: 'Dev' }
        { name: 'Cors__AllowedOrigin', value: 'https://${swa.properties.defaultHostname}' }
        { name: 'Auth__Dev__SigningKey', value: devSigningKey }
      ]
      connectionStrings: [
        {
          name: 'Default'
          type: 'SQLAzure'
          connectionString: 'Server=tcp:${sql.properties.fullyQualifiedDomainName},1433;Database=LeaveManagement;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;'
        }
      ]
    }
  }
}

output apiUrl string = 'https://${api.properties.defaultHostName}'
output webUrl string = 'https://${swa.properties.defaultHostname}'
output sqlServer string = sql.properties.fullyQualifiedDomainName
