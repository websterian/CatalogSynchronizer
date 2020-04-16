using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Commerce.Plugin.Payments;
using Sitecore.Commerce.Sample.Console;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Scenarios
{
    public static class BuyAvailabilitySplitItem
    {
        public static string ScenarioName = "BuyAvailabilitySplitItem";

        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    // making sure the pre-orderable item have a pre-order date in the future
                    EngineExtensions.EditInventoryInformation(
                        "Adventure Works Inventory",
                        "AW074 04|9".ToEntityId<SellableItem>(),
                        new List<ViewProperty>
                        {
                            new ViewProperty
                            {
                                Name = "Quantity",
                                Value = "0",
                                OriginalType = typeof(int).FullName
                            },
                            new ViewProperty
                            {
                                Name = "Preorderable",
                                Value = "true",
                                OriginalType = typeof(bool).FullName
                            },
                            new ViewProperty
                            {
                                Name = "PreorderAvailabilityDate",
                                Value = DateTimeOffset.UtcNow.AddMonths(1).ToString(),
                                OriginalType = typeof(DateTimeOffset).FullName
                            },
                            new ViewProperty
                            {
                                Name = "PreorderLimit",
                                Value = 10.ToString(),
                                OriginalType = typeof(int).FullName
                            }
                        });

                    //Add Cart Line without Variant
                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));

                    //Add a back-ordered item
                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW210 12|4", 1));

                    //Add a pre-ordered item
                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW074 04|9", 1));

                    var commandResult = Proxy.DoCommand(
                        container.SetCartFulfillment(
                            cartId,
                            context.Components.OfType<PhysicalFulfillmentComponent>().First()));

                    var totals = commandResult.Models.OfType<Totals>().First();

                    var paymentComponent = context.Components.OfType<FederatedPaymentComponent>().First();
                    paymentComponent.Amount = Money.CreateMoney(totals.GrandTotal.Amount);

                    commandResult = Proxy.DoCommand(container.AddFederatedPayment(cartId, paymentComponent));
                    totals = commandResult.Models.OfType<Totals>().First();

                    totals.PaymentsTotal.Amount.Should().Be(totals.GrandTotal.Amount);

                    var order = Orders.CreateAndValidateOrder(container, cartId, context);

                    order.Totals.GrandTotal.Amount.Should().Be(433.40M);

                    return order.Id;
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
