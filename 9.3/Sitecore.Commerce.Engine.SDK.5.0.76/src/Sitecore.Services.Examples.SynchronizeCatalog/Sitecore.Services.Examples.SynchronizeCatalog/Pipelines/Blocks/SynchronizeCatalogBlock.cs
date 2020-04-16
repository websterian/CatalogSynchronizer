// © 2019 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;
using Sitecore.Services.Examples.SynchronizeCatalog.Models;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Arguments;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Blocks
{
    public class SynchronizeCatalogBlock : PipelineBlock<SynchronizeCatalogArgument, SynchronizeCatalogResult, CommercePipelineExecutionContext>
    { 
        private readonly CommerceCommander _commerceCommander;

        public SynchronizeCatalogBlock(CommerceCommander commerceCommander)
        {
            _commerceCommander = commerceCommander;
        }

        public override async Task<SynchronizeCatalogResult> Run(SynchronizeCatalogArgument arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The argument can not be null");

            return await new CatalogSynchronizer(_commerceCommander).Run(arg, context).ConfigureAwait(false);
        }
    }
}
