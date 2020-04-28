using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.ManagedLists;
using Sitecore.Services.Examples.SynchronizeCatalog.Models;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Arguments;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Framework
{
    public class CatalogSynchronizer : CatalogSynchronizerBase
    {
        public CatalogSynchronizer(CommerceCommander commerceCommander, CommercePipelineExecutionContext context) : base(commerceCommander, context)
        {
           
        }

        public override void SetProductCustomFields(Product sourceProduct, SellableItem destinationProduct)
        {
           //Add your custom mappings here e.g. map to new components etc.
        }

        public override void SetVariantCustomFields(Variant sourceVariant, ItemVariationComponent destinationVariant)
        {
            //Add your custom mappings here e.g. map to new components etc.
        }
    }
}
