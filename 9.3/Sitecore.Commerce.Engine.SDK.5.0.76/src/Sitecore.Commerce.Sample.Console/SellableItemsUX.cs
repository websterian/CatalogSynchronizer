using System;
using System.Reflection;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Catalog;

namespace Sitecore.Commerce.Sample.Console
{
    public static class SellableItemsUX
    {
        internal const string CatalogName = "Adventure Works Catalog";
        internal const string ImageId = "f6ba4fec-07e7-4c69-858e-6acd5eba4c1b";
        internal static string _categoryName;
        internal static string _product1Name;
        internal static string _product2Name;
        internal static string _variant1Name;
        internal static readonly string CatalogId = CatalogName.ToEntityId<Catalog>();


        internal static string _categoryId;
        internal static string _product1Id;
        internal static string _product2Id;

        public static void RunScenarios()
        {
            using (new SampleScenarioScope(MethodBase.GetCurrentMethod().DeclaringType.Name))
            {
                var partial = $"{Guid.NewGuid():N}".Substring(0, 3);

                _categoryName = $"SellableItemUXCategory{partial}";
                _product1Name = $"ConsoleProduct1{partial}";
                _product2Name = $"ConsoleProduct2{partial}";
                _variant1Name = $"ConsoleVariant1{partial}";

                _categoryId = _categoryName.ToEntityId<Category>(CatalogName);
                _product1Id = _product1Name.ToEntityId<SellableItem>();
                _product2Id = _product2Name.ToEntityId<SellableItem>();

                EngineExtensions.AddCategory(_categoryId, CatalogId, CatalogName);
                AddSellableItemToCatalog();
                AddSellableItemToCategory();
                AddSellableItemVariant();
                DisableSellableItemVariant();
                EnableSellableItemVariant();
                DeleteSellableItemVariant();
                AssociateSellableItemToCatalog();
                AssociateSellableItemToCategory();
                DissassociateSellableItemFromCatalog();
                DissassociateSellableItemFromCategory();
                AddSellableItemImage();
                RemoveSellableItemImage();
                EngineExtensions.DeleteSellableItem(_product1Id, _categoryId, _categoryName, CatalogName);
                EngineExtensions.DeleteSellableItem(_product2Id, _categoryId, _categoryName, CatalogName);
                EngineExtensions.DeleteCategory(_categoryId);
            }
        }

        private static void RemoveSellableItemImage()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.RemoveSellableItemImage(_product1Id, CatalogName, ImageId);
            }
        }

        private static void AddSellableItemImage()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.AddSellableItemImage(_product1Id, CatalogName, ImageId);
            }
        }

        private static void DissassociateSellableItemFromCategory()
        {
            using (new SampleMethodScope())
            {
                // Remove Product1 from the category
                EngineExtensions.AssertChildViewItemExists(_categoryId, ChildViewNames.SellableItems, _product1Id);
                EngineExtensions.DisassociateItem(_product1Id, _product1Name, _categoryId, _categoryName);
            }
        }

        private static void DissassociateSellableItemFromCatalog()
        {
            using (new SampleMethodScope())
            {
                // Remove Product2 from the catalog root
                EngineExtensions.AssertChildViewItemExists(CatalogId, ChildViewNames.SellableItems, _product2Id);
                EngineExtensions.DisassociateItem(_product2Id, _product2Name, CatalogId, CatalogName);
            }
        }

        private static void AssociateSellableItemToCategory()
        {
            using (new SampleMethodScope())
            {
                // Product1 should be a child of the catalog, but not the category.
                EngineExtensions.AssertChildViewItemExists(CatalogId, ChildViewNames.SellableItems, _product1Id);
                EngineExtensions.AssertChildViewItemNotExists(_categoryId, ChildViewNames.SellableItems, _product1Id);

                // Add Product1 to the category
                EngineExtensions.AssociateSellableItem(_product1Id, _categoryId, _categoryName, CatalogName);
            }
        }

        private static void AssociateSellableItemToCatalog()
        {
            using (new SampleMethodScope())
            {
                // Product2 should be a child of the category, but not the catalog.
                EngineExtensions.AssertChildViewItemExists(_categoryId, ChildViewNames.SellableItems, _product2Id);
                EngineExtensions.AssertChildViewItemNotExists(CatalogId, ChildViewNames.SellableItems, _product2Id);

                // Add Product2 to the catalog root
                EngineExtensions.AssociateSellableItem(_product2Id, CatalogId, CatalogName, CatalogName);
            }
        }

        private static void DeleteSellableItemVariant()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.DeleteSellableItemVariant(_variant1Name, _product1Name, CatalogName, CatalogName);
            }
        }

        private static void DisableSellableItemVariant()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.DisableSellableItemVariant(_variant1Name, _product1Name, CatalogName, CatalogName);
            }
        }

        private static void EnableSellableItemVariant()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.EnableSellableItemVariant(_variant1Name, _product1Name, CatalogName, CatalogName);
            }
        }

        private static void AddSellableItemVariant()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.AddSellableItemVariant(_variant1Name, _product1Name, CatalogName, CatalogName);
            }
        }

        private static void AddSellableItemToCatalog()
        {
            using (new SampleMethodScope())
            {
                // Add Product1 to the catalog root.
                EngineExtensions.AssertCatalogExists(CatalogId);
                EngineExtensions.AddSellableItem(_product1Id, CatalogId, CatalogName, CatalogName);
            }
        }

        private static void AddSellableItemToCategory()
        {
            using (new SampleMethodScope())
            {
                // Add Product2 to the category.
                EngineExtensions.AssertCategoryExists(_categoryId);
                EngineExtensions.AddSellableItem(_product2Id, _categoryId, _categoryName, CatalogName);
            }
        }
    }
}
