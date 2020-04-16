using Sitecore.Commerce.Core;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Models
{
    public class Category : SynchronizeEntityBase
    {
        public string Name { get; set; }

        public string IdWithCatalog()
        {
            return SplitCatalogId + "-" + Id;
        }

        public string FullIdWithCatalog => CommerceEntity.IdPrefix<Commerce.Plugin.Catalog.Category>() + IdWithCatalog();

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
