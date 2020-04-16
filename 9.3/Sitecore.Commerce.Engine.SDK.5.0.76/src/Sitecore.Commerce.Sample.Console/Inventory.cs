using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Inventory
    {
        private static readonly Container AwShopsContainer = new AnonymousCustomerJeff().Context.ShopsContainer();
        private static readonly Container HabitatShopsContainer = new AnonymousCustomerSteve().Context.ShopsContainer();

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Inventory"))
            {
                VerifyInventorySet(AwShopsContainer, "Adventure Works Inventory");
                VerifyInventorySet(HabitatShopsContainer, "Habitat_Inventory");
            }
        }

        private static void VerifyInventorySet(Container container, string expectedName)
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.Execute(container.InventorySets).ToList();

                result.Should().NotBeNull();
                result.Count.Should().BeGreaterOrEqualTo(1);
                result.Any(x => x.Name.Equals(expectedName)).Should().BeTrue();
            }
        }
    }
}
