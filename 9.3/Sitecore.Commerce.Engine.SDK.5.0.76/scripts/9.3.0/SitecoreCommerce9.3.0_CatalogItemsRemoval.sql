/******************************************************************************
* This script should run against 'SitecoreCommerce9_SharedEnvironments' and
* 'SitecoreCommerce9_Global' to upgrade from Sitecore XC 9.2 to 9.3
******************************************************************************/

/* Remove all catalog items entries from the CatalogLists table */
delete from sitecore_commerce_storage.CatalogLists where listname like '%-catalogitems-%'
