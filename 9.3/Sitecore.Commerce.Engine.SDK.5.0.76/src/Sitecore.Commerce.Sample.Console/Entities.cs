using System;
using FluentAssertions;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Entities
    {
        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Entities"))
            {
                // GetRawEntity(); // needs QA role in admin user 
                GetEntityView();
            }
        }

        private static void GetRawEntity()
        {
            using (new SampleMethodScope())
            {
                var devOps = new DevOpAndre();
                var container = devOps.Context.OpsContainer();
                Proxy.GetValue(container.GetRawEntity("invalidEntityId", "uid", EnvironmentConstants.HabitatShops));

                var uniqueId = Proxy.GetValue(
                    container.GetDeterministicEntityUniqueId("Entity-SellableItem-AW007 08", 1));
                uniqueId.Should().NotBe(Guid.Empty);
                var result = Proxy.GetValue(
                    container.GetRawEntity("Entity-SellableItem-AW007 08", uniqueId.ToString(), EnvironmentConstants.AdventureWorksShops));
                result.Should().NotBeNull();

                Proxy.GetValue(
                    container.GetRawEntity(
                        $"Environments/{devOps.Context.Environment}",
                        string.Empty,
                        EnvironmentConstants.HabitatShops));
            }
        }

        private static void GetEntityView()
        {
            using (new SampleMethodScope())
            {
                var csrSheila = new CsrSheila();
                var container = csrSheila.Context.ShopsContainer();

                Proxy.GetValue(container.GetEntityView("fakeentityid", "Master", string.Empty, string.Empty));

                Proxy.GetValue(container.GetEntityView(null, "Master", string.Empty, string.Empty));
            }
        }
    }
}
