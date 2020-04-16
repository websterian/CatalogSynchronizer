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
    public static class AddRemoveCartLine
    {
        public static string ScenarioName = "AddRemoveCartLine";

        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW098 04|5", 1));

                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));

                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));

                    var updatedCart = Proxy.GetValue(container.Carts.ByKey(cartId).Expand("Lines"));

                    var cartLineComponent =
                        updatedCart.Lines.FirstOrDefault(l => l.ItemId.Equals("Adventure Works Catalog|AW475 14|"));
                    if (cartLineComponent != null)
                    {
                        Proxy.DoCommand(container.RemoveCartLine(cartId, cartLineComponent.Id));
                    }

                    var commandResponse = Proxy.DoCommand(
                        container.SetCartFulfillment(
                            cartId,
                            context.Components.OfType<PhysicalFulfillmentComponent>().First()));

                    var totals = commandResponse.Models.OfType<Totals>().First();

                    var paymentComponent = context.Components.OfType<FederatedPaymentComponent>().First();
                    paymentComponent.Amount = Money.CreateMoney(totals.GrandTotal.Amount);

                    commandResponse = Proxy.DoCommand(
                        container.AddFederatedPayment(
                            cartId,
                            paymentComponent));

                    totals = commandResponse.Models.OfType<Totals>().First();

                    totals.PaymentsTotal.Amount.Should().Be(totals.GrandTotal.Amount);

                    var order = Orders.CreateAndValidateOrder(container, cartId, context);

                    order.Totals.GrandTotal.Amount.Should().Be(115.50M);

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
