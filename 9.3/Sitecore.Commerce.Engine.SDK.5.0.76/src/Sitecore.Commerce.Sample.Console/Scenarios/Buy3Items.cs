using System;
using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Commerce.Plugin.Payments;
using Sitecore.Commerce.Sample.Console;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Scenarios
{
    public static class Buy3Items
    {
        public static string ScenarioName = "Buy3Items";

        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW029 03|54", 1));
                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW029 03|64", 2));
                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW032 01|37", 3));

                    // Add Cart Level Physical Fulfillment
                    var commandResponse = Proxy.DoCommand(
                        container.SetCartFulfillment(
                            cartId,
                            context.Components.OfType<PhysicalFulfillmentComponent>().First()));
                    var totals = commandResponse.Models.OfType<Totals>().First();

                    totals.AdjustmentsTotal.Amount.Should().Be(13.50M);
                    totals.GrandTotal.Amount.Should().Be(148.50M);

                    // Add a Payment
                    var paymentComponent = context.Components.OfType<FederatedPaymentComponent>().First();
                    paymentComponent.Amount = Money.CreateMoney(totals.GrandTotal.Amount - totals.PaymentsTotal.Amount);
                    commandResponse = Proxy.DoCommand(container.AddFederatedPayment(cartId, paymentComponent));
                    totals = commandResponse.Models.OfType<Totals>().First();
                    totals.PaymentsTotal.Amount.Should().Be(148.5M);

                    var order = Orders.CreateAndValidateOrder(container, cartId, context);
                    order.Status.Should().NotBe("Problem");
                    order.Totals.GrandTotal.Amount.Should().Be(148.50M);

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
