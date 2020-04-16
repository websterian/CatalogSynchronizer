using Sitecore.Commerce.Core;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Models
{
    public class Catalog : SynchronizeEntityBase
    {
        public string Name { get; set; }
        public string FullId => CommerceEntity.IdPrefix<Commerce.Plugin.Catalog.Catalog>() + Id;
    }
}
