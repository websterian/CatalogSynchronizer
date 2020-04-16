using Sitecore.Commerce.Core;
using Sitecore.Framework.Pipelines;
using Sitecore.Services.Examples.SynchronizeCatalog.Models;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Arguments;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Pipelines
{
    public interface ISynchronizeCatalogMinionPipeline : IPipeline<SynchronizeCatalogArgument, SynchronizeCatalogResult, CommercePipelineExecutionContext>
    {
    }
}
