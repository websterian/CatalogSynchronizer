/*****************************************************************************************************
* This script should run against 'SitecoreCommerce9_SharedEnvironments' and 'SitecoreCommerce9_Global'
* database to clean Sitecore XC 9.1.* data
******************************************************************************************************/

-- delete 9.1.* tables

DROP TABLE IF EXISTS [dbo].[Versions]
GO

DROP TABLE IF EXISTS [dbo].[PricingLists]
GO

DROP TABLE IF EXISTS [dbo].[CartsLists]
GO

DROP TABLE IF EXISTS [dbo].[OrdersLists]
GO

DROP TABLE IF EXISTS [dbo].[PromotionsLists]
GO

DROP TABLE IF EXISTS [dbo].[OrdersEntities]
GO

DROP TABLE IF EXISTS [dbo].[PricingEntities]
GO

DROP TABLE IF EXISTS [dbo].[CommerceLists]
GO

DROP TABLE IF EXISTS [dbo].[ContentLists]
GO

DROP TABLE IF EXISTS [dbo].[PromotionsEntities]
GO

DROP TABLE IF EXISTS [dbo].[Mappings]
GO

DROP TABLE IF EXISTS [dbo].[CatalogLists]
GO

DROP TABLE IF EXISTS [dbo].[CatalogEntities]
GO

DROP TABLE IF EXISTS [dbo].[ContentEntities]
GO

DROP TABLE IF EXISTS [dbo].[CommerceEntities]
GO

-- delete 9.1.* stored procedures

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsSelectWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsSelectByRangeWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsSelectByRange]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsSelectByEntityIdWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsSelectByEntityId]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsSelectAllWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsSelectAll]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsSelect]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsInsertWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsInsert]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsDeleteWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsDeleteEntityWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsDeleteEntity]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsDelete]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsCountWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsCount]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceListsClearListWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceEntitiesUpdateWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceEntitiesUpdate]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceEntitiesSelectWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceEntitiesSelectAllVersionsWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceEntitiesSelectAllVersions]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceEntitiesSelect]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceEntitiesInsertWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceEntitiesInsert]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceEntitiesDeleteWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceEntitiesDelete]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceDBVersionGet]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CommerceBulkDeleteAllEntitiesByListNameWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CleanEnvironmentWithSharding]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CleanEnvironment]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CatalogUpdateMappings]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CatalogInsertMappings]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CatalogGetSitecoreIdsForEntityIdList]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CatalogGetMappingsForId]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CatalogGetMappings]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CatalogGetEntityIdsForSitecoreIdList]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CatalogGetDeterministicIdsForEntityId]
GO

DROP PROCEDURE IF EXISTS [dbo].[sp_CatalogDeleteMappings]
GO

-- delete 9.1.* user defined table types

DROP TYPE IF EXISTS [dbo].[SitecoreIdList]
GO

DROP TYPE IF EXISTS [dbo].[EntityIdList]
GO
