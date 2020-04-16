using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Pipelines;
using Sitecore.Services.Examples.SynchronizeCatalog.Models;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Arguments;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Pipelines 
{
    public class SynchronizeCatalogMinionPipeline : CommercePipeline<SynchronizeCatalogArgument, SynchronizeCatalogResult>, ISynchronizeCatalogMinionPipeline
    {
        public SynchronizeCatalogMinionPipeline(IPipelineConfiguration<ISynchronizeCatalogMinionPipeline> configuration, ILoggerFactory loggerFactory)
            : base(configuration, loggerFactory)
        {
        }

    }
}
