// © 2016 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Commerce.Core;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Arguments;
using Sitecore.Services.Examples.SynchronizeCatalog.Policies;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Minions
{
    public class SynchronizeCatalogMinion : Minion
    {
        protected ISynchronizeCatalogMinionPipeline Pipeline;

        public override void Initialize(IServiceProvider serviceProvider, MinionPolicy policy, CommerceContext commerceContext)
        {
            base.Initialize(serviceProvider, policy, commerceContext);
            Pipeline = serviceProvider.GetService<ISynchronizeCatalogMinionPipeline>();
        }

        protected override async Task<MinionRunResultsModel> Execute()
        {
            var arg = new SynchronizeCatalogArgument();

            var policy = MinionContext.GetPolicy<SynchronizeCatalogPolicy>();

            arg.Options.ExcludeLogInResults = policy.ExcludeLogInResults;
            arg.Options.SkipRelationships = policy.SkipRelationships;

            var result = await Pipeline.Run(arg, new CommercePipelineExecutionContextOptions(MinionContext)).ConfigureAwait(false);

            var runResults = new MinionRunResultsModel { ItemsProcessed = result.TotalNumberOfEntitiesEffected, DidRun = true };

            return runResults;
        }
    }
}
