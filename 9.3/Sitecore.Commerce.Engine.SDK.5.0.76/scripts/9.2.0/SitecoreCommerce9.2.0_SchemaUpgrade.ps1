#******************************************************************************
# This script applies the Sitecore XC 9.2.0 schema to the Global and
# SharedEnvironments databases on top of an existing 9.1.* databases.
# The 9.1.* and 9.2.0 schemas can exist side-by-side.
#******************************************************************************

# Configuration
$serverName = "."
$globalDatabaseName = "SitecoreCommerce9_Global"
$sharedDatabaseName = "SitecoreCommerce9_SharedEnvironments"
$gdacPacPath = "<PathTo>\Sitecore.Commerce.Engine.Global.DB.dacpac"
$sdacPacPath = "<PathTo>\Sitecore.Commerce.Engine.Shared.DB.dacpac"

# Find most recent SQL Server version
$sql = Get-ChildItem -Path "C:\Program Files (x86)\Microsoft SQL Server\*0" | Sort-Object -Property @{e={$_.Name -as [int]}} | Select-Object -Last 1
$sqlPackage = "$sql\DAC\bin\SqlPackage.exe"
if (!(Test-Path $sqlPackage)) {
    Write-Host "Unable to find SqlPackage.exe"
}

# Publish DACPAC to databases
& $sqlPackage /SourceFile:"$gdacPacPath" /Action:Publish /TargetServerName:"$serverName" /TargetDatabaseName:"$globalDatabaseName" /TargetUser:"<Username>" /TargetPassword:"<Password>"
& $sqlPackage /SourceFile:"$sdacPacPath" /Action:Publish /TargetServerName:"$serverName" /TargetDatabaseName:"$sharedDatabaseName" /TargetUser:"<Username>" /TargetPassword:"<Password>"
