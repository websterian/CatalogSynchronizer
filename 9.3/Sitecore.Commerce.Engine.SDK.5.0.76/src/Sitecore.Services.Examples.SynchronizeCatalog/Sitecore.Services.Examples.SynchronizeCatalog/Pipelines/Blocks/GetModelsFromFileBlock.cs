using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Framework.Pipelines;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Arguments;
using Sitecore.Services.Examples.SynchronizeCatalog.Policies;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Blocks
{
    [PipelineDisplayName("Orders.block.GetModelsFromFileBlock")]
    public class GetModelsFromFileBlock : PipelineBlock<SynchronizeCatalogArgument, SynchronizeCatalogArgument, CommercePipelineExecutionContext>
    {
        private readonly IFindEntityPipeline _findEntityPipeline;

        public GetModelsFromFileBlock(IFindEntityPipeline findEntityPipeline)
        {
            _findEntityPipeline = findEntityPipeline;
        }

        public override async Task<SynchronizeCatalogArgument> Run(SynchronizeCatalogArgument arg, CommercePipelineExecutionContext context)
        {
            var policy = context.GetPolicy<SynchronizeCatalogPolicy>();

            //TODO : Look in folders in policy and read the CSV

            var order = await _findEntityPipeline.Run(new FindEntityArgument(typeof(Order), "DummyForNow"), context) as Order;
           
            return new SynchronizeCatalogArgument();
        }
    }
}
