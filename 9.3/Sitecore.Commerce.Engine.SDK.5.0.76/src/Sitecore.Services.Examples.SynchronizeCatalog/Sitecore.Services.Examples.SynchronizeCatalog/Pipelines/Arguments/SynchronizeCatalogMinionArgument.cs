using Sitecore.Commerce.Core;
using Sitecore.Framework.Conditions;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Arguments
{
    public class SynchronizeCatalogMinionArgument : PipelineArgument
    {
        public string OrderId { get; set; }

        public SynchronizeCatalogMinionArgument(string orderId)
        {
            Condition.Requires<string>(orderId, "orderId").IsNotNullOrEmpty();

            OrderId = orderId;
        }
    }
}
