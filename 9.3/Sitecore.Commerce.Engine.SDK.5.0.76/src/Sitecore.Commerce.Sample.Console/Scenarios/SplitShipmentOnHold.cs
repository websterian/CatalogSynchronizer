using System;
using FluentAssertions;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Sample.Console;

namespace Sitecore.Commerce.Sample.Scenarios
{
    public static class SplitShipmentOnHold
    {
        public static string ScenarioName = "SplitShipmentOnHold";

        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var orderId = BuySplitShipment.Run(context);
                    if (!string.IsNullOrEmpty(orderId))
                    {
                        OrdersUX.HoldOrder(orderId);
                    }

                    var journalizedOrdersMetadata =
                        container.GetListMetadata($"JournalEntries-ByEntity-{orderId}").GetValue();
                    System.Console.WriteLine(
                        $"List:{journalizedOrdersMetadata.ListName} Count:{journalizedOrdersMetadata.Count}");

                    var journalizedOrdersList = container.GetList(
                            $"JournalEntries-ByEntity-{orderId}",
                            "Sitecore.Commerce.Plugin.Journaling.JournalEntry, Sitecore.Commerce.Plugin.Journaling",
                            0,
                            10)
                        .Expand("Items")
                        .GetValue();

                    journalizedOrdersList.TotalItemCount.Should().Be(2);

                    return orderId;
                }
                catch (Exception ex)
                {
                    ConsoleExtensions.WriteColoredLine(
                        ConsoleColor.Red,
                        $"Exception in Scenario {ScenarioName} (${ex.Message}) : Stack={ex.StackTrace}");
                    return null;
                }
            }
        }
    }
}
