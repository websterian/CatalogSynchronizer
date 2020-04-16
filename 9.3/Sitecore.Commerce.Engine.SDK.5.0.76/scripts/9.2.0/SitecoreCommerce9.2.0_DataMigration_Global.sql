/******************************************************************************
* This script should run against 'SitecoreCommerce9_Global' database to
* migrate data from Sitecore XC 9.1.* to 9.2.0
******************************************************************************/

-- All Commerce data is migrated by bootstrapping the service after updating
-- the schema and _SharedEnvironments data

-- Set DB Version
DELETE FROM [sitecore_commerce_storage].[Versions]
INSERT INTO [sitecore_commerce_storage].[Versions] (DBVersion) VALUES (N'9.2.0')

