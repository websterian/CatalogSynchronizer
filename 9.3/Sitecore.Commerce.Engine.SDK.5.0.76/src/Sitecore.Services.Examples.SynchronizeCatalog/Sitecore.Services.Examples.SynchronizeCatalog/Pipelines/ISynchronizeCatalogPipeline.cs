// © 2019 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using Sitecore.Commerce.Core;
using Sitecore.Framework.Pipelines;
using Sitecore.Services.Examples.SynchronizeCatalog.Models;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Arguments;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Pipelines
{
    public interface ISynchronizeCatalogPipeline : IPipeline<SynchronizeCatalogArgument, SynchronizeCatalogResult, CommercePipelineExecutionContext>
    {
    }
}
