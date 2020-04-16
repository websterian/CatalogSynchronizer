DECLARE @userName varchar(MAX) = REPLACE('PlaceholderForSharedDatabaseUserName','\\','\');
DECLARE @dbName varchar(MAX) = 'PlaceholderForSharedDatabaseName';

DECLARE @sql nvarchar(MAX) = 'IF NOT EXISTS (SELECT * FROM master.dbo.syslogins WHERE name = ''UserNameParameter'') 
BEGIN 
    CREATE LOGIN [UserNameParameter] FROM WINDOWS WITH DEFAULT_DATABASE=[DbNameParameter], DEFAULT_LANGUAGE=[us_english] 
END

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = ''UserNameParameter'') 
BEGIN 
    USE [DbNameParameter] 
	CREATE USER [UserNameParameter] FOR LOGIN [UserNameParameter] 
END

USE [DbNameParameter] EXEC sp_addrolemember ''db_owner'', ''UserNameParameter'''

SET @sql = REPLACE(@sql,'UserNameParameter', @userName)
SET @sql = REPLACE(@sql,'DbNameParameter', @dbName)

EXECUTE sp_executesql @sql 