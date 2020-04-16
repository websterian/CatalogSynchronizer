using Sitecore.Commerce.Core;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Models
{
    public class Variant : Product
    {
        public string Style { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }

        public string FullVariantParentId => CommerceEntity.IdPrefix<Commerce.Plugin.Catalog.SellableItem>() + SplitParentId;
    }
}
