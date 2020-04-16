using System.Threading.Tasks;
using Microsoft.AspNetCore.OData.Builder;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;

namespace Sitecore.Services.Examples.SynchronizeCatalog
{
    [PipelineDisplayName("ConfigureServiceApiBlock")]
    public class ConfigureServiceApiBlock : PipelineBlock<ODataConventionModelBuilder, ODataConventionModelBuilder, CommercePipelineExecutionContext>
    {
        public override Task<ODataConventionModelBuilder> Run(ODataConventionModelBuilder modelBuilder, CommercePipelineExecutionContext context)
        {
            Condition.Requires(modelBuilder).IsNotNull($"{Name}: The argument cannot be null.");

            var syncCatalog = modelBuilder.Action("SynchronizeCatalog");
            syncCatalog.Parameter<string>("SynchronizeCatalog");
            syncCatalog.Returns<string>();

            return Task.FromResult(modelBuilder);
        }
    }
}
