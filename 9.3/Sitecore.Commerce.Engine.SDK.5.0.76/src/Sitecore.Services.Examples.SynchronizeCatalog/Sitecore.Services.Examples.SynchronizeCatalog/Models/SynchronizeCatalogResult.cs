using System.Collections.Generic;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Models
{
    public class SynchronizeCatalogResult
    {
        public SynchronizeCatalogResult()
        {
            LogMessages = new List<string>();
        }

        public int NumberOfProductsUpdated { get; set; }
        public int NumberOfProductsCreated { get; set; }
        public int NumberOfProductsDeleted { get; set; }

        public int NumberOfCatalogsUpdated { get; set; }
        public int NumberOfCatalogsCreated { get; set; }
        public int NumberOfCatalogsMarkedForPurging { get; set; }

        public int NumberOfCategoriesUpdated { get; set; }
        public int NumberOfCategoriesCreated { get; set; }
        public int NumberOfCategoriesMarkedforPurging { get; set; }

        public int TotalNumberOfEntitiesEffected =>
            this.NumberOfCatalogsCreated + this.NumberOfCategoriesCreated +
            this.NumberOfProductsCreated
            + this.NumberOfCatalogsUpdated + this.NumberOfCategoriesUpdated +
            this.NumberOfProductsUpdated
            + this.NumberOfCatalogsMarkedForPurging + this.NumberOfCategoriesMarkedforPurging +
            this.NumberOfProductsDeleted;

        public List<string> LogMessages { get; set; }
    }
}
