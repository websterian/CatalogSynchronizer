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
    public static class SimplePhysical2Items
    {
        public static string ScenarioName = "SimplePhysical2Items";

        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW188 06|19", 1));

                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW048 01|32", 1));

                    // Add Cart Level Physical Fulfillment
                    var commandResponse = Proxy.DoCommand(
                        container.SetCartFulfillment(
                            cartId,
                            context.Components.OfType<PhysicalFulfillmentComponent>().First()));
                    var totals = commandResponse.Models.OfType<Totals>().First();
                    totals.AdjustmentsTotal.Amount.Should().Be(14.5M);
                    totals.GrandTotal.Amount.Should().Be(159.5M);

                    var paymentComponent = context.Components.OfType<FederatedPaymentComponent>().First();
                    paymentComponent.Amount = Money.CreateMoney(totals.GrandTotal.Amount - totals.PaymentsTotal.Amount);

                    // Add a Payment
                    commandResponse = Proxy.DoCommand(container.AddFederatedPayment(cartId, paymentComponent));
                    totals = commandResponse.Models.OfType<Totals>().First();
                    totals.PaymentsTotal.Amount.Should().Be(159.5M);

                    // Get the cart one last time before creating the order
                    var cart = Carts.GetCart(cartId, context);
                    cart.Version.Should().Be(4);

                    var order = Orders.CreateAndValidateOrder(container, cartId, context);
                    order.Status.Should().NotBe("Problem");
                    order.Totals.GrandTotal.Amount.Should().Be(159.5M);

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
