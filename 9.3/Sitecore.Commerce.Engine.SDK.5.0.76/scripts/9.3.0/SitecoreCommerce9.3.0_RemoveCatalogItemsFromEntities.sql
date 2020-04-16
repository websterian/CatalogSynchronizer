/******************************************************************************
* This script should run against 'SitecoreCommerce9_SharedEnvironments'
* Removes the 'CatalogItems' from list membership component if present.
*
* This script is optional and is not required by all customers.  Only run this
* script if your implementation has a dependency on the names of the list 
* membership component of catalog entities.  Keep in mind, the larger the 
* catalog, the longer it will take.  This could require a few hours for 
* large catalogs as the script traverses every single catalog entity, 
* sequentially.
******************************************************************************/

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET NOCOUNT ON
GO

declare @count int = 1;
declare @entityJson as nvarchar(max)
declare @uniqueId as uniqueidentifier

DECLARE entity_cursor CURSOR FOR   
SELECT UniqueId, Entity
FROM [sitecore_commerce_storage].[CatalogEntity]

open entity_cursor

fetch next from entity_cursor into @uniqueId, @entityJson
WHILE @@FETCH_STATUS = 0  
BEGIN  

	if (@count % 100 = 0)
		print @count

	-- Get the list member ship component
	declare @listMembershipComponent nvarchar(max)

	SELECT @listMembershipComponent = value 
	FROM OPENJSON(@entityJson, '$.Components."$values"') 
	where JSON_VALUE([value], '$."$type"') LIKE '%Sitecore.Commerce.Plugin.ManagedLists.ListMembershipsComponent, Sitecore.Commerce.Plugin.ManagedLists%'

	-- Find the index of the membership list component
	DECLARE @index integer;
	declare @json nvarchar(max);

	set @json = (SELECT JSON_QUERY(@entityJson,'$.Components."$values"'))

	SELECT Top 1 @index=[Index] FROM (
		SELECT [type], ROW_NUMBER() OVER(ORDER BY (SELECT 100)) AS [Index]
		FROM OPENJSON(@json)
			WITH (
				[type] NVARCHAR(MAX) '$."$type"'
			)) component_types
		WHERE component_types.[type] like '%ListMembershipsComponent%'

	SET @index = @index -1

	-- Check if there is a list membership component and that we have returned JSON
	declare @catalogItemEntry nvarchar(100) =
	(
		select value
		from OPENJSON(@listMembershipComponent, '$.Memberships."$values"')
		where value = N'CatalogItems'
	)


	if (LEN(@catalogItemEntry) > 0) 
	BEGIN
		-- Remove the CatalogItems entry from the list member ship component
		DECLARE @ListNames TABLE
		(
		  Name nvarchar(100) 
		)

		insert into @ListNames 
			select value
			from OPENJSON(@listMembershipComponent, '$.Memberships."$values"')
			where value <> N'CatalogItems'

		declare @newListNames nvarchar(max)

		set @newListNames =
		STUFF
		(
			(
				SELECT CONCAT(',"', Name, '"')
				FROM @ListNames
				FOR XML PATH(''), TYPE
			).value('.', 'varchar(max)')
			,1
			,1
			,'['
		) + ']';

		delete from @ListNames;

		declare @componentOutputEmpty as nvarchar(max)
		SELECT @componentOutputEmpty = JSON_MODIFY(@listMembershipComponent, '$.Memberships."$values"', JSON_QUERY('[]'))

		DECLARE @componentOutput nvarchar(max)
		SELECT @componentOutput = JSON_MODIFY(
		   @componentOutputEmpty,
		   '$.Memberships."$values"',
		JSON_QUERY(@newListNames) 
		)

		SELECT @entityJson = JSON_MODIFY(
		@entityJson,
		'$.Components."$values"[' + TRIM(STR(@index)) +']',
		JSON_QUERY(@componentOutput, '$') 
		)

		update [sitecore_commerce_storage].[CatalogEntity]
		set Entity = @entityJson
		where UniqueId = @uniqueId
	END
	set @count = @count + 1
fetch next from entity_cursor into @uniqueId, @entityJson
END

print @count

CLOSE entity_cursor;
DEALLOCATE entity_cursor;
GO