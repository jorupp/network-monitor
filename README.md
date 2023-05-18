# network-monitor
.Net tool to make regular network requests and log duration, with the intent of debugging networking issues

## Setup
Copy `appsettings.json` to `appsettings.Development.json` and set up your connection to Application Insights and the URLs/addresses to monitor:
```
{
  "ApplicationInsights": {
    "ConnectionString": "<copy from Application Insights in Azure Portal>",
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "NetworkMonitor.NetworkMonitorService":  "Debug",
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "settings": {
    "interval": "00:00:05",
    "machineName":  "my-laptop",
    "http": {
      "google": "https://google.com",
      "microsoft": "https://microsoft.com",
      "ado": "https://dev.azure.com",
      "teams": "https://teams.microsoft.com"
    },
    "ping": {
      "cf-dns": "1.1.1.1",
      "google-dns": "8.8.8.8"
    }
  }
}
```

## Example AI queries to review the data
```
customEvents
| where timestamp > ago(1h)
| where name == 'network-monitor'
| extend targetTime = todatetime(customDimensions['targetTime'])
| extend machineName = tostring(customDimensions['MachineName'])
| extend testName = tostring(customDimensions['testName'])
| extend testType = tostring(customDimensions['testType'])
| extend duration = toint(customMeasurements['duration'])
| where machineName in ('rupp-laptop', 'rupp-new-desktop')
| summarize avg(duration) by machineName, bin(targetTime, 5s)
| render timechart 
```
