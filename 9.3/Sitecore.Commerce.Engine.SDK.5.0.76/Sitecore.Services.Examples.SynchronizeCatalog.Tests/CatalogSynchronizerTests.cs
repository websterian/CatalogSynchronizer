using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Framework.Pipelines;
using Sitecore.Services.Examples.SynchronizeCatalog.Framework;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Tests
{
    [TestClass]
    public class CatalogSynchronizerTests
    {
        private List<string> _sellableItemIdsToBeFound = new List<string>();
        private List<string> categoryIdsToBeFound = new List<string>();
        private List<string> catalogIdsToBeFound = new List<string>();

        [TestMethod]
        public void Update_Existing_SellableItem()
        {
            var mockCommerceCommander = new Mock<CommerceCommander>();
            var mockContext = new Mock<CommercePipelineExecutionContext>();

            _sellableItemIdsToBeFound.AddRange(new List<string>(){"Entity-SellableItem-EXISTING_PRODUCT0001", "Entity-SellableItem-EXISTING_PRODUCT0002" });

            mockCommerceCommander.Setup(s => s.Pipeline<IFindEntitiesPipeline>().Run(It.IsAny<FindEntitiesArgument>(),
                It.IsAny<CommercePipelineExecutionContext>())).Returns((FindEntitiesArgument arg, CommercePipelineExecutionContext context) => MockFoundEntities(arg, context));

            var catalogSynchronizer = new CatalogSynchronizer(mockCommerceCommander.Object, mockContext.Object);
        }

        public Task<List<CommerceEntity>> MockFoundEntities(FindEntitiesArgument arg, CommercePipelineExecutionContext context)
        {
            var returnList = new List<CommerceEntity>();
            if (arg.EntityType == typeof(SellableItem))
            {
                returnList.AddRange(_sellableItemIdsToBeFound.Select(sellableItemId => new SellableItem() {Id = sellableItemId}));
            }
            else if (arg.EntityType == typeof(Category))
            {

            }
            else if (arg.EntityType == typeof(Catalog))
            {

            }
            return Task.FromResult(returnList);
        }
    }
}
