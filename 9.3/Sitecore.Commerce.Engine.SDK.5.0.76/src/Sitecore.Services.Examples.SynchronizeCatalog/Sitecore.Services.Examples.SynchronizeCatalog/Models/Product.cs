using Sitecore.Commerce.Core;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Models
{
    public class Product : SynchronizeEntityBase
    {
        public string Name { get; set; }

        public string Brand { get; set; }

        public string Manufacturer { get; set; }

        public string TypeOfGood { get; set; }

        public decimal ListPrice { get; set; }
        public string ListPriceCurrency { get; set; }

        public string Tags { get; set; }

        public string FullId => CommerceEntity.IdPrefix<Commerce.Plugin.Catalog.SellableItem>() + Id;

        public string FullParentId
        {
            get
            {
                var parentId = ParentType.ToLower() == "catalog"
                    ? CommerceEntity.IdPrefix<Commerce.Plugin.Catalog.Catalog>() + SplitCatalogId
                    : CommerceEntity.IdPrefix<Commerce.Plugin.Catalog.Category>() + SplitCatalogId + "-" + SplitParentId;

                return parentId;
            }
        }
    }
}
