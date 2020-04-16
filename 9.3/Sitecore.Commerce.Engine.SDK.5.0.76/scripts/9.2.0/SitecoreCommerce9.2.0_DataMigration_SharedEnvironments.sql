/******************************************************************************
* This script should run against 'SitecoreCommerce9_SharedEnvironments'
* database to migrate data from Sitecore XC 9.1.* to 9.2.0
******************************************************************************/

DROP FUNCTION IF EXISTS [sitecore_commerce_storage].[GenerateEntityUniqueId]
GO

CREATE FUNCTION [sitecore_commerce_storage].[GenerateEntityUniqueId](@artifactStoreId UNIQUEIDENTIFIER, @entityId VARCHAR(150), @entityVersion INT)
RETURNS UNIQUEIDENTIFIER
AS
BEGIN
	DECLARE @key VARCHAR(200)
	SET @key = UPPER(CONVERT(VARCHAR(MAX), @artifactStoreId)) + N'|' + UPPER(@entityId) + N'|'
	IF (@entityVersion IS NOT NULL) SET @key = @key + Convert(VARCHAR(50), @entityVersion)

	DECLARE @hash VARBINARY(MAX)
	DECLARE @uniqueId UNIQUEIDENTIFIER
	SET @hash = HASHBYTES('SHA2_256', @key)
	SET @uniqueId = CONVERT(UNIQUEIDENTIFIER, @hash, 2)

	RETURN @uniqueId
END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[MigrateEntities920]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[MigrateEntities920]
(
	@SourceTable NVARCHAR(128),
	@TargetTable NVARCHAR(128),
	@EntityTypeFilter NVARCHAR(150)
)
WITH EXECUTE AS OWNER
AS
BEGIN
	DECLARE @Command NVARCHAR(MAX)
    DECLARE @Definitions AS NVARCHAR(max);

	SET @Command = N'INSERT INTO [sitecore_commerce_storage].' + QUOTENAME(@TargetTable + N'Entities') + '
		([UniqueId], [Id], [ArtifactStoreId], [ConcurrencyVersion], [EntityVersion], [Published])
		SELECT
			[sitecore_commerce_storage].GenerateEntityUniqueId([EnvironmentId], [Id], [EntityVersion]) AS [UniqueId],
			[Id] AS [Id],
			[EnvironmentId] AS [ArtifictStoreId],
			[Version] AS [ConcurrencyVersion],
			[EntityVersion] AS [EntityVersion],
			[Published] AS [Published]
		FROM [dbo].' + QUOTENAME(@SourceTable + N'Entities') + '
		WHERE [Id] LIKE @EntityTypeFilter'
	SET @Definitions = N'@EntityTypeFilter NVARCHAR(150)'
	EXEC sp_executesql @Command, @Definitions, @EntityTypeFilter = @EntityTypeFilter

	SET @Command = N'INSERT INTO [sitecore_commerce_storage].' + QUOTENAME(@TargetTable + N'Entity') + '
		([UniqueId], [ArtifactStoreId], [Entity])
		SELECT
			[sitecore_commerce_storage].GenerateEntityUniqueId([EnvironmentId], [Id], [EntityVersion]) AS [UniqueId],
			[EnvironmentId] AS [ArtifictStoreId],
			JSON_MODIFY([Entity], ''$.UniqueId'', CONVERT(nvarchar(150), [sitecore_commerce_storage].GenerateEntityUniqueId([EnvironmentId], [Id], [EntityVersion]))) AS [Entity]
		FROM [dbo].' + QUOTENAME(@SourceTable + N'Entities') + '
		WHERE [Id] LIKE @EntityTypeFilter'
	SET @Definitions = N'@EntityTypeFilter NVARCHAR(150)'
	EXEC sp_executesql @Command, @Definitions, @EntityTypeFilter = @EntityTypeFilter
END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[MigrateLists920]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[MigrateLists920]
(
	@SourceTable NVARCHAR(128),
	@TargetTable NVARCHAR(128),
	@ListNameFilter NVARCHAR(150)
)
WITH EXECUTE AS OWNER
AS
BEGIN
	DECLARE @Command NVARCHAR(MAX)
    DECLARE @Definitions AS NVARCHAR(max);

	SET @Command = N'INSERT INTO [sitecore_commerce_storage].' + QUOTENAME(@TargetTable + N'Lists') + '
([ListName], [ArtifactStoreId], [Id])
SELECT
	[ListName] AS [ListName],
	[EnvironmentId] AS [ArtifactStoreId],
	[CommerceEntityId] AS [Id]
FROM [dbo].' + QUOTENAME(@SourceTable + N'Lists') + '
WHERE [ListName] LIKE @ListNameFilter'
	SET @Definitions = N'@ListNameFilter NVARCHAR(150)'
	EXEC sp_executesql @Command, @Definitions, @ListNameFilter = @ListNameFilter
END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[MigrateLocalizations920]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[MigrateLocalizations920]
(
	@TargetTable NVARCHAR(128)
)
WITH EXECUTE AS OWNER
AS
BEGIN

	DECLARE @Command NVARCHAR(MAX)
	DECLARE @Definitions AS NVARCHAR(max);

	-- Temporary table used during LocalizationEntity migration
	IF OBJECT_ID('tempdb..#LocalizationInfo') IS NULL
	BEGIN
		CREATE TABLE #LocalizationInfo
		(
			[EntityId] NVARCHAR(150),
			[EntityUniqueId] UNIQUEIDENTIFIER,
			[EntityVersion] INT,
			[LocalizationId] NVARCHAR(150),
			[ComponentIndex] INT,
			[EntityJ] NVARCHAR(MAX),
		)
	END

	-- Cleanup temporary table used to store LocalizationEntity info for migration
	TRUNCATE TABLE #LocalizationInfo

	SET @Command = N'SELECT
			[es].[Id] AS [EntityId],
			[es].[UniqueId] AS [EntityUniqueId],
			[es].[EntityVersion] AS [EntityVersion],
			[cpv].[LocalizationId] AS [LocalizationId],
			[cp].[key] AS [ComponentIndex],
			[e].[Entity] AS [EntityJ]
		FROM [sitecore_commerce_storage].' + QUOTENAME(@TargetTable + N'Entities') + ' [es]
		JOIN [sitecore_commerce_storage].' + QUOTENAME(@TargetTable + N'Entity') + ' [e] ON [es].[UniqueId] = [e].[UniqueId]
		CROSS APPLY OPENJSON([e].[Entity], ''$.Components."$values"'') [cp]
		CROSS APPLY OPENJSON([cp].[value], ''$'')
		WITH
		(
			[ComponentType] NVARCHAR(512) ''$."$type"'',
			[LocalizationId] NVARCHAR(150) ''$.Entity.EntityTarget''
		) [cpv]
		WHERE [cpv].[ComponentType] LIKE ''%Sitecore.Commerce.Core.LocalizedEntityComponent%'''
	SET @Definitions = N''

	INSERT INTO #LocalizationInfo
	EXEC sp_executesql @Command, @Definitions

	INSERT INTO [sitecore_commerce_storage].[LocalizationEntities]
	SELECT
		[sitecore_commerce_storage].GenerateEntityUniqueId([ce].[EnvironmentId], STUFF([li].[LocalizationId],  27, 32, REPLACE([li].[EntityId], 'Entity-', '')), [li].[EntityVersion]) AS [UniqueId],
		STUFF([li].[LocalizationId],  27, 32, REPLACE([li].[EntityId], 'Entity-', '')) AS [Id],
		[ce].[EnvironmentId] AS [ArtifactStoreId],
		[ce].[Version] AS [ConcurrencyVersion],
		[ce].[EntityVersion] AS [EntityVersion],
		[ce].[Published] AS [Published]
	FROM [dbo].[CommerceEntities] [ce]
	JOIN #LocalizationInfo [li] ON [li].[LocalizationId] = [ce].[Id] AND [li].[EntityVersion] = [ce].[EntityVersion]

	INSERT INTO [sitecore_commerce_storage].[LocalizationEntity]
	SELECT
		[sitecore_commerce_storage].GenerateEntityUniqueId([ce].[EnvironmentId], STUFF([li].[LocalizationId],  27, 32, REPLACE([li].[EntityId], 'Entity-', '')), [li].[EntityVersion]) AS [UniqueId],
		[ce].[EnvironmentId] AS [ArtifactStoreId],
		JSON_MODIFY(JSON_MODIFY([ce].[Entity], '$.UniqueId', CONVERT(nvarchar(150), [sitecore_commerce_storage].GenerateEntityUniqueId([ce].[EnvironmentId], STUFF([li].[LocalizationId],  27, 32, REPLACE([li].[EntityId], 'Entity-', '')), [li].[EntityVersion]))), '$.Id', STUFF([li].[LocalizationId],  27, 32, REPLACE([li].[EntityId], 'Entity-', ''))) AS [Entity],
		[li].[EntityUniqueId] AS [EntityUniqueId]
	FROM [dbo].[CommerceEntities] [ce]
	JOIN #LocalizationInfo [li] ON [li].[LocalizationId] = [ce].[Id] AND [li].[EntityVersion] = [ce].[EntityVersion]

	SET @Command = N'UPDATE [sitecore_commerce_storage].' + QUOTENAME(@TargetTable + N'Entity') + N'
		SET [Entity] = JSON_MODIFY(
			[li].[EntityJ],
			''$.Components."$values"'',
			JSON_QUERY(
				''['' +
				STUFF
				(
					(
						SELECT	'','' + [value]
						FROM OPENJSON([li].[EntityJ], ''$.Components."$values"'')
						WHERE [key] <> [li].[ComponentIndex]
						FOR XML PATH('''')
					),
					1,
					1,
					''''
				) +
				'']''
			)
		)
		FROM [sitecore_commerce_storage].' + QUOTENAME(@TargetTable + N'Entity') + N'
			INNER JOIN #LocalizationInfo as li
			ON [UniqueId] = [li].EntityUniqueId'
	SET @Definitions = N''
	EXEC sp_executesql @Command, @Definitions
END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[MigrateRelationships920]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[MigrateRelationships920]
(
	@SourceTable NVARCHAR(128)
)
WITH EXECUTE AS OWNER
AS
BEGIN
	-- In this situation, using a cursor is significantly faster than using a join
	-- because the RelationshipDefinitions contains a small number of entries
	-- compared to the source table.
	DECLARE @RelationshipName NVARCHAR(150)
	DECLARE RelationshipCursor CURSOR LOCAL STATIC FORWARD_ONLY READ_ONLY
	FOR SELECT JSON_VALUE([Entity], N'$.Name') FROM [sitecore_commerce_storage].[RelationshipDefinitionEntity]

	OPEN RelationshipCursor
	FETCH NEXT FROM RelationshipCursor INTO @RelationshipName
	WHILE @@FETCH_STATUS = 0
	BEGIN
		DECLARE @Command NVARCHAR(MAX)
		DECLARE @Definitions NVARCHAR(MAX)

		SET @Command = N'INSERT INTO [sitecore_commerce_storage].[RelationshipLists]
			SELECT
				[ListName] AS [ListName],
				[EnvironmentId] AS [ArtifactStoreId],
				[CommerceEntityId] AS [Id]
			FROM [dbo].' + QUOTENAME(@SourceTable + N'Lists') + N' WHERE [ListName] LIKE ''List-'' + @RelationshipName + ''-%'''
		SET @Definitions = N'@RelationshipName NVARCHAR(150)'
		EXEC sp_executesql @Command, @Definitions, @RelationshipName = @RelationshipName

		FETCH NEXT FROM RelationshipCursor INTO @RelationshipName
	END

	CLOSE RelationshipCursor
	DEALLOCATE RelationshipCursor
END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[MigrateWorkflows920]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[MigrateWorkflows920]
(
	@SourceTable NVARCHAR(128)
)
WITH EXECUTE AS OWNER
AS
BEGIN
	DECLARE @Command NVARCHAR(MAX)

	SET @Command = N'INSERT INTO [sitecore_commerce_storage].[WorkflowLists]
		SELECT
			N''List-'' + UPPER([cpv].[WorkflowId]) + N''-'' + UPPER([cpv].[WorkflowState]) + N''-ByDate'' AS [ListName],
			[es].[EnvironmentId] AS [ArtifactStoreId],
			[es].[Id] AS [Id],
			[es].[EntityVersion] AS [EntityVersion]
		FROM [dbo].' + QUOTENAME(@SourceTable + N'Entities') + N' [es]
		CROSS APPLY OPENJSON([es].[Entity], N''$.Components."$values"'')
		WITH
		(
			[ComponentType] NVARCHAR(512) N''$."$type"'',
			[WorkflowId] NVARCHAR(150) N''$.Workflow.EntityTarget'',
			[WorkflowState] NVARCHAR(150) N''$.CurrentState''
		) [cpv]
		WHERE [cpv].[ComponentType] LIKE N''%Sitecore.Commerce.Plugin.Workflow.WorkflowComponent%'''
	EXEC sp_executesql @Command
END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[CreateVersioningEntities920]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[CreateVersioningEntities920]
(
	@TargetTable NVARCHAR(128)
)
WITH EXECUTE AS OWNER
AS
BEGIN
	DECLARE @json NVARCHAR(MAX)=N'{
		"$type": "Sitecore.Commerce.Core.VersioningEntity, Sitecore.Commerce.Core",
		"Versions": {
			"$type": "System.Collections.Generic.List`1[[Sitecore.Commerce.Core.EntityVersion, Sitecore.Commerce.Core]], mscorlib",
			"$values": []
		},
		"Version": 1,
		"EntityVersion": 1,
		"Published": true,
		"IsPersisted": true
	}'

	IF OBJECT_ID('tempdb..#EntitiesInfo') IS NULL
	BEGIN
		CREATE TABLE #EntitiesInfo
		(
			[Id] NVARCHAR(150),
			[ArtifactStoreId] UNIQUEIDENTIFIER,
			[EntityVersion] INT,
			[Published] BIT,
		)
	END

	TRUNCATE TABLE #EntitiesInfo

	DECLARE @Command NVARCHAR(MAX)
	DECLARE @Definitions AS NVARCHAR(MAX);

	-- getting only entities with multiple entity versions
	SET @Command = N'SELECT
			[Id], [ArtifactStoreId], [EntityVersion], [Published]
		FROM [sitecore_commerce_storage].' + QUOTENAME(@TargetTable + N'Entities') + '
		WHERE [Id] IN (
			SELECT [Id]
			FROM [sitecore_commerce_storage].' + QUOTENAME(@TargetTable + N'Entities') + '
			GROUP BY [Id], [ArtifactStoreId]
			HAVING COUNT(*) > 1
		)
		ORDER BY [Id], [ArtifactStoreId], [EntityVersion]'
	SET @Definitions = N''

	INSERT INTO #EntitiesInfo
	EXEC sp_executesql @Command, @Definitions

	DECLARE @id	NVARCHAR(150), @artifactStoreId UNIQUEIDENTIFIER,  @entityVersion INT, @published BIT
	DECLARE versioning_cursor CURSOR FOR
	SELECT [Id], [ArtifactStoreId], [EntityVersion], [Published]
	FROM #EntitiesInfo
	ORDER BY [Id], [ArtifactStoreId], [EntityVersion]

	OPEN versioning_cursor

	FETCH NEXT FROM versioning_cursor
	INTO @id, @artifactStoreId, @entityVersion, @published

	WHILE @@FETCH_STATUS = 0
	BEGIN
		DECLARE @versioningId NVARCHAR(150) = CONCAT('Entity-VersioningEntity-', REPLACE(@id, 'Entity-', ''))
		DECLARE	@versioningUniqueId UNIQUEIDENTIFIER = [sitecore_commerce_storage].GenerateEntityUniqueId(@artifactStoreId, @versioningId, 1)
		DECLARE @versioningJson NVARCHAR(MAX) = ''
		DECLARE @publishedBool NVARCHAR(6) = N'false'
		IF (@published = 1)
		BEGIN
			SET @publishedBool = N'true'
		END
		DECLARE @versionJson NVARCHAR(250) = N'{"$type": "Sitecore.Commerce.Core.EntityVersion, Sitecore.Commerce.Core", "Version": '+CONVERT(NVARCHAR(1),@entityVersion)+N', "Published": '+@publishedBool+N'}'

		-- insert the versioning entity if it does not exists, otherwise update the json with the new entity version
		SELECT @versioningJson = [Entity] FROM [sitecore_commerce_storage].[VersioningEntity] WHERE [UniqueId] = @versioningUniqueId
		IF (@versioningJson = '')
		BEGIN
			SET @versioningJson = JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(@json,'$.Id',@versioningId),'$.UniqueId',CONVERT(NVARCHAR(150),@versioningUniqueId)), 'append $.Versions."$values"',JSON_QUERY(@versionJson))

			INSERT INTO [sitecore_commerce_storage].[VersioningEntity]
			SELECT
				@versioningUniqueId AS [UniqueId],
				@artifactStoreId AS [ArtifactStoreId],
				@versioningJson AS [Entity]

			INSERT INTO [sitecore_commerce_storage].[VersioningEntities]
			SELECT
				@versioningUniqueId AS [UniqueId],
				@versioningId AS [Id],
				@artifactStoreId AS [ArtifactStoreId],
				1 AS [ConcurrencyVersion],
				1 AS [EntityVersion],
				1 AS [Published]
		END
		ELSE
		BEGIN
			SET @versioningJson = JSON_MODIFY(@versioningJson,'append $.Versions."$values"',JSON_QUERY(@versionJson))

			UPDATE [sitecore_commerce_storage].[VersioningEntity]
			SET [Entity] = @versioningJson
			WHERE [UniqueId] = @versioningUniqueId
		END

		FETCH NEXT FROM versioning_cursor
		INTO @id, @artifactStoreId, @entityVersion, @published
	END
	CLOSE versioning_cursor
	DEALLOCATE versioning_cursor
END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[MigrateTombstones920]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[MigrateTombstones920]
WITH EXECUTE AS OWNER
AS
BEGIN
	INSERT INTO [sitecore_commerce_storage].[CommerceEntities]
	SELECT
		[sitecore_commerce_storage].GenerateEntityUniqueId([EnvironmentId], CONCAT('Entity-Tombstone-', JSON_VALUE([Entity],'$.IndexId')), [EntityVersion]) AS [UniqueId],
	 	CONCAT('Entity-Tombstone-', JSON_VALUE([Entity],'$.IndexId')) AS [Id],
	 	[EnvironmentId] AS [ArtifictStoreId],
	 	[Version] AS [ConcurrencyVersion],
	 	[EntityVersion] AS [EntityVersion],
	 	[Published] AS [Published]
	FROM [dbo].[CommerceEntities]
	WHERE [Id] LIKE 'Entity-Tombstone%'

	INSERT INTO [sitecore_commerce_storage].[CommerceEntity]
	SELECT
		[sitecore_commerce_storage].GenerateEntityUniqueId([EnvironmentId], CONCAT('Entity-Tombstone-', JSON_VALUE([Entity],'$.IndexId')), [EntityVersion]) AS [UniqueId],
	 	[EnvironmentId] AS [ArtifictStoreId],
	 	JSON_MODIFY(JSON_MODIFY([Entity], '$.UniqueId', CONVERT(nvarchar(150), [sitecore_commerce_storage].GenerateEntityUniqueId([EnvironmentId], CONCAT('Entity-Tombstone-', JSON_VALUE([Entity],'$.IndexId')), [EntityVersion]))), '$.Id', CONCAT('Entity-Tombstone-', JSON_VALUE([Entity],'$.IndexId'))) AS [Entity]
	FROM [dbo].[CommerceEntities]
	WHERE [Id] LIKE 'Entity-Tombstone%'

	INSERT INTO [sitecore_commerce_storage].[CommerceLists]
	SELECT
		N'List-TOMBSTONES-ByDate' AS [ListName],
		[ArtifactStoreId] AS [ArtifactStoreId],
		[Id] AS [Id]
	FROM [sitecore_commerce_storage].[CommerceEntities]
	WHERE [Id] LIKE 'Entity-Tombstone%'
END
GO

-- BusinessUsers Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-BusinessUser%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-BusinessUsers%'

-- Cart Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-Cart%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Carts', N'Commerce', N'List-Carts%'

-- Catalog Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Catalog', N'Catalog', N'Entity-Catalog%'
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Catalog', N'Catalog', N'Entity-Category%'
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Catalog', N'Catalog', N'Entity-SellableItem%'
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Catalog', N'RelationshipDefinition', N'Entity-RelationshipDefinition%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Catalog', N'Catalog', N'List-CatalogItems%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Catalog', N'Catalog', N'List-Catalogs%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Catalog', N'Catalog', N'List-Categories%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Catalog', N'Catalog', N'List-SellableItems%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Catalog', N'List-PurgeCatalogs%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Catalog', N'List-PurgeCategories%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'RelationshipDefinition', N'List-CustomRelationshipDefinitions%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'RelationshipDefinition', N'List-DefaultRelationshipDefinitions%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Promotions', N'Relationship', N'List-PromotionBookToCatalog%'
EXEC [sitecore_commerce_storage].[MigrateLocalizations920] N'Catalog'
EXEC [sitecore_commerce_storage].[MigrateRelationships920] N'Catalog'
EXEC [sitecore_commerce_storage].[MigrateWorkflows920] N'Catalog'
EXEC [sitecore_commerce_storage].[MigrateWorkflows920] N'Catalog'
EXEC [sitecore_commerce_storage].[CreateVersioningEntities920] N'Catalog'

-- Composer Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-ComposerTemplate%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-ComposerTemplates%'

-- Content Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Content', N'Content', N'%' -- migrate all entities
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Content', N'Content', N'%' -- migrate all lists

-- Core Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-Workflow%'
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-PolicySet%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-Workflows%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-PolicySets%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-%DeletedIndexMaster%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-%DeletedIndexWeb%'

-- Coupon Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Promotions', N'Entity-Coupon%'
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Promotions', N'Entity-PrivateCouponGroup%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Promotions', N'List-Coupon%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Promotions', N'List-PrivateCouponGroups%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Promotions', N'Entity-Coupon%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Promotions', N'Entity-PrivateCouponGroups%'

-- Customers Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Customers', N'Entity-Customer%'
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Customers', N'Customer-%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Customers', N'List-Customers%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Customers', N'List-RecentCustomers%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-DeletedCustomersIndex%'

-- DigitalItems Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-DigitalProduct%'
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-Installation%'
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-Warranty%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-DigitalProducts%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-Installations%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-Warranties%'

-- Entitlements Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-Entitlement%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-Entitlements%'

-- Fulfillments Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-Shipment%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-Shipments%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-PendingShipments%'

-- GiftCard Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-GiftCard%'
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'GiftCard%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-GiftCards%'

-- Inventory Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Inventory', N'Entity-InventorySet%'
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Inventory', N'Entity-InventoryInformation%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Inventory', N'List-InventorySets%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Inventory', N'List-InventoryInformations%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Inventory', N'List-InventorySetToInventoryInformation%'
EXEC [sitecore_commerce_storage].[MigrateLocalizations920] N'Inventory'
EXEC [sitecore_commerce_storage].[MigrateRelationships920] N'Commerce'

-- Journaling Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-JournalEntry%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-JournalEntries%'

-- ManagedLists Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-ManagedList%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-ManagedLists%'

-- Orders Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Orders', N'Orders', N'%' -- migrate all entities
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Orders', N'Orders', N'%' -- migrate all lists
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'List-OrdersIndex%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'List-WaitingForAvailabilityOrders%'

-- Payments Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Orders', N'Entity-SalesActivity%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'List-SalesActivities%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'List-Order-%-SalesActivities%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'List-SettleSalesActivities%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'Entity-Order%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'Entity-SalesActivity%'

-- Preorderable Migration
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'List-PreOrders%'

-- Pricing Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Pricing', N'Pricing', N'%' -- migrate all entities
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Pricing', N'Pricing', N'%' -- migrate all lists

-- Promotions Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Promotions', N'Promotions', N'%' -- migrate all entities
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Promotions', N'Promotions', N'%' -- migrate all lists
EXEC [sitecore_commerce_storage].[MigrateLocalizations920] N'Promotions'

-- Returns Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Orders', N'Entity-ReturnMerchandiseAuthorization%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'List-ReturnMerchandiseAuthorizations%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'List-PendingRmas%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'List-ProblemRmas%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'List-RefundPendingRmas%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'List-CompletedRmas%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Orders', N'Entity-ReturnMerchandiseAuthorization%'

-- Shops Migration
EXEC [sitecore_commerce_storage].[MigrateEntities920] N'Commerce', N'Commerce', N'Entity-Shop%'
EXEC [sitecore_commerce_storage].[MigrateLists920] N'Commerce', N'Commerce', N'List-Shops%'

-- Tombstones Migration
EXEC [sitecore_commerce_storage].[MigrateTombstones920]

-- Set DB Version
DELETE FROM [sitecore_commerce_storage].[Versions]
INSERT INTO [sitecore_commerce_storage].[Versions] (DBVersion) VALUES (N'9.2.0')
GO

-- Clean up procedures
DROP FUNCTION IF EXISTS [sitecore_commerce_storage].[GenerateEntityUniqueId]
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[MigrateEntities920]
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[MigrateLists920]
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[MigrateLocalizations920]
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[MigrateRelationships920]
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[MigrateWorkflows920]
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[MigrateWorkflows920]
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[CreateVersioningEntities920]
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[MigrateTombstones920]
GO
