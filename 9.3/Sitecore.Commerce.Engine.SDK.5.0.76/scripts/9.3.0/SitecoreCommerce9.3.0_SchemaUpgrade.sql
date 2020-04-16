/******************************************************************************
* This script should run against 'SitecoreCommerce9_SharedEnvironments' and
* 'SitecoreCommerce9_Global' to upgrade from Sitecore XC 9.2 to 9.3
******************************************************************************/

/**************************************
* Update database version
**************************************/
PRINT N'Updating database version ...'

UPDATE [sitecore_commerce_storage].[Versions] SET DBVersion='9.3.0'
GO

/**************************************
* Update tables schema
**************************************/
PRINT N'Updating tables schema ...'
GO

ALTER TABLE [sitecore_commerce_storage].[Mappings] ADD [ParentCatalog] UNIQUEIDENTIFIER NULL
GO

ALTER TABLE [sitecore_commerce_storage].[Mappings] DROP COLUMN [ParentCatalogList]
GO

/**************************************
* Add new indexes
**************************************/
PRINT N'Adding new indexes ...'
GO

DROP INDEX IF EXISTS [IX_Mappings_ParentId] ON [sitecore_commerce_storage].[Mappings]
GO

CREATE NONCLUSTERED INDEX [IX_Mappings_ParentId] ON [sitecore_commerce_storage].[Mappings]
(
	[ParentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

DROP INDEX IF EXISTS  [IX_Mappings_SitecoreId] ON [sitecore_commerce_storage].[Mappings]
GO

CREATE NONCLUSTERED INDEX [IX_Mappings_SitecoreId] ON [sitecore_commerce_storage].[Mappings]
(
	[SitecoreId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

DROP INDEX IF EXISTS [IX_Mappings_DeterministicId] ON [sitecore_commerce_storage].[Mappings]
GO

CREATE NONCLUSTERED INDEX [IX_Mappings_DeterministicId] ON [sitecore_commerce_storage].[Mappings]
(
	[DeterministicId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

/**************************************
* Add new used defined types
**************************************/
PRINT N'Adding new used defined types ...'
GO

CREATE TYPE [sitecore_commerce_storage].[EntityUniqueIdList] AS TABLE (
    [UniqueId] UNIQUEIDENTIFIER NOT NULL);
GO

/**************************************
* Update stored procedures
**************************************/
PRINT N'Updating stored procedures ...'
GO

DROP PROCEDURE [sitecore_commerce_storage].[DeleteListEntity];
GO

ALTER PROCEDURE [sitecore_commerce_storage].[SelectEntities]
(
	@EntityIds EntityIdList READONLY,
	@TableName NVARCHAR(150) = 'CommerceEntities',
	@ArtifactStoreId UNIQUEIDENTIFIER,
	@IgnorePublished bit = 0
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

	IF (@TableName = '')
		SET @TableName = 'CommerceEntities'

	IF (@TableName = 'CommerceEntities')
    BEGIN

		SELECT 
			[entities].[UniqueId],
			[entities].[Id],
			[entities].[EntityVersion]
		INTO 
			#tmpCommerceEntities
		FROM (
			SELECT
				[innerEntities].[UniqueId],
				[innerEntities].[Id],
				[innerEntities].[EntityVersion],
				ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
			FROM
				[sitecore_commerce_storage].[CommerceEntities] [innerEntities]
			JOIN @EntityIds ids ON ids.EntityId = [innerEntities].[Id]
			WHERE 
				[innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND ((ids.EntityVersion IS NULL AND ([Published] = 1 OR @IgnorePublished = 1)) OR (([innerEntities].[EntityVersion] = ids.EntityVersion) AND ([Published] = 1 OR @IgnorePublished = 1)))
		) [entities]
		WHERE rowNumber = 1

        
		SELECT 
			[entities].[Id],
			[entities].[UniqueId],
			[entities].[EntityVersion],
			[entity].[Entity] AS [Entity],
			[localization].[Entity] AS [LocalizationEntity]
		FROM 
			#tmpCommerceEntities [entities]
		INNER JOIN
			[sitecore_commerce_storage].[CommerceEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
		LEFT OUTER JOIN
			[sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]

        DROP TABLE #tmpCommerceEntities

        END
	ELSE IF (@TableName = 'CatalogEntities')
    BEGIN

		SELECT 
			[entities].[UniqueId],
			[entities].[Id],
			[entities].[EntityVersion]
		INTO 
			#tmpCatalogEntities
		FROM (
			SELECT
				[innerEntities].[UniqueId],
				[innerEntities].[Id],
				[innerEntities].[EntityVersion],
				ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
			FROM
				[sitecore_commerce_storage].[CatalogEntities] [innerEntities]
			JOIN @EntityIds ids ON ids.EntityId = [innerEntities].[Id]
			WHERE 
				[innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND ((ids.EntityVersion IS NULL AND ([Published] = 1 OR @IgnorePublished = 1)) OR (([innerEntities].[EntityVersion] = ids.EntityVersion) AND ([Published] = 1 OR @IgnorePublished = 1)))
		) [entities]
		WHERE rowNumber = 1

        
		SELECT 
			[entities].[Id],
			[entities].[UniqueId],
			[entities].[EntityVersion],
			[entity].[Entity] AS [Entity],
			[localization].[Entity] AS [LocalizationEntity]
		FROM 
			#tmpCatalogEntities [entities]
		INNER JOIN
			[sitecore_commerce_storage].[CatalogEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
		LEFT OUTER JOIN
			[sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]

        DROP TABLE #tmpCatalogEntities

        END
	ELSE IF (@TableName = 'ContentEntities')
    BEGIN

		SELECT 
			[entities].[UniqueId],
			[entities].[Id],
			[entities].[EntityVersion]
		INTO 
			#tmpContentEntities
		FROM (
			SELECT
				[innerEntities].[UniqueId],
				[innerEntities].[Id],
				[innerEntities].[EntityVersion],
				ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
			FROM
				[sitecore_commerce_storage].[ContentEntities] [innerEntities]
			JOIN @EntityIds ids ON ids.EntityId = [innerEntities].[Id]
			WHERE 
				[innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND ((ids.EntityVersion IS NULL AND ([Published] = 1 OR @IgnorePublished = 1)) OR (([innerEntities].[EntityVersion] = ids.EntityVersion) AND ([Published] = 1 OR @IgnorePublished = 1)))
		) [entities]
		WHERE rowNumber = 1

        
		SELECT 
			[entities].[Id],
			[entities].[UniqueId],
			[entities].[EntityVersion],
			[entity].[Entity] AS [Entity],
			[localization].[Entity] AS [LocalizationEntity]
		FROM 
			#tmpContentEntities [entities]
		INNER JOIN
			[sitecore_commerce_storage].[ContentEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
		LEFT OUTER JOIN
			[sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]

        DROP TABLE #tmpContentEntities

        END
	ELSE IF (@TableName = 'CustomersEntities')
    BEGIN

		SELECT 
			[entities].[UniqueId],
			[entities].[Id],
			[entities].[EntityVersion]
		INTO 
			#tmpCustomersEntities
		FROM (
			SELECT
				[innerEntities].[UniqueId],
				[innerEntities].[Id],
				[innerEntities].[EntityVersion],
				ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
			FROM
				[sitecore_commerce_storage].[CustomersEntities] [innerEntities]
			JOIN @EntityIds ids ON ids.EntityId = [innerEntities].[Id]
			WHERE 
				[innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND ((ids.EntityVersion IS NULL AND ([Published] = 1 OR @IgnorePublished = 1)) OR (([innerEntities].[EntityVersion] = ids.EntityVersion) AND ([Published] = 1 OR @IgnorePublished = 1)))
		) [entities]
		WHERE rowNumber = 1

        
		SELECT 
			[entities].[Id],
			[entities].[UniqueId],
			[entities].[EntityVersion],
			[entity].[Entity] AS [Entity],
			[localization].[Entity] AS [LocalizationEntity]
		FROM 
			#tmpCustomersEntities [entities]
		INNER JOIN
			[sitecore_commerce_storage].[CustomersEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
		LEFT OUTER JOIN
			[sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]

        DROP TABLE #tmpCustomersEntities

        END
	ELSE IF (@TableName = 'InventoryEntities')
    BEGIN

		SELECT 
			[entities].[UniqueId],
			[entities].[Id],
			[entities].[EntityVersion]
		INTO 
			#tmpInventoryEntities
		FROM (
			SELECT
				[innerEntities].[UniqueId],
				[innerEntities].[Id],
				[innerEntities].[EntityVersion],
				ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
			FROM
				[sitecore_commerce_storage].[InventoryEntities] [innerEntities]
			JOIN @EntityIds ids ON ids.EntityId = [innerEntities].[Id]
			WHERE 
				[innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND ((ids.EntityVersion IS NULL AND ([Published] = 1 OR @IgnorePublished = 1)) OR (([innerEntities].[EntityVersion] = ids.EntityVersion) AND ([Published] = 1 OR @IgnorePublished = 1)))
		) [entities]
		WHERE rowNumber = 1

        
		SELECT 
			[entities].[Id],
			[entities].[UniqueId],
			[entities].[EntityVersion],
			[entity].[Entity] AS [Entity],
			[localization].[Entity] AS [LocalizationEntity]
		FROM 
			#tmpInventoryEntities [entities]
		INNER JOIN
			[sitecore_commerce_storage].[InventoryEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
		LEFT OUTER JOIN
			[sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]

        DROP TABLE #tmpInventoryEntities

        END
	ELSE IF (@TableName = 'LocalizationEntities')
    BEGIN

		SELECT 
			[entities].[UniqueId],
			[entities].[Id],
			[entities].[EntityVersion]
		INTO 
			#tmpLocalizationEntities
		FROM (
			SELECT
				[innerEntities].[UniqueId],
				[innerEntities].[Id],
				[innerEntities].[EntityVersion],
				ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
			FROM
				[sitecore_commerce_storage].[LocalizationEntities] [innerEntities]
			JOIN @EntityIds ids ON ids.EntityId = [innerEntities].[Id]
			WHERE 
				[innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND ((ids.EntityVersion IS NULL AND ([Published] = 1 OR @IgnorePublished = 1)) OR (([innerEntities].[EntityVersion] = ids.EntityVersion) AND ([Published] = 1 OR @IgnorePublished = 1)))
		) [entities]
		WHERE rowNumber = 1

        
        SELECT 
			[entities].[Id],
			[entities].[UniqueId],
			[entities].[EntityVersion],
			[entity].[Entity] AS [Entity],
			NULL AS [LocalizationEntity]
		FROM 
			#tmpLocalizationEntities [entities]
		INNER JOIN
			[sitecore_commerce_storage].[LocalizationEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]

        DROP TABLE #tmpLocalizationEntities

       END
	ELSE IF (@TableName = 'OrdersEntities')
    BEGIN

		SELECT 
			[entities].[UniqueId],
			[entities].[Id],
			[entities].[EntityVersion]
		INTO 
			#tmpOrdersEntities
		FROM (
			SELECT
				[innerEntities].[UniqueId],
				[innerEntities].[Id],
				[innerEntities].[EntityVersion],
				ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
			FROM
				[sitecore_commerce_storage].[OrdersEntities] [innerEntities]
			JOIN @EntityIds ids ON ids.EntityId = [innerEntities].[Id]
			WHERE 
				[innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND ((ids.EntityVersion IS NULL AND ([Published] = 1 OR @IgnorePublished = 1)) OR (([innerEntities].[EntityVersion] = ids.EntityVersion) AND ([Published] = 1 OR @IgnorePublished = 1)))
		) [entities]
		WHERE rowNumber = 1

        
		SELECT 
			[entities].[Id],
			[entities].[UniqueId],
			[entities].[EntityVersion],
			[entity].[Entity] AS [Entity],
			[localization].[Entity] AS [LocalizationEntity]
		FROM 
			#tmpOrdersEntities [entities]
		INNER JOIN
			[sitecore_commerce_storage].[OrdersEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
		LEFT OUTER JOIN
			[sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]

        DROP TABLE #tmpOrdersEntities

        END
	ELSE IF (@TableName = 'PricingEntities')
    BEGIN

		SELECT 
			[entities].[UniqueId],
			[entities].[Id],
			[entities].[EntityVersion]
		INTO 
			#tmpPricingEntities
		FROM (
			SELECT
				[innerEntities].[UniqueId],
				[innerEntities].[Id],
				[innerEntities].[EntityVersion],
				ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
			FROM
				[sitecore_commerce_storage].[PricingEntities] [innerEntities]
			JOIN @EntityIds ids ON ids.EntityId = [innerEntities].[Id]
			WHERE 
				[innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND ((ids.EntityVersion IS NULL AND ([Published] = 1 OR @IgnorePublished = 1)) OR (([innerEntities].[EntityVersion] = ids.EntityVersion) AND ([Published] = 1 OR @IgnorePublished = 1)))
		) [entities]
		WHERE rowNumber = 1

        
		SELECT 
			[entities].[Id],
			[entities].[UniqueId],
			[entities].[EntityVersion],
			[entity].[Entity] AS [Entity],
			[localization].[Entity] AS [LocalizationEntity]
		FROM 
			#tmpPricingEntities [entities]
		INNER JOIN
			[sitecore_commerce_storage].[PricingEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
		LEFT OUTER JOIN
			[sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]

        DROP TABLE #tmpPricingEntities

        END
	ELSE IF (@TableName = 'PromotionsEntities')
    BEGIN

		SELECT 
			[entities].[UniqueId],
			[entities].[Id],
			[entities].[EntityVersion]
		INTO 
			#tmpPromotionsEntities
		FROM (
			SELECT
				[innerEntities].[UniqueId],
				[innerEntities].[Id],
				[innerEntities].[EntityVersion],
				ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
			FROM
				[sitecore_commerce_storage].[PromotionsEntities] [innerEntities]
			JOIN @EntityIds ids ON ids.EntityId = [innerEntities].[Id]
			WHERE 
				[innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND ((ids.EntityVersion IS NULL AND ([Published] = 1 OR @IgnorePublished = 1)) OR (([innerEntities].[EntityVersion] = ids.EntityVersion) AND ([Published] = 1 OR @IgnorePublished = 1)))
		) [entities]
		WHERE rowNumber = 1

        
		SELECT 
			[entities].[Id],
			[entities].[UniqueId],
			[entities].[EntityVersion],
			[entity].[Entity] AS [Entity],
			[localization].[Entity] AS [LocalizationEntity]
		FROM 
			#tmpPromotionsEntities [entities]
		INNER JOIN
			[sitecore_commerce_storage].[PromotionsEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
		LEFT OUTER JOIN
			[sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]

        DROP TABLE #tmpPromotionsEntities

        END
	ELSE IF (@TableName = 'RelationshipDefinitionEntities')
    BEGIN

		SELECT 
			[entities].[UniqueId],
			[entities].[Id],
			[entities].[EntityVersion]
		INTO 
			#tmpRelationshipDefinitionEntities
		FROM (
			SELECT
				[innerEntities].[UniqueId],
				[innerEntities].[Id],
				[innerEntities].[EntityVersion],
				ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
			FROM
				[sitecore_commerce_storage].[RelationshipDefinitionEntities] [innerEntities]
			JOIN @EntityIds ids ON ids.EntityId = [innerEntities].[Id]
			WHERE 
				[innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND ((ids.EntityVersion IS NULL AND ([Published] = 1 OR @IgnorePublished = 1)) OR (([innerEntities].[EntityVersion] = ids.EntityVersion) AND ([Published] = 1 OR @IgnorePublished = 1)))
		) [entities]
		WHERE rowNumber = 1

        
		SELECT 
			[entities].[Id],
			[entities].[UniqueId],
			[entities].[EntityVersion],
			[entity].[Entity] AS [Entity],
			[localization].[Entity] AS [LocalizationEntity]
		FROM 
			#tmpRelationshipDefinitionEntities [entities]
		INNER JOIN
			[sitecore_commerce_storage].[RelationshipDefinitionEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
		LEFT OUTER JOIN
			[sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]

        DROP TABLE #tmpRelationshipDefinitionEntities

        END
	ELSE IF (@TableName = 'VersioningEntities')
    BEGIN

		SELECT 
			[entities].[UniqueId],
			[entities].[Id],
			[entities].[EntityVersion]
		INTO 
			#tmpVersioningEntities
		FROM (
			SELECT
				[innerEntities].[UniqueId],
				[innerEntities].[Id],
				[innerEntities].[EntityVersion],
				ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
			FROM
				[sitecore_commerce_storage].[VersioningEntities] [innerEntities]
			JOIN @EntityIds ids ON ids.EntityId = [innerEntities].[Id]
			WHERE 
				[innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND ((ids.EntityVersion IS NULL AND ([Published] = 1 OR @IgnorePublished = 1)) OR (([innerEntities].[EntityVersion] = ids.EntityVersion) AND ([Published] = 1 OR @IgnorePublished = 1)))
		) [entities]
		WHERE rowNumber = 1

        
        SELECT 
			[entities].[Id],
			[entities].[UniqueId],
			[entities].[EntityVersion],
			[entity].[Entity] AS [Entity],
			NULL AS [LocalizationEntity]
		FROM 
			#tmpVersioningEntities [entities]
		INNER JOIN
			[sitecore_commerce_storage].[VersioningEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]

        DROP TABLE #tmpVersioningEntities

       END

END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[CatalogInsertMappings]
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[CatalogGetMappings]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[CatalogGetMappings]
(
	@ArtifactStoreId UNIQUEIDENTIFIER
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

       SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

        SELECT [EntityId]
		,[EntityVersion]
		,[Published]
		,[VariationId]
		,[Mappings].[SitecoreId]
		,[DeterministicId]
		,[ParentId]
		,[IsBundle]
		,[ParentCatalog]
       FROM [sitecore_commerce_storage].[Mappings]
       WHERE EntityId LIKE 'Entity-Catalog-%' AND ParentId IS NOT NULL AND ArtifactStoreId = @ArtifactStoreId

       UNION 

       SELECT [EntityId]
		,[EntityVersion]
		,[Published]
		,[VariationId]
		,[Mappings].[SitecoreId]
		,[DeterministicId]
		,[ParentId]
		,[IsBundle]
		,[ParentCatalog]
       FROM [sitecore_commerce_storage].[Mappings]
	   WHERE ArtifactStoreId = @ArtifactStoreId
END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[CatalogGetMappingsForId]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[CatalogGetMappingsForId]
(
	@DeterministicId uniqueidentifier
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @sitecoreId uniqueidentifier
	SELECT @sitecoreId =  (SELECT TOP 1 [SitecoreId] FROM [sitecore_commerce_storage].[Mappings] WHERE DeterministicId = @DeterministicId)


	SELECT DISTINCT [EntityId]
		,[EntityVersion]
		,[Published]
		,[VariationId]
		,[Mappings].[SitecoreId]
		,[DeterministicId]
		,[ParentId]
		,[IsBundle]
		,[ParentCatalog]
	FROM [sitecore_commerce_storage].[Mappings] WITH (NOLOCK)
	WHERE 
	SitecoreId = @sitecoreId
	AND ParentId IS NOT NULL

	UNION 

	SELECT DISTINCT [EntityId]
		,[EntityVersion]
		,[Published]
		,[VariationId]
		,[Mappings].[SitecoreId]
		,[DeterministicId]
		,[ParentId]
		,[IsBundle]
		,[ParentCatalog]
	FROM [sitecore_commerce_storage].[Mappings] WITH (NOLOCK)
	WHERE [Mappings].[SitecoreId] = @sitecoreId
	ORDER BY VariationId ASC
END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[CatalogUpdateMappings]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[CatalogUpdateMappings]
(
	@Id NVARCHAR(150),
	@EntityVersion INT,
	@Published BIT,
	@ArtifactStoreId UNIQUEIDENTIFIER,
	@SitecoreId UNIQUEIDENTIFIER,
	@ParentCatalogList NVARCHAR(MAX),
	@CatalogToEntityList NVARCHAR(MAX),
	@ChildrenCategoryList NVARCHAR(MAX),
	@ChildrenSellableItemList NVARCHAR(MAX),
	@ParentCategoryList NVARCHAR(MAX),
	@IsBundle BIT,
	@ItemVariations NVARCHAR(MAX)
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON

	DELETE FROM 
		[sitecore_commerce_storage].[Mappings]
	WHERE 
		ArtifactStoreId = @ArtifactStoreId AND EntityId = @Id AND EntityVersion = @EntityVersion

	DECLARE @CatalogMappings TABLE 
	(
		Id NVARCHAR(150),
		EntityVersion INT,
		Published BIT,
		ArtifactStoreId UNIQUEIDENTIFIER NOT NULL,
		SitecoreId UNIQUEIDENTIFIER,
		ParentCatalogList NVARCHAR(MAX) NULL,
		CatalogToEntityList NVARCHAR(MAX) NULL,
		ChildrenCategoryList NVARCHAR(MAX) NULL,
		ChildrenSellableItemList NVARCHAR(MAX) NULL,
		ParentCategoryList NVARCHAR(MAX) NULL,
		IsBundle BIT NULL,
		ItemVariations NVARCHAR(MAX) NULL
	)

	INSERT INTO
		@CatalogMappings 
	SELECT 
		@Id, 
		@EntityVersion, 
		@Published, 
		@ArtifactStoreId, 
		@SitecoreId, 
		@ParentCatalogList, 
		@CatalogToEntityList, 
		@ChildrenCategoryList, 
		@ChildrenSellableItemList, 
		@ParentCategoryList,
		@IsBundle, 
		@ItemVariations

	IF(@Id LIKE 'Entity-Catalog-%')
	BEGIN
		INSERT INTO 
			[sitecore_commerce_storage].[Mappings]
		SELECT DISTINCT
			NEWID() AS Id
			,@Id AS EntityId
			,@EntityVersion AS EntityVersion
			,@Published AS Published
			,@ArtifactStoreId AS ArtifactStoreId
			,NULL AS VariationId
			,@SitecoreId AS SitecoreId
			,@SitecoreId AS DeterministicId
			,IIF(LEN(@ParentCatalogList) > 0, @ParentCatalogList, NULL) AS ParentId
			,NULL AS [IsBundle]
            ,NULL AS ParentCatalog
		FROM 
			@CatalogMappings
	END
	ELSE IF(@Id LIKE 'Entity-Category-%')
	BEGIN
		INSERT INTO	
			[sitecore_commerce_storage].[Mappings]
		SELECT DISTINCT
			NEWID() AS Id
			,@Id AS EntityId
			,@EntityVersion AS EntityVersion
			,@Published AS Published
			,@ArtifactStoreId AS ArtifactStoreId
			,NULL AS VariationId
			,@SitecoreId AS SitecoreId
			,@SitecoreId AS DeterministicId
			,IIF(LEN(ParentCategory.VALUE) > 0, ParentCategory.VALUE, IIF(LEN(ParentCatalog.VALUE) > 0, ParentCatalog.VALUE, NULL)) AS ParentId
			,NULL AS [IsBundle]
            ,ParentCatalog.value AS ParentCatalog
		FROM 
			@CatalogMappings
				OUTER APPLY STRING_SPLIT(ParentCatalogList, '|') AS ParentCatalog
				OUTER APPLY STRING_SPLIT(ParentCategoryList, '|') AS ParentCategory
	END
	ELSE IF(@Id LIKE 'Entity-SellableItem-%')
	BEGIN
		INSERT INTO 
			[sitecore_commerce_storage].[Mappings]
		SELECT 
			NEWID() AS Id
			,@Id AS EntityId
			,@EntityVersion AS EntityVersion
			,@Published AS Published
			,@ArtifactStoreId AS ArtifactStoreId
			,NULL AS VariationId
			,@SitecoreId AS SitecoreId
			,CONVERT(UNIQUEIDENTIFIER, HashBytes('MD5', CAST(LOWER(CONCAT(@SitecoreId, '|', ParentCategory.VALUE)) AS VARCHAR(100))), 2) AS DeterministicId
			,IIF(LEN(ParentCategory.VALUE) > 0, ParentCategory.VALUE, NULL) AS ParentId
			,@IsBundle AS [IsBundle]
            ,IIF(LEN(ParentCategory.VALUE) > 0, (SELECT TOP 1 [ParentCatalog] FROM [sitecore_commerce_storage].[Mappings] WHERE [DeterministicId] = ParentCategory.VALUE), NULL) AS ParentCatalog
		FROM
			@CatalogMappings
				CROSS APPLY STRING_SPLIT(ParentCategoryList, '|') AS ParentCategory
		
		UNION
		
		SELECT 
			NEWID() AS Id
			,@Id AS EntityId
			,@EntityVersion AS EntityVersion
			,@Published AS Published
			,@ArtifactStoreId AS ArtifactStoreId
			,ItemVariations.VALUE AS VariationId
			,@SitecoreId AS SitecoreId
			,CONVERT(UNIQUEIDENTIFIER, HashBytes('MD5', 
				CAST(CONCAT(@Id, '|', ItemVariations.VALUE, '|', LOWER(CONVERT(UNIQUEIDENTIFIER, HashBytes('MD5', CAST(LOWER(CONCAT(@SitecoreId, '|', ParentCategory.VALUE)) AS VARCHAR(100))), 2))) AS varchar(200))
				)) 	 AS DeterministicId
			,CONVERT(UNIQUEIDENTIFIER, HashBytes('MD5', CAST(LOWER(CONCAT(@SitecoreId, '|', ParentCategory.VALUE)) AS VARCHAR(100))), 2) AS ParentId
			,@IsBundle AS [IsBundle]
            ,IIF(LEN(ParentCategory.VALUE) > 0, (SELECT TOP 1 [ParentCatalog] FROM [sitecore_commerce_storage].[Mappings] WHERE [DeterministicId] = ParentCategory.VALUE), NULL) AS ParentCatalog
		FROM 
			@CatalogMappings
				OUTER APPLY STRING_SPLIT(ParentCategoryList, '|') AS ParentCategory
				CROSS APPLY STRING_SPLIT(ItemVariations, '|') AS ItemVariations
				
		UNION

		SELECT 
			NEWID() AS Id
			,@Id AS EntityId
			,@EntityVersion AS EntityVersion
			,@Published AS Published
			,@ArtifactStoreId AS ArtifactStoreId
			,NULL AS VariationId
			,@SitecoreId AS SitecoreId
			,CONVERT(UNIQUEIDENTIFIER, HashBytes('MD5', CAST(LOWER(CONCAT(@SitecoreId, '|', CatalogEntities.VALUE)) AS VARCHAR(100))), 2) AS DeterministicId
			,IIF(LEN(CatalogEntities.VALUE) > 0, CatalogEntities.VALUE, NULL) AS ParentId
			,@IsBundle AS [IsBundle]
            ,IIF(LEN(CatalogEntities.VALUE) > 0, (SELECT TOP 1 [DeterministicId] FROM [sitecore_commerce_storage].[Mappings] WHERE [DeterministicId] = CatalogEntities.VALUE), NULL) AS ParentCatalog
		FROM 
			@CatalogMappings
				CROSS APPLY STRING_SPLIT(CatalogToEntityList, '|') AS CatalogEntities
		WHERE 
			LEN(CatalogEntities.VALUE) > 0
		
		UNION
		
		SELECT 
			NEWID() AS Id
			,@Id AS EntityId
			,@EntityVersion AS EntityVersion
			,@Published AS Published
			,@ArtifactStoreId AS ArtifactStoreId
			,ItemVariations.VALUE AS VariationId
			,@SitecoreId AS SitecoreId
			,CONVERT(UNIQUEIDENTIFIER, HashBytes('MD5', 
				CAST(CONCAT(@Id, '|', ItemVariations.VALUE, '|', LOWER(CONVERT(UNIQUEIDENTIFIER, HashBytes('MD5', CAST(LOWER(CONCAT(@SitecoreId, '|', CatalogEntities.VALUE)) AS VARCHAR(100))), 2))) AS varchar(200))
				)) 	 AS DeterministicId
			,CONVERT(UNIQUEIDENTIFIER, HashBytes('MD5', CAST(LOWER(CONCAT(@SitecoreId, '|', CatalogEntities.VALUE)) AS VARCHAR(100))), 2) AS ParentId
			,@IsBundle AS [IsBundle]
            ,IIF(LEN(CatalogEntities.VALUE) > 0, (SELECT TOP 1 [DeterministicId] FROM [sitecore_commerce_storage].[Mappings] WHERE [DeterministicId] = CatalogEntities.VALUE), NULL) AS ParentCatalog
		FROM 
			@CatalogMappings
				OUTER APPLY STRING_SPLIT(CatalogToEntityList, '|') AS CatalogEntities
				CROSS APPLY STRING_SPLIT(ItemVariations, '|') AS ItemVariations
		WHERE 
			LEN(CatalogEntities.VALUE) > 0
	END
END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[InsertEntities]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[InsertEntities]
(
	@TableName NVARCHAR(150) = 'CommerceEntities',
	@ArtifactStoreId UNIQUEIDENTIFIER,
	@Entities PersistEntityList READONLY
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON

	IF (@TableName = '')
		SET @TableName = 'CommerceEntities'

	IF (@TableName = 'CommerceEntities')
	BEGIN
		INSERT INTO [sitecore_commerce_storage].[CommerceEntities]
		(
			[UniqueId],
			[Id],
			[ArtifactStoreId],
			[ConcurrencyVersion],
			[EntityVersion],
			[Published]
		) 
		SELECT
			[json].[UniqueId],
			[json].[Id],
			@ArtifactStoreId,
			[json].[ConcurrencyVersion],
			[json].[EntityVersion],
			[json].[Published]
		FROM
			@Entities [entity]
		CROSS APPLY OPENJSON([entity].[Entity])
		WITH
		(
			[UniqueId] UNIQUEIDENTIFIER '$.UniqueId',
			[Id] NVARCHAR(150) '$.Id',
			[ConcurrencyVersion] INT '$.Version',
			[EntityVersion] INT '$.EntityVersion',
			[Published] BIT '$.Published'
		) [json]

		INSERT INTO [sitecore_commerce_storage].[CommerceEntity]
		(
			[UniqueId],
			[ArtifactStoreId],
			[Entity]
		) 
		SELECT
			JSON_VALUE([entity].[Entity], '$.UniqueId'),
			@ArtifactStoreId,
			[entity].[Entity]
		FROM
			@Entities [entity]

	END
	ELSE IF (@TableName = 'CatalogEntities')
	BEGIN
		INSERT INTO [sitecore_commerce_storage].[CatalogEntities]
		(
			[UniqueId],
			[Id],
			[ArtifactStoreId],
			[ConcurrencyVersion],
			[EntityVersion],
			[Published]
		) 
		SELECT
			[json].[UniqueId],
			[json].[Id],
			@ArtifactStoreId,
			[json].[ConcurrencyVersion],
			[json].[EntityVersion],
			[json].[Published]
		FROM
			@Entities [entity]
		CROSS APPLY OPENJSON([entity].[Entity])
		WITH
		(
			[UniqueId] UNIQUEIDENTIFIER '$.UniqueId',
			[Id] NVARCHAR(150) '$.Id',
			[ConcurrencyVersion] INT '$.Version',
			[EntityVersion] INT '$.EntityVersion',
			[Published] BIT '$.Published'
		) [json]

		INSERT INTO [sitecore_commerce_storage].[CatalogEntity]
		(
			[UniqueId],
			[ArtifactStoreId],
			[Entity]
		) 
		SELECT
			JSON_VALUE([entity].[Entity], '$.UniqueId'),
			@ArtifactStoreId,
			[entity].[Entity]
		FROM
			@Entities [entity]

		DECLARE
			@EntityId NVARCHAR(150),
			@EntityVersion INT,
			@EntityPublished BIT,
			@EntityJson NVARCHAR(MAX),
			@SitecoreId UNIQUEIDENTIFIER,
			@ParentCatalogList NVARCHAR(MAX),
			@CatalogToEntityList NVARCHAR(MAX),
			@ChildrenCategoryList NVARCHAR(MAX),
			@ChildrenSellableItemList NVARCHAR(MAX),
			@ParentCategoryList NVARCHAR(MAX),
			@IsBundle BIT,
			@ItemVariations NVARCHAR(MAX)

		DECLARE entityCursor CURSOR LOCAL FAST_FORWARD FOR
			SELECT
				[json].[Id],
				[json].[EntityVersion],
				[json].[Published],
				[entity].[Entity]
			FROM
				@Entities [entity]
			CROSS APPLY OPENJSON([entity].[Entity])
			WITH
			(
				[Id] NVARCHAR(150) '$.Id',
				[EntityVersion] INT '$.EntityVersion',
				[Published] BIT '$.Published'
			) [json]

		OPEN entityCursor

		FETCH NEXT FROM entityCursor INTO @EntityId, @EntityVersion, @EntityPublished, @EntityJson

		WHILE (@@FETCH_STATUS = 0)
		BEGIN
			SELECT
				@SitecoreId = [json].[SitecoreId],
				@ParentCatalogList = [json].[ParentCatalogList],
				@ParentCategoryList = [json].[ParentCategoryList],
				@ChildrenCategoryList = [json].[ChildrenCategoryList],
				@ChildrenSellableItemList = [json].[ChildrenSellableItemList],
				@CatalogToEntityList = [json].[CatalogToEntityList],
				@IsBundle = [json].[IsBundle],
				@ItemVariations = [json].[ItemVariations]
			FROM OPENJSON(@EntityJson) WITH
			(
				SitecoreId UNIQUEIDENTIFIER '$.SitecoreId',
				ParentCatalogList NVARCHAR(MAX)  '$.ParentCatalogList',
				ParentCategoryList NVARCHAR(MAX) '$.ParentCategoryList',
				ChildrenCategoryList NVARCHAR(MAX) '$.ChildrenCategoryList',
				ChildrenSellableItemList NVARCHAR(MAX) '$.ChildrenSellableItemList',
				CatalogToEntityList NVARCHAR(MAX) '$.CatalogToEntityList',
				IsBundle BIT '$.IsBundle',
				ItemVariations NVARCHAR(MAX) '$.ItemVariations'
			) AS [json]

			EXEC [sitecore_commerce_storage].[CatalogUpdateMappings] @EntityId, @EntityVersion, @EntityPublished, @ArtifactStoreId, @SitecoreId, @ParentCatalogList, @CatalogToEntityList, @ChildrenCategoryList, @ChildrenSellableItemList, @ParentCategoryList, @IsBundle, @ItemVariations

			FETCH NEXT FROM entityCursor INTO @EntityId, @EntityVersion, @EntityPublished, @EntityJson
		END

		CLOSE entityCursor
		DEALLOCATE entityCursor
	END
	ELSE IF (@TableName = 'ContentEntities')
	BEGIN
		INSERT INTO [sitecore_commerce_storage].[ContentEntities]
		(
			[UniqueId],
			[Id],
			[ArtifactStoreId],
			[ConcurrencyVersion],
			[EntityVersion],
			[Published]
		) 
		SELECT
			[json].[UniqueId],
			[json].[Id],
			@ArtifactStoreId,
			[json].[ConcurrencyVersion],
			[json].[EntityVersion],
			[json].[Published]
		FROM
			@Entities [entity]
		CROSS APPLY OPENJSON([entity].[Entity])
		WITH
		(
			[UniqueId] UNIQUEIDENTIFIER '$.UniqueId',
			[Id] NVARCHAR(150) '$.Id',
			[ConcurrencyVersion] INT '$.Version',
			[EntityVersion] INT '$.EntityVersion',
			[Published] BIT '$.Published'
		) [json]

		INSERT INTO [sitecore_commerce_storage].[ContentEntity]
		(
			[UniqueId],
			[ArtifactStoreId],
			[Entity]
		) 
		SELECT
			JSON_VALUE([entity].[Entity], '$.UniqueId'),
			@ArtifactStoreId,
			[entity].[Entity]
		FROM
			@Entities [entity]

	END
	ELSE IF (@TableName = 'CustomersEntities')
	BEGIN
		INSERT INTO [sitecore_commerce_storage].[CustomersEntities]
		(
			[UniqueId],
			[Id],
			[ArtifactStoreId],
			[ConcurrencyVersion],
			[EntityVersion],
			[Published]
		) 
		SELECT
			[json].[UniqueId],
			[json].[Id],
			@ArtifactStoreId,
			[json].[ConcurrencyVersion],
			[json].[EntityVersion],
			[json].[Published]
		FROM
			@Entities [entity]
		CROSS APPLY OPENJSON([entity].[Entity])
		WITH
		(
			[UniqueId] UNIQUEIDENTIFIER '$.UniqueId',
			[Id] NVARCHAR(150) '$.Id',
			[ConcurrencyVersion] INT '$.Version',
			[EntityVersion] INT '$.EntityVersion',
			[Published] BIT '$.Published'
		) [json]

		INSERT INTO [sitecore_commerce_storage].[CustomersEntity]
		(
			[UniqueId],
			[ArtifactStoreId],
			[Entity]
		) 
		SELECT
			JSON_VALUE([entity].[Entity], '$.UniqueId'),
			@ArtifactStoreId,
			[entity].[Entity]
		FROM
			@Entities [entity]

	END
	ELSE IF (@TableName = 'InventoryEntities')
	BEGIN
		INSERT INTO [sitecore_commerce_storage].[InventoryEntities]
		(
			[UniqueId],
			[Id],
			[ArtifactStoreId],
			[ConcurrencyVersion],
			[EntityVersion],
			[Published]
		) 
		SELECT
			[json].[UniqueId],
			[json].[Id],
			@ArtifactStoreId,
			[json].[ConcurrencyVersion],
			[json].[EntityVersion],
			[json].[Published]
		FROM
			@Entities [entity]
		CROSS APPLY OPENJSON([entity].[Entity])
		WITH
		(
			[UniqueId] UNIQUEIDENTIFIER '$.UniqueId',
			[Id] NVARCHAR(150) '$.Id',
			[ConcurrencyVersion] INT '$.Version',
			[EntityVersion] INT '$.EntityVersion',
			[Published] BIT '$.Published'
		) [json]

		INSERT INTO [sitecore_commerce_storage].[InventoryEntity]
		(
			[UniqueId],
			[ArtifactStoreId],
			[Entity]
		) 
		SELECT
			JSON_VALUE([entity].[Entity], '$.UniqueId'),
			@ArtifactStoreId,
			[entity].[Entity]
		FROM
			@Entities [entity]

	END
	ELSE IF (@TableName = 'LocalizationEntities')
	BEGIN
		INSERT INTO [sitecore_commerce_storage].[LocalizationEntities]
		(
			[UniqueId],
			[Id],
			[ArtifactStoreId],
			[ConcurrencyVersion],
			[EntityVersion],
			[Published]
		) 
		SELECT
			[json].[UniqueId],
			[json].[Id],
			@ArtifactStoreId,
			[json].[ConcurrencyVersion],
			[json].[EntityVersion],
			[json].[Published]
		FROM
			@Entities [entity]
		CROSS APPLY OPENJSON([entity].[Entity])
		WITH
		(
			[UniqueId] UNIQUEIDENTIFIER '$.UniqueId',
			[Id] NVARCHAR(150) '$.Id',
			[ConcurrencyVersion] INT '$.Version',
			[EntityVersion] INT '$.EntityVersion',
			[Published] BIT '$.Published'
		) [json]

		INSERT INTO [sitecore_commerce_storage].[LocalizationEntity]
		(
			[UniqueId],
			[ArtifactStoreId],
			[Entity],
			[EntityUniqueId]
		) 
		SELECT
			JSON_VALUE([entity].[Entity], '$.UniqueId'),
			@ArtifactStoreId,
			[entity].[Entity],
			IIF(JSON_VALUE([entity].[Entity], '$.RelatedEntity.EntityTargetUniqueId') IS NOT NULL, JSON_VALUE([entity].[Entity], '$.RelatedEntity.EntityTargetUniqueId'), '00000000-0000-0000-0000-000000000000')
		FROM
			@Entities [entity]

	END
	ELSE IF (@TableName = 'OrdersEntities')
	BEGIN
		INSERT INTO [sitecore_commerce_storage].[OrdersEntities]
		(
			[UniqueId],
			[Id],
			[ArtifactStoreId],
			[ConcurrencyVersion],
			[EntityVersion],
			[Published]
		) 
		SELECT
			[json].[UniqueId],
			[json].[Id],
			@ArtifactStoreId,
			[json].[ConcurrencyVersion],
			[json].[EntityVersion],
			[json].[Published]
		FROM
			@Entities [entity]
		CROSS APPLY OPENJSON([entity].[Entity])
		WITH
		(
			[UniqueId] UNIQUEIDENTIFIER '$.UniqueId',
			[Id] NVARCHAR(150) '$.Id',
			[ConcurrencyVersion] INT '$.Version',
			[EntityVersion] INT '$.EntityVersion',
			[Published] BIT '$.Published'
		) [json]

		INSERT INTO [sitecore_commerce_storage].[OrdersEntity]
		(
			[UniqueId],
			[ArtifactStoreId],
			[Entity]
		) 
		SELECT
			JSON_VALUE([entity].[Entity], '$.UniqueId'),
			@ArtifactStoreId,
			[entity].[Entity]
		FROM
			@Entities [entity]

	END
	ELSE IF (@TableName = 'PricingEntities')
	BEGIN
		INSERT INTO [sitecore_commerce_storage].[PricingEntities]
		(
			[UniqueId],
			[Id],
			[ArtifactStoreId],
			[ConcurrencyVersion],
			[EntityVersion],
			[Published]
		) 
		SELECT
			[json].[UniqueId],
			[json].[Id],
			@ArtifactStoreId,
			[json].[ConcurrencyVersion],
			[json].[EntityVersion],
			[json].[Published]
		FROM
			@Entities [entity]
		CROSS APPLY OPENJSON([entity].[Entity])
		WITH
		(
			[UniqueId] UNIQUEIDENTIFIER '$.UniqueId',
			[Id] NVARCHAR(150) '$.Id',
			[ConcurrencyVersion] INT '$.Version',
			[EntityVersion] INT '$.EntityVersion',
			[Published] BIT '$.Published'
		) [json]

		INSERT INTO [sitecore_commerce_storage].[PricingEntity]
		(
			[UniqueId],
			[ArtifactStoreId],
			[Entity]
		) 
		SELECT
			JSON_VALUE([entity].[Entity], '$.UniqueId'),
			@ArtifactStoreId,
			[entity].[Entity]
		FROM
			@Entities [entity]

	END
	ELSE IF (@TableName = 'PromotionsEntities')
	BEGIN
		INSERT INTO [sitecore_commerce_storage].[PromotionsEntities]
		(
			[UniqueId],
			[Id],
			[ArtifactStoreId],
			[ConcurrencyVersion],
			[EntityVersion],
			[Published]
		) 
		SELECT
			[json].[UniqueId],
			[json].[Id],
			@ArtifactStoreId,
			[json].[ConcurrencyVersion],
			[json].[EntityVersion],
			[json].[Published]
		FROM
			@Entities [entity]
		CROSS APPLY OPENJSON([entity].[Entity])
		WITH
		(
			[UniqueId] UNIQUEIDENTIFIER '$.UniqueId',
			[Id] NVARCHAR(150) '$.Id',
			[ConcurrencyVersion] INT '$.Version',
			[EntityVersion] INT '$.EntityVersion',
			[Published] BIT '$.Published'
		) [json]

		INSERT INTO [sitecore_commerce_storage].[PromotionsEntity]
		(
			[UniqueId],
			[ArtifactStoreId],
			[Entity]
		) 
		SELECT
			JSON_VALUE([entity].[Entity], '$.UniqueId'),
			@ArtifactStoreId,
			[entity].[Entity]
		FROM
			@Entities [entity]

	END
	ELSE IF (@TableName = 'RelationshipDefinitionEntities')
	BEGIN
		INSERT INTO [sitecore_commerce_storage].[RelationshipDefinitionEntities]
		(
			[UniqueId],
			[Id],
			[ArtifactStoreId],
			[ConcurrencyVersion],
			[EntityVersion],
			[Published]
		) 
		SELECT
			[json].[UniqueId],
			[json].[Id],
			@ArtifactStoreId,
			[json].[ConcurrencyVersion],
			[json].[EntityVersion],
			[json].[Published]
		FROM
			@Entities [entity]
		CROSS APPLY OPENJSON([entity].[Entity])
		WITH
		(
			[UniqueId] UNIQUEIDENTIFIER '$.UniqueId',
			[Id] NVARCHAR(150) '$.Id',
			[ConcurrencyVersion] INT '$.Version',
			[EntityVersion] INT '$.EntityVersion',
			[Published] BIT '$.Published'
		) [json]

		INSERT INTO [sitecore_commerce_storage].[RelationshipDefinitionEntity]
		(
			[UniqueId],
			[ArtifactStoreId],
			[Entity]
		) 
		SELECT
			JSON_VALUE([entity].[Entity], '$.UniqueId'),
			@ArtifactStoreId,
			[entity].[Entity]
		FROM
			@Entities [entity]

	END
	ELSE IF (@TableName = 'VersioningEntities')
	BEGIN
		INSERT INTO [sitecore_commerce_storage].[VersioningEntities]
		(
			[UniqueId],
			[Id],
			[ArtifactStoreId],
			[ConcurrencyVersion],
			[EntityVersion],
			[Published]
		) 
		SELECT
			[json].[UniqueId],
			[json].[Id],
			@ArtifactStoreId,
			[json].[ConcurrencyVersion],
			[json].[EntityVersion],
			[json].[Published]
		FROM
			@Entities [entity]
		CROSS APPLY OPENJSON([entity].[Entity])
		WITH
		(
			[UniqueId] UNIQUEIDENTIFIER '$.UniqueId',
			[Id] NVARCHAR(150) '$.Id',
			[ConcurrencyVersion] INT '$.Version',
			[EntityVersion] INT '$.EntityVersion',
			[Published] BIT '$.Published'
		) [json]

		INSERT INTO [sitecore_commerce_storage].[VersioningEntity]
		(
			[UniqueId],
			[ArtifactStoreId],
			[Entity]
		) 
		SELECT
			JSON_VALUE([entity].[Entity], '$.UniqueId'),
			@ArtifactStoreId,
			[entity].[Entity]
		FROM
			@Entities [entity]

	END

	-- Persist localization entities
	INSERT INTO [sitecore_commerce_storage].[LocalizationEntities]
	(
		[UniqueId],
		[Id],
		[ArtifactStoreId],
		[ConcurrencyVersion],
		[EntityVersion],
		[Published]
	)
	SELECT
		[json].[UniqueId],
		[json].[Id],
		@ArtifactStoreId,
		[json].[ConcurrencyVersion],
		[json].[EntityVersion],
		[json].[Published]
	FROM
		@Entities [entity]
	CROSS APPLY OPENJSON([entity].[LocalizationEntity])
	WITH
	(
		[UniqueId] UNIQUEIDENTIFIER '$.UniqueId',
		[Id] NVARCHAR(150) '$.Id',
		[ConcurrencyVersion] INT '$.Version',
		[EntityVersion] INT '$.EntityVersion',
		[Published] BIT '$.Published'
	) [json]
	WHERE
		[entity].[LocalizationEntity] IS NOT NULL

	INSERT INTO [sitecore_commerce_storage].[LocalizationEntity]
	(
		[UniqueId],
		[ArtifactStoreId],
		[Entity],
		[EntityUniqueId]
	)
	SELECT
		JSON_VALUE([entity].[LocalizationEntity], '$.UniqueId'),
		@ArtifactStoreId,
		[entity].[LocalizationEntity],
		JSON_VALUE([entity].[Entity], '$.UniqueId')
	FROM
		@Entities [entity]
	WHERE
		[entity].[LocalizationEntity] IS NOT NULL

	-- Persist versioning entities
	INSERT INTO [sitecore_commerce_storage].[VersioningEntities]
	(
		[UniqueId],
		[Id],
		[ArtifactStoreId],
		[ConcurrencyVersion],
		[EntityVersion],
		[Published]
	)
	SELECT
		[json].[UniqueId],
		[json].[Id],
		@ArtifactStoreId,
		[json].[ConcurrencyVersion],
		[json].[EntityVersion],
		[json].[Published]
	FROM
		@Entities [entity]
	CROSS APPLY OPENJSON([entity].[VersioningEntity])
	WITH
	(
		[UniqueId] UNIQUEIDENTIFIER '$.UniqueId',
		[Id] NVARCHAR(150) '$.Id',
		[ConcurrencyVersion] INT '$.Version',
		[EntityVersion] INT '$.EntityVersion',
		[Published] BIT '$.Published'
	) [json]
	WHERE
		[entity].[VersioningEntity] IS NOT NULL

	MERGE [sitecore_commerce_storage].[VersioningEntity] AS [target]
	USING
	(
		SELECT JSON_VALUE([VersioningEntity], '$.UniqueId'), [VersioningEntity] FROM @Entities WHERE [VersioningEntity] IS NOT NULL
	)
	AS [source] ([UniqueId], [Entity])
	ON ([target].[UniqueId] = [source].[UniqueId])
	WHEN MATCHED THEN 
	UPDATE SET
		[target].[Entity] = [source].[Entity]
	WHEN NOT MATCHED THEN
		INSERT
		(
			[UniqueId],
			[ArtifactStoreId],
			[Entity]
		)
		VALUES
		(
			[source].[UniqueId],
			@ArtifactStoreId,
			[source].[Entity]
		);
END
GO

/**************************************
* Add new stored procedures
**************************************/
PRINT N'Addings new stored procedures ...'
GO

CREATE PROCEDURE [sitecore_commerce_storage].[DeleteListEntities]
(
    @ListName NVARCHAR(150),
    @TableName NVARCHAR(150),
    @ArtifactStoreId UNIQUEIDENTIFIER,
    @Entities EntityIdList READONLY
)
WITH EXECUTE AS OWNER
AS
BEGIN

    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

    SET NOCOUNT ON

    IF @ListName IS NULL
    BEGIN
        RAISERROR('Parameter @ListName is NULL.', 16, 1) WITH NOWAIT;
        RETURN
    END

    IF @TableName IS NULL
    BEGIN
        RAISERROR('Parameter @TableName is NULL.', 16, 1) WITH NOWAIT;
        RETURN
    END

    IF @ArtifactStoreId IS NULL
    BEGIN
        RAISERROR('Parameter @ArtifactStoreId is NULL.', 16, 1) WITH NOWAIT;
        RETURN
    END

    IF (@TableName = '')
        SET @TableName = 'CommerceLists'

    DECLARE @EntityId VARCHAR(150)
    DECLARE @EntityVersion INT
    DECLARE id_cursor CURSOR FAST_FORWARD FOR SELECT EntityId, EntityVersion FROM @Entities

    IF (@TableName = 'CommerceLists')
    BEGIN
        OPEN id_cursor  
        FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        WHILE @@FETCH_STATUS = 0  
        BEGIN  
            DELETE FROM [sitecore_commerce_storage].[CommerceLists]
            WHERE
                [Id] = @EntityId AND 
                [ListName] = @ListName AND
                [ArtifactStoreId] = @ArtifactStoreId

            FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        END 
    END
    ELSE IF (@TableName = 'CatalogLists')
    BEGIN
        OPEN id_cursor  
        FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        WHILE @@FETCH_STATUS = 0  
        BEGIN  
            DELETE FROM [sitecore_commerce_storage].[CatalogLists]
            WHERE
                [Id] = @EntityId AND 
                [ListName] = @ListName AND
                [ArtifactStoreId] = @ArtifactStoreId

            FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        END 
    END
    ELSE IF (@TableName = 'ContentLists')
    BEGIN
        OPEN id_cursor  
        FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        WHILE @@FETCH_STATUS = 0  
        BEGIN  
            DELETE FROM [sitecore_commerce_storage].[ContentLists]
            WHERE
                [Id] = @EntityId AND 
                [ListName] = @ListName AND
                [ArtifactStoreId] = @ArtifactStoreId

            FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        END 
    END
    ELSE IF (@TableName = 'CustomersLists')
    BEGIN
        OPEN id_cursor  
        FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        WHILE @@FETCH_STATUS = 0  
        BEGIN  
            DELETE FROM [sitecore_commerce_storage].[CustomersLists]
            WHERE
                [Id] = @EntityId AND 
                [ListName] = @ListName AND
                [ArtifactStoreId] = @ArtifactStoreId

            FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        END 
    END
    ELSE IF (@TableName = 'InventoryLists')
    BEGIN
        OPEN id_cursor  
        FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        WHILE @@FETCH_STATUS = 0  
        BEGIN  
            DELETE FROM [sitecore_commerce_storage].[InventoryLists]
            WHERE
                [Id] = @EntityId AND 
                [ListName] = @ListName AND
                [ArtifactStoreId] = @ArtifactStoreId

            FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        END 
    END
    ELSE IF (@TableName = 'OrdersLists')
    BEGIN
        OPEN id_cursor  
        FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        WHILE @@FETCH_STATUS = 0  
        BEGIN  
            DELETE FROM [sitecore_commerce_storage].[OrdersLists]
            WHERE
                [Id] = @EntityId AND 
                [ListName] = @ListName AND
                [ArtifactStoreId] = @ArtifactStoreId

            FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        END 
    END
    ELSE IF (@TableName = 'PricingLists')
    BEGIN
        OPEN id_cursor  
        FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        WHILE @@FETCH_STATUS = 0  
        BEGIN  
            DELETE FROM [sitecore_commerce_storage].[PricingLists]
            WHERE
                [Id] = @EntityId AND 
                [ListName] = @ListName AND
                [ArtifactStoreId] = @ArtifactStoreId

            FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        END 
    END
    ELSE IF (@TableName = 'PromotionsLists')
    BEGIN
        OPEN id_cursor  
        FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        WHILE @@FETCH_STATUS = 0  
        BEGIN  
            DELETE FROM [sitecore_commerce_storage].[PromotionsLists]
            WHERE
                [Id] = @EntityId AND 
                [ListName] = @ListName AND
                [ArtifactStoreId] = @ArtifactStoreId

            FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        END 
    END
    ELSE IF (@TableName = 'RelationshipDefinitionLists')
    BEGIN
        OPEN id_cursor  
        FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        WHILE @@FETCH_STATUS = 0  
        BEGIN  
            DELETE FROM [sitecore_commerce_storage].[RelationshipDefinitionLists]
            WHERE
                [Id] = @EntityId AND 
                [ListName] = @ListName AND
                [ArtifactStoreId] = @ArtifactStoreId

            FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        END 
    END
    ELSE IF (@TableName = 'RelationshipLists')
    BEGIN
        OPEN id_cursor  
        FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        WHILE @@FETCH_STATUS = 0  
        BEGIN  
            DELETE FROM [sitecore_commerce_storage].[RelationshipLists]
            WHERE
                [Id] = @EntityId AND 
                [ListName] = @ListName AND
                [ArtifactStoreId] = @ArtifactStoreId

            FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        END 
    END
    ELSE IF (@TableName = 'WorkflowLists')
    BEGIN
        OPEN id_cursor  
        FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        WHILE @@FETCH_STATUS = 0  
        BEGIN  
            DELETE FROM [sitecore_commerce_storage].[WorkflowLists]
            WHERE
                [Id] = @EntityId AND 
				[EntityVersion] = @EntityVersion AND
                [ListName] = @ListName AND
                [ArtifactStoreId] = @ArtifactStoreId

            FETCH NEXT FROM id_cursor INTO @EntityId, @EntityVersion
        END 
    END
END
GO

CREATE PROCEDURE [sitecore_commerce_storage].[MinionDeleteListEntities]
(
	@ListName NVARCHAR(150),
	@TableName NVARCHAR(150),
	@ArtifactStoreId UNIQUEIDENTIFIER,
	@Entities EntityIdList READONLY
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON

    IF @ListName IS NULL
    BEGIN
        RAISERROR('Parameter @ListName is NULL.', 16, 1) WITH NOWAIT;
        RETURN
    END

    IF @TableName IS NULL
    BEGIN
        RAISERROR('Parameter @TableName is NULL.', 16, 1) WITH NOWAIT;
        RETURN
    END

    IF @ArtifactStoreId IS NULL
    BEGIN
        RAISERROR('Parameter @ArtifactStoreId is NULL.', 16, 1) WITH NOWAIT;
        RETURN
    END

	IF (@TableName = '')
		SET @TableName = 'CommerceLists'

	IF (@TableName = 'CommerceLists')
	BEGIN
		DELETE [list] FROM
			[sitecore_commerce_storage].[CommerceLists] [list]
        JOIN
            @Entities [entitiesToDelete] ON
            [list].[Id] = [entitiesToDelete].[EntityId] AND 
            [list].[ListName] = @ListName AND
            [list].[ArtifactStoreId] = @ArtifactStoreId
	END
	ELSE IF (@TableName = 'CatalogLists')
	BEGIN
		DELETE [list] FROM
			[sitecore_commerce_storage].[CatalogLists] [list]
        JOIN
            @Entities [entitiesToDelete] ON
            [list].[Id] = [entitiesToDelete].[EntityId] AND 
            [list].[ListName] = @ListName AND
            [list].[ArtifactStoreId] = @ArtifactStoreId
	END
	ELSE IF (@TableName = 'ContentLists')
	BEGIN
		DELETE [list] FROM
			[sitecore_commerce_storage].[ContentLists] [list]
        JOIN
            @Entities [entitiesToDelete] ON
            [list].[Id] = [entitiesToDelete].[EntityId] AND 
            [list].[ListName] = @ListName AND
            [list].[ArtifactStoreId] = @ArtifactStoreId
	END
	ELSE IF (@TableName = 'CustomersLists')
	BEGIN
		DELETE [list] FROM
			[sitecore_commerce_storage].[CustomersLists] [list]
        JOIN
            @Entities [entitiesToDelete] ON
            [list].[Id] = [entitiesToDelete].[EntityId] AND 
            [list].[ListName] = @ListName AND
            [list].[ArtifactStoreId] = @ArtifactStoreId
	END
	ELSE IF (@TableName = 'InventoryLists')
	BEGIN
		DELETE [list] FROM
			[sitecore_commerce_storage].[InventoryLists] [list]
        JOIN
            @Entities [entitiesToDelete] ON
            [list].[Id] = [entitiesToDelete].[EntityId] AND 
            [list].[ListName] = @ListName AND
            [list].[ArtifactStoreId] = @ArtifactStoreId
	END
	ELSE IF (@TableName = 'OrdersLists')
	BEGIN
		DELETE [list] FROM
			[sitecore_commerce_storage].[OrdersLists] [list]
        JOIN
            @Entities [entitiesToDelete] ON
            [list].[Id] = [entitiesToDelete].[EntityId] AND 
            [list].[ListName] = @ListName AND
            [list].[ArtifactStoreId] = @ArtifactStoreId
	END
	ELSE IF (@TableName = 'PricingLists')
	BEGIN
		DELETE [list] FROM
			[sitecore_commerce_storage].[PricingLists] [list]
        JOIN
            @Entities [entitiesToDelete] ON
            [list].[Id] = [entitiesToDelete].[EntityId] AND 
            [list].[ListName] = @ListName AND
            [list].[ArtifactStoreId] = @ArtifactStoreId
	END
	ELSE IF (@TableName = 'PromotionsLists')
	BEGIN
		DELETE [list] FROM
			[sitecore_commerce_storage].[PromotionsLists] [list]
        JOIN
            @Entities [entitiesToDelete] ON
            [list].[Id] = [entitiesToDelete].[EntityId] AND 
            [list].[ListName] = @ListName AND
            [list].[ArtifactStoreId] = @ArtifactStoreId
	END
	ELSE IF (@TableName = 'RelationshipDefinitionLists')
	BEGIN
		DELETE [list] FROM
			[sitecore_commerce_storage].[RelationshipDefinitionLists] [list]
        JOIN
            @Entities [entitiesToDelete] ON
            [list].[Id] = [entitiesToDelete].[EntityId] AND 
            [list].[ListName] = @ListName AND
            [list].[ArtifactStoreId] = @ArtifactStoreId
	END
	ELSE IF (@TableName = 'RelationshipLists')
	BEGIN
		DELETE [list] FROM
			[sitecore_commerce_storage].[RelationshipLists] [list]
        JOIN
            @Entities [entitiesToDelete] ON
            [list].[Id] = [entitiesToDelete].[EntityId] AND 
            [list].[ListName] = @ListName AND
            [list].[ArtifactStoreId] = @ArtifactStoreId
	END
	ELSE IF (@TableName = 'WorkflowLists')
	BEGIN
		DELETE [list] FROM
			[sitecore_commerce_storage].[WorkflowLists] [list]
        JOIN
            @Entities [entitiesToDelete] ON
            [list].[Id] = [entitiesToDelete].[EntityId] AND 
			[list].[EntityVersion] = [entitiesToDelete].[EntityVersion] AND 
            [list].[ListName] = @ListName AND
            [list].[ArtifactStoreId] = @ArtifactStoreId
	END
END
GO

CREATE PROCEDURE [sitecore_commerce_storage].[SelectEntitiesByUniqueIds]
(
    @EntityUniqueIds EntityUniqueIdList READONLY,
    @TableName NVARCHAR(150) = 'CommerceEntities'
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED

    IF (@TableName = '')
        SET @TableName = 'CommerceEntities'

    IF (@TableName = 'CommerceEntities')
    BEGIN
            SELECT 
                [entity].[UniqueId],
                [entity].[Entity] AS [Entity],
                [localization].[Entity] AS [LocalizationEntity]
            FROM
                [sitecore_commerce_storage].[CommerceEntity] [entity]
            INNER JOIN
                @EntityUniqueIds ids ON ids.UniqueId = [entity].[UniqueId]
            LEFT OUTER JOIN
                [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entity].[UniqueId] = [localization].[EntityUniqueId]
        END
    ELSE IF (@TableName = 'CatalogEntities')
    BEGIN
            SELECT 
                [entity].[UniqueId],
                [entity].[Entity] AS [Entity],
                [localization].[Entity] AS [LocalizationEntity]
            FROM
                [sitecore_commerce_storage].[CatalogEntity] [entity]
            INNER JOIN
                @EntityUniqueIds ids ON ids.UniqueId = [entity].[UniqueId]
            LEFT OUTER JOIN
                [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entity].[UniqueId] = [localization].[EntityUniqueId]
        END
    ELSE IF (@TableName = 'ContentEntities')
    BEGIN
            SELECT 
                [entity].[UniqueId],
                [entity].[Entity] AS [Entity],
                [localization].[Entity] AS [LocalizationEntity]
            FROM
                [sitecore_commerce_storage].[ContentEntity] [entity]
            INNER JOIN
                @EntityUniqueIds ids ON ids.UniqueId = [entity].[UniqueId]
            LEFT OUTER JOIN
                [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entity].[UniqueId] = [localization].[EntityUniqueId]
        END
    ELSE IF (@TableName = 'CustomersEntities')
    BEGIN
            SELECT 
                [entity].[UniqueId],
                [entity].[Entity] AS [Entity],
                [localization].[Entity] AS [LocalizationEntity]
            FROM
                [sitecore_commerce_storage].[CustomersEntity] [entity]
            INNER JOIN
                @EntityUniqueIds ids ON ids.UniqueId = [entity].[UniqueId]
            LEFT OUTER JOIN
                [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entity].[UniqueId] = [localization].[EntityUniqueId]
        END
    ELSE IF (@TableName = 'InventoryEntities')
    BEGIN
            SELECT 
                [entity].[UniqueId],
                [entity].[Entity] AS [Entity],
                [localization].[Entity] AS [LocalizationEntity]
            FROM
                [sitecore_commerce_storage].[InventoryEntity] [entity]
            INNER JOIN
                @EntityUniqueIds ids ON ids.UniqueId = [entity].[UniqueId]
            LEFT OUTER JOIN
                [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entity].[UniqueId] = [localization].[EntityUniqueId]
        END
    ELSE IF (@TableName = 'LocalizationEntities')
    BEGIN
            SELECT 
                [entity].[UniqueId],
                [entity].[Entity] AS [Entity],
                NULL AS [LocalizationEntity]
            FROM
                [sitecore_commerce_storage].[LocalizationEntity] [entity]
            INNER JOIN
                @EntityUniqueIds ids ON ids.UniqueId = [entity].[UniqueId]
       END
    ELSE IF (@TableName = 'OrdersEntities')
    BEGIN
            SELECT 
                [entity].[UniqueId],
                [entity].[Entity] AS [Entity],
                [localization].[Entity] AS [LocalizationEntity]
            FROM
                [sitecore_commerce_storage].[OrdersEntity] [entity]
            INNER JOIN
                @EntityUniqueIds ids ON ids.UniqueId = [entity].[UniqueId]
            LEFT OUTER JOIN
                [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entity].[UniqueId] = [localization].[EntityUniqueId]
        END
    ELSE IF (@TableName = 'PricingEntities')
    BEGIN
            SELECT 
                [entity].[UniqueId],
                [entity].[Entity] AS [Entity],
                [localization].[Entity] AS [LocalizationEntity]
            FROM
                [sitecore_commerce_storage].[PricingEntity] [entity]
            INNER JOIN
                @EntityUniqueIds ids ON ids.UniqueId = [entity].[UniqueId]
            LEFT OUTER JOIN
                [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entity].[UniqueId] = [localization].[EntityUniqueId]
        END
    ELSE IF (@TableName = 'PromotionsEntities')
    BEGIN
            SELECT 
                [entity].[UniqueId],
                [entity].[Entity] AS [Entity],
                [localization].[Entity] AS [LocalizationEntity]
            FROM
                [sitecore_commerce_storage].[PromotionsEntity] [entity]
            INNER JOIN
                @EntityUniqueIds ids ON ids.UniqueId = [entity].[UniqueId]
            LEFT OUTER JOIN
                [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entity].[UniqueId] = [localization].[EntityUniqueId]
        END
    ELSE IF (@TableName = 'RelationshipDefinitionEntities')
    BEGIN
            SELECT 
                [entity].[UniqueId],
                [entity].[Entity] AS [Entity],
                [localization].[Entity] AS [LocalizationEntity]
            FROM
                [sitecore_commerce_storage].[RelationshipDefinitionEntity] [entity]
            INNER JOIN
                @EntityUniqueIds ids ON ids.UniqueId = [entity].[UniqueId]
            LEFT OUTER JOIN
                [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entity].[UniqueId] = [localization].[EntityUniqueId]
        END
    ELSE IF (@TableName = 'VersioningEntities')
    BEGIN
            SELECT 
                [entity].[UniqueId],
                [entity].[Entity] AS [Entity],
                NULL AS [LocalizationEntity]
            FROM
                [sitecore_commerce_storage].[VersioningEntity] [entity]
            INNER JOIN
                @EntityUniqueIds ids ON ids.UniqueId = [entity].[UniqueId]
       END

END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[CatalogGetHierarchy]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[CatalogGetHierarchy]
(
	@EntityId NVARCHAR(150),
	@IgnorePublished BIT = 0
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON

    DECLARE @deterministicId UNIQUEIDENTIFIER

	DECLARE deterministicIdCursor CURSOR FORWARD_ONLY FOR
	SELECT [DeterministicId]
	FROM (
		SELECT
			[DeterministicId],
			ROW_NUMBER() OVER(PARTITION BY [DeterministicId] ORDER BY [EntityVersion] DESC) rowNumber
		FROM
			[sitecore_commerce_storage].[Mappings]
		WHERE
			[EntityId] = @EntityId AND [VariationId] IS NULL AND ([Published] = 1 OR @IgnorePublished = 1)
	) x
	WHERE x.rowNumber = 1

	OPEN deterministicIdCursor

	FETCH NEXT FROM deterministicIdCursor INTO @deterministicId

	WHILE @@FETCH_STATUS = 0
	BEGIN
		;WITH CTE AS (
			SELECT *
			FROM (
				SELECT
					[EntityId],
					[DeterministicId],
					[ParentId],
					0 AS [Level],
					ROW_NUMBER() OVER(PARTITION BY [DeterministicId] ORDER BY [EntityVersion] DESC) rowNumber
				FROM
					[sitecore_commerce_storage].[Mappings]
				WHERE
					[DeterministicId] = @deterministicId AND ([Published] = 1 OR @IgnorePublished = 1)
			) first
			WHERE first.rowNumber = 1

			UNION ALL

			SELECT
				[m].[EntityId],
				[m].[DeterministicId],
				[m].[ParentId],
				([Level] + 1) AS [Level],
				ROW_NUMBER() OVER(PARTITION BY [m].[DeterministicId] ORDER BY [m].[EntityVersion] DESC) rowNumber
			FROM
				[sitecore_commerce_storage].[Mappings] m
			INNER JOIN CTE c ON [m].[DeterministicId] = [c].[ParentId]  AND ([m].[Published] = 1 OR @IgnorePublished = 1)
		)
		SELECT EntityId, DeterministicId FROM CTE ORDER BY Level DESC

		FETCH NEXT FROM deterministicIdCursor INTO @deterministicId
	END

	CLOSE deterministicIdCursor
	DEALLOCATE deterministicIdCursor
END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[CatalogGetMappingsMaster]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[CatalogGetMappingsMaster]
(
	@ArtifactStoreId UNIQUEIDENTIFIER,
    @Skip INT = 0,
    @Take INT = 1000
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON

    -- Get catalogs
    SELECT
	    CONVERT(NVARCHAR(150), [SitecoreId]) AS [SitecoreId]
    INTO
	    #tmpCatalogs
    FROM
	    [sitecore_commerce_storage].[Mappings]
    WITH (NOLOCK)
    WHERE
	    [EntityId] LIKE 'Entity-Catalog-%' AND [ParentId] IS NOT NULL AND [ArtifactStoreId] = @ArtifactStoreId

    SELECT
	    [EntityId]
	    ,MAX([EntityVersion])
    FROM
	    [sitecore_commerce_storage].[Mappings]
    WITH (NOLOCK)
    WHERE
	    [EntityId] LIKE 'Entity-Catalog-%' AND [ParentId] IS NOT NULL AND [ArtifactStoreId] = @ArtifactStoreId
    GROUP BY [EntityId]

    UNION

    SELECT
        [EntityId],
        MAX([EntityVersion])
    FROM
        [sitecore_commerce_storage].[Mappings] [mappings]
    INNER JOIN #tmpCatalogs [catalog] ON [catalog].[SitecoreId] = [mappings].[ParentCatalog]
    WHERE [VariationId] IS NULL

    GROUP BY [EntityId]
    ORDER BY [EntityId]
    OFFSET @Skip ROWS
    FETCH NEXT @Take ROWS ONLY
END
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[CatalogGetMappingsWeb]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[CatalogGetMappingsWeb]
(
	@ArtifactStoreId UNIQUEIDENTIFIER,
    @Skip INT = 0,
    @Take INT = 1000
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON

    -- Get catalogs
    SELECT
	    CONVERT(NVARCHAR(150), [SitecoreId]) AS [SitecoreId]
    INTO
	    #tmpCatalogs
    FROM
	    [sitecore_commerce_storage].[Mappings]
    WITH (NOLOCK)
    WHERE 
	    [EntityId] LIKE 'Entity-Catalog-%' AND [ParentId] IS NOT NULL AND [ArtifactStoreId] = @ArtifactStoreId

    SELECT 
	    [EntityId]
	    ,MAX([EntityVersion])
    FROM 
	    [sitecore_commerce_storage].[Mappings]
    WITH (NOLOCK)
    WHERE 
	    [EntityId] LIKE 'Entity-Catalog-%' AND [ParentId] IS NOT NULL AND [Published] = 1 AND [ArtifactStoreId] = @ArtifactStoreId
    GROUP BY [EntityId]

    UNION 

    SELECT 
        [EntityId],
        MAX([EntityVersion])
    FROM 
        [sitecore_commerce_storage].[Mappings] [mappings]
    INNER JOIN #tmpCatalogs [catalog] ON [catalog].[SitecoreId] = [mappings].[ParentCatalog]
    WHERE [VariationId] IS NULL AND [Published] = 1

    GROUP BY [EntityId]
    ORDER BY [EntityId]
    OFFSET @Skip ROWS
    FETCH NEXT @Take ROWS ONLY
END
GO

/******************************************************************************
* Rebuilding catalog mappings (NOTE: KEEP THIS SECTION LAST)
******************************************************************************/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET NOCOUNT ON
GO

-- Clean the table before rebuilding the mappings
DELETE FROM [sitecore_commerce_storage].[Mappings]
GO

DECLARE @Id nvarchar(150)
DECLARE @EntityVersion int
DECLARE @ArtifactStoreId uniqueidentifier
DECLARE @Published bit
DECLARE @Entity nvarchar(max)

DECLARE entityCursor CURSOR
	LOCAL STATIC READ_ONLY FORWARD_ONLY
FOR
	SELECT [es].[Id], [es].[EntityVersion], [es].[ArtifactStoreId], [es].[Published], [e].[Entity]
	FROM [sitecore_commerce_storage].[CatalogEntities] [es]
	JOIN [sitecore_commerce_storage].[CatalogEntity] [e] ON [e].UniqueId = [es].UniqueId
	WHERE [Id] LIKE 'Entity-Catalog-%' OR [Id] LIKE 'Entity-Category-%' OR [Id] LIKE 'Entity-SellableItem-%'
	ORDER BY [Id]

OPEN entityCursor
FETCH NEXT FROM entityCursor INTO @Id, @EntityVersion, @ArtifactStoreId, @Published, @Entity
WHILE @@FETCH_STATUS = 0
BEGIN 
	DECLARE @SitecoreId uniqueidentifier
	DECLARE @ParentCatalogList NVARCHAR(MAX)
	DECLARE @CatalogToEntityList NVARCHAR(MAX)
	DECLARE @ChildrenCategoryList NVARCHAR(MAX)
	DECLARE @ChildrenSellableItemList NVARCHAR(MAX)
	DECLARE @ParentCategoryList NVARCHAR(MAX)
	DECLARE @IsBundle bit
	DECLARE @ItemVariations NVARCHAR(MAX)

	SELECT
		@SitecoreId = json.SitecoreId,
		@ParentCatalogList = json.ParentCatalogList,
		@ParentCategoryList = json.ParentCategoryList,
		@ChildrenCategoryList = json.ChildrenCategoryList,
		@ChildrenSellableItemList = json.ChildrenSellableItemList,
		@CatalogToEntityList = json.CatalogToEntityList,
		@IsBundle = json.IsBundle,
		@ItemVariations = json.ItemVariations
	FROM OPENJSON(@Entity) WITH (
		SitecoreId uniqueidentifier'$.SitecoreId',
		ParentCatalogList NVARCHAR(MAX) '$.ParentCatalogList',
		ParentCategoryList NVARCHAR(MAX) '$.ParentCategoryList',
		ChildrenCategoryList NVARCHAR(MAX) '$.ChildrenCategoryList',
		ChildrenSellableItemList NVARCHAR(MAX) '$.ChildrenSellableItemList',
		CatalogToEntityList NVARCHAR(MAX) '$.CatalogToEntityList',
		IsBundle bit '$.IsBundle',
		ItemVariations NVARCHAR(MAX) '$.ItemVariations') AS json

	EXEC [sitecore_commerce_storage].[CatalogUpdateMappings]
		@Id,
		@EntityVersion,
		@Published,
		@ArtifactStoreId,
		@SitecoreId,
		@ParentCatalogList,
		@CatalogToEntityList,
		@ChildrenCategoryList,
		@ChildrenSellableItemList,
		@ParentCategoryList,
		@IsBundle,
		@ItemVariations
	
	FETCH NEXT FROM entityCursor INTO @Id, @EntityVersion, @ArtifactStoreId, @Published, @Entity
END

CLOSE entityCursor;  
DEALLOCATE entityCursor; 
GO

DROP PROCEDURE IF EXISTS [sitecore_commerce_storage].[SelectListEntitiesByRange]
GO

CREATE PROCEDURE [sitecore_commerce_storage].[SelectListEntitiesByRange]
(
    @ListName nvarchar(150),
    @TableName nvarchar(150) = 'CommerceLists',
    @ArtifactStoreId UNIQUEIDENTIFIER,
    @Skip int = 0,
    @Take int = 1,
    @SortOrder int = 0,
    @IgnorePublished bit = 0
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON
    SET TRANSACTION ISOLATION LEVEL SNAPSHOT

    IF (@TableName = '')
        SET @TableName = 'CommerceLists'

    IF (@TableName = 'CommerceLists')
    BEGIN
        -- Get matching entities
        SELECT 
            [entities].[UniqueId],
            [entities].[Id],
            [entities].[EntityVersion]
        INTO 
            #tmpCommerceLists
        FROM (
            SELECT
                [innerEntities].[UniqueId],
                [innerEntities].[Id],
                [innerEntities].[EntityVersion],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[CommerceEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[CommerceLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY

        -- First result: Paged results
        SELECT 
            [entities].[Id],
            [entities].[UniqueId],
            [entities].[EntityVersion],
            [entity].[Entity] AS [Entity],
            [localization].[Entity] AS [LocalizationEntity]
        FROM 
            #tmpCommerceLists [entities]
        INNER JOIN
            [sitecore_commerce_storage].[CommerceEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
        LEFT OUTER JOIN
            [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC

        DROP TABLE #tmpCommerceLists

        -- Second result: Total count
        SELECT
            COUNT([entities].[Id]) AS [TotalCount]
        FROM (
            SELECT
                [innerEntities].[Id],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[CommerceEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[CommerceLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
    END
    ELSE IF (@TableName = 'CatalogLists')
    BEGIN
        -- Get matching entities
        SELECT 
            [entities].[UniqueId],
            [entities].[Id],
            [entities].[EntityVersion]
        INTO 
            #tmpCatalogLists
        FROM (
            SELECT
                [innerEntities].[UniqueId],
                [innerEntities].[Id],
                [innerEntities].[EntityVersion],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[CatalogEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[CatalogLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY

        -- First result: Paged results
        SELECT 
            [entities].[Id],
            [entities].[UniqueId],
            [entities].[EntityVersion],
            [entity].[Entity] AS [Entity],
            [localization].[Entity] AS [LocalizationEntity]
        FROM 
            #tmpCatalogLists [entities]
        INNER JOIN
            [sitecore_commerce_storage].[CatalogEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
        LEFT OUTER JOIN
            [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC

        DROP TABLE #tmpCatalogLists

        -- Second result: Total count
        SELECT
            COUNT([entities].[Id]) AS [TotalCount]
        FROM (
            SELECT
                [innerEntities].[Id],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[CatalogEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[CatalogLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
    END
    ELSE IF (@TableName = 'ContentLists')
    BEGIN
        -- Get matching entities
        SELECT 
            [entities].[UniqueId],
            [entities].[Id],
            [entities].[EntityVersion]
        INTO 
            #tmpContentLists
        FROM (
            SELECT
                [innerEntities].[UniqueId],
                [innerEntities].[Id],
                [innerEntities].[EntityVersion],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[ContentEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[ContentLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY

        -- First result: Paged results
        SELECT 
            [entities].[Id],
            [entities].[UniqueId],
            [entities].[EntityVersion],
            [entity].[Entity] AS [Entity],
            [localization].[Entity] AS [LocalizationEntity]
        FROM 
            #tmpContentLists [entities]
        INNER JOIN
            [sitecore_commerce_storage].[ContentEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
        LEFT OUTER JOIN
            [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC

        DROP TABLE #tmpContentLists

        -- Second result: Total count
        SELECT
            COUNT([entities].[Id]) AS [TotalCount]
        FROM (
            SELECT
                [innerEntities].[Id],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[ContentEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[ContentLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
    END
    ELSE IF (@TableName = 'CustomersLists')
    BEGIN
        -- Get matching entities
        SELECT 
            [entities].[UniqueId],
            [entities].[Id],
            [entities].[EntityVersion]
        INTO 
            #tmpCustomersLists
        FROM (
            SELECT
                [innerEntities].[UniqueId],
                [innerEntities].[Id],
                [innerEntities].[EntityVersion],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[CustomersEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[CustomersLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY

        -- First result: Paged results
        SELECT 
            [entities].[Id],
            [entities].[UniqueId],
            [entities].[EntityVersion],
            [entity].[Entity] AS [Entity],
            [localization].[Entity] AS [LocalizationEntity]
        FROM 
            #tmpCustomersLists [entities]
        INNER JOIN
            [sitecore_commerce_storage].[CustomersEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
        LEFT OUTER JOIN
            [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC

        DROP TABLE #tmpCustomersLists

        -- Second result: Total count
        SELECT
            COUNT([entities].[Id]) AS [TotalCount]
        FROM (
            SELECT
                [innerEntities].[Id],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[CustomersEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[CustomersLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
    END
    ELSE IF (@TableName = 'InventoryLists')
    BEGIN
        -- Get matching entities
        SELECT 
            [entities].[UniqueId],
            [entities].[Id],
            [entities].[EntityVersion]
        INTO 
            #tmpInventoryLists
        FROM (
            SELECT
                [innerEntities].[UniqueId],
                [innerEntities].[Id],
                [innerEntities].[EntityVersion],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[InventoryEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[InventoryLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY

        -- First result: Paged results
        SELECT 
            [entities].[Id],
            [entities].[UniqueId],
            [entities].[EntityVersion],
            [entity].[Entity] AS [Entity],
            [localization].[Entity] AS [LocalizationEntity]
        FROM 
            #tmpInventoryLists [entities]
        INNER JOIN
            [sitecore_commerce_storage].[InventoryEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
        LEFT OUTER JOIN
            [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC

        DROP TABLE #tmpInventoryLists

        -- Second result: Total count
        SELECT
            COUNT([entities].[Id]) AS [TotalCount]
        FROM (
            SELECT
                [innerEntities].[Id],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[InventoryEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[InventoryLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
    END
    ELSE IF (@TableName = 'OrdersLists')
    BEGIN
        -- Get matching entities
        SELECT 
            [entities].[UniqueId],
            [entities].[Id],
            [entities].[EntityVersion]
        INTO 
            #tmpOrdersLists
        FROM (
            SELECT
                [innerEntities].[UniqueId],
                [innerEntities].[Id],
                [innerEntities].[EntityVersion],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[OrdersEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[OrdersLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY

        -- First result: Paged results
        SELECT 
            [entities].[Id],
            [entities].[UniqueId],
            [entities].[EntityVersion],
            [entity].[Entity] AS [Entity],
            [localization].[Entity] AS [LocalizationEntity]
        FROM 
            #tmpOrdersLists [entities]
        INNER JOIN
            [sitecore_commerce_storage].[OrdersEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
        LEFT OUTER JOIN
            [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC

        DROP TABLE #tmpOrdersLists

        -- Second result: Total count
        SELECT
            COUNT([entities].[Id]) AS [TotalCount]
        FROM (
            SELECT
                [innerEntities].[Id],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[OrdersEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[OrdersLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
    END
    ELSE IF (@TableName = 'PricingLists')
    BEGIN
        -- Get matching entities
        SELECT 
            [entities].[UniqueId],
            [entities].[Id],
            [entities].[EntityVersion]
        INTO 
            #tmpPricingLists
        FROM (
            SELECT
                [innerEntities].[UniqueId],
                [innerEntities].[Id],
                [innerEntities].[EntityVersion],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[PricingEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[PricingLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY

        -- First result: Paged results
        SELECT 
            [entities].[Id],
            [entities].[UniqueId],
            [entities].[EntityVersion],
            [entity].[Entity] AS [Entity],
            [localization].[Entity] AS [LocalizationEntity]
        FROM 
            #tmpPricingLists [entities]
        INNER JOIN
            [sitecore_commerce_storage].[PricingEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
        LEFT OUTER JOIN
            [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC

        DROP TABLE #tmpPricingLists

        -- Second result: Total count
        SELECT
            COUNT([entities].[Id]) AS [TotalCount]
        FROM (
            SELECT
                [innerEntities].[Id],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[PricingEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[PricingLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
    END
    ELSE IF (@TableName = 'PromotionsLists')
    BEGIN
        -- Get matching entities
        SELECT 
            [entities].[UniqueId],
            [entities].[Id],
            [entities].[EntityVersion]
        INTO 
            #tmpPromotionsLists
        FROM (
            SELECT
                [innerEntities].[UniqueId],
                [innerEntities].[Id],
                [innerEntities].[EntityVersion],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[PromotionsEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[PromotionsLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY

        -- First result: Paged results
        SELECT 
            [entities].[Id],
            [entities].[UniqueId],
            [entities].[EntityVersion],
            [entity].[Entity] AS [Entity],
            [localization].[Entity] AS [LocalizationEntity]
        FROM 
            #tmpPromotionsLists [entities]
        INNER JOIN
            [sitecore_commerce_storage].[PromotionsEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
        LEFT OUTER JOIN
            [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC

        DROP TABLE #tmpPromotionsLists

        -- Second result: Total count
        SELECT
            COUNT([entities].[Id]) AS [TotalCount]
        FROM (
            SELECT
                [innerEntities].[Id],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[PromotionsEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[PromotionsLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
    END
    ELSE IF (@TableName = 'RelationshipDefinitionLists')
    BEGIN
        -- Get matching entities
        SELECT 
            [entities].[UniqueId],
            [entities].[Id],
            [entities].[EntityVersion]
        INTO 
            #tmpRelationshipDefinitionLists
        FROM (
            SELECT
                [innerEntities].[UniqueId],
                [innerEntities].[Id],
                [innerEntities].[EntityVersion],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[RelationshipDefinitionEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[RelationshipDefinitionLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY

        -- First result: Paged results
        SELECT 
            [entities].[Id],
            [entities].[UniqueId],
            [entities].[EntityVersion],
            [entity].[Entity] AS [Entity],
            [localization].[Entity] AS [LocalizationEntity]
        FROM 
            #tmpRelationshipDefinitionLists [entities]
        INNER JOIN
            [sitecore_commerce_storage].[RelationshipDefinitionEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
        LEFT OUTER JOIN
            [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC

        DROP TABLE #tmpRelationshipDefinitionLists

        -- Second result: Total count
        SELECT
            COUNT([entities].[Id]) AS [TotalCount]
        FROM (
            SELECT
                [innerEntities].[Id],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[RelationshipDefinitionEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[RelationshipDefinitionLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
    END
    ELSE IF (@TableName = 'RelationshipLists')
    BEGIN
        -- Get matching entities
        SELECT 
            [entities].[UniqueId],
            [entities].[Id],
            [entities].[EntityVersion]
        INTO 
            #tmpRelationshipLists
        FROM (
            SELECT
                [innerEntities].[UniqueId],
                [innerEntities].[Id],
                [innerEntities].[EntityVersion],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[CatalogEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[RelationshipLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY

        -- First result: Paged results
        SELECT 
            [entities].[Id],
            [entities].[UniqueId],
            [entities].[EntityVersion],
            [entity].[Entity] AS [Entity],
            [localization].[Entity] AS [LocalizationEntity]
        FROM 
            #tmpRelationshipLists [entities]
        INNER JOIN
            [sitecore_commerce_storage].[CatalogEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
        LEFT OUTER JOIN
            [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC

        DROP TABLE #tmpRelationshipLists

        -- Second result: Total count
        SELECT
            COUNT([entities].[Id]) AS [TotalCount]
        FROM (
            SELECT
                [innerEntities].[Id],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[CatalogEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[RelationshipLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
    END
    ELSE IF (@TableName = 'WorkflowLists')
    BEGIN
        -- Get matching entities
        SELECT 
            [entities].[UniqueId],
            [entities].[Id],
            [entities].[EntityVersion]
        INTO 
            #tmpWorkflowLists
        FROM (
            SELECT
                [innerEntities].[UniqueId],
                [innerEntities].[Id],
                [innerEntities].[EntityVersion],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[CatalogEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[WorkflowLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId AND [lists].[EntityVersion] = [innerEntities].[EntityVersion] -- NOTE: WorkflowLists needs to compare the entity version
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY

        -- First result: Paged results
        SELECT 
            [entities].[Id],
            [entities].[UniqueId],
            [entities].[EntityVersion],
            [entity].[Entity] AS [Entity],
            [localization].[Entity] AS [LocalizationEntity]
        FROM 
            #tmpWorkflowLists [entities]
        INNER JOIN
            [sitecore_commerce_storage].[CatalogEntity] [entity] ON [entity].[UniqueId] = [entities].[UniqueId]
        LEFT OUTER JOIN
            [sitecore_commerce_storage].[LocalizationEntity] [localization] ON [entities].[UniqueId] = [localization].[EntityUniqueId]
        ORDER BY
            CASE WHEN @SortOrder = 0 THEN [entities].[Id] END ASC,
            CASE WHEN @SortOrder = 1 THEN [entities].[Id] END DESC

        DROP TABLE #tmpWorkflowLists

        -- Second result: Total count
        SELECT
            COUNT([entities].[Id]) AS [TotalCount]
        FROM (
            SELECT
                [innerEntities].[Id],
                ROW_NUMBER() OVER(PARTITION BY [innerEntities].[Id] ORDER BY [innerEntities].[EntityVersion] DESC) rowNumber
            FROM
                [sitecore_commerce_storage].[CatalogEntities] [innerEntities]
            INNER JOIN 
                [sitecore_commerce_storage].[WorkflowLists] [lists] ON [lists].[Id] = [innerEntities].[Id] AND [lists].[ArtifactStoreId] = @ArtifactStoreId AND [lists].[EntityVersion] = [innerEntities].[EntityVersion] -- NOTE: WorkflowLists needs to compare the entity version
            WHERE 
                [lists].[ListName] = @ListName AND [innerEntities].[ArtifactStoreId] = @ArtifactStoreId AND (@IgnorePublished = 1 OR [innerEntities].[Published] = 1)
        ) [entities]
        WHERE rowNumber = 1
    END
END
GO

PRINT N'Update completed.';
GO
