/******************************************************************************
* This script should run against 'SitecoreCommerce9_SharedEnvironments'
* database to populate the [sitecore_commerce_storage].[Mappings] table after
* upgrading the schema from Sitecore XC 9.1.* to 9.2.0.
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

	EXEC [sitecore_commerce_storage].[CatalogInsertMappings]
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
