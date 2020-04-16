using System.Linq;
using Sitecore.Commerce.Core;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Models
{
    public abstract class SynchronizeEntityBase
    {
        public string EntityType { get; set; }
        public string ParentId { get; set; }
        public string SplitCatalogId => ParentId.Split('|').First();

        public string SplitParentId => ParentId.Split('|').Last();

        public string FullCatalogId => CommerceEntity.IdPrefix<Commerce.Plugin.Catalog.Catalog>() + SplitCatalogId;
      
        public string ParentType { get; set; }        
        public string Id { get; set; }
       
        public string Description { get; set; }
        public string DisplayName { get; set; }
        public string Operation { get; set; }
    }
}
