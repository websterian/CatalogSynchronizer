using System;
using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Commerce.Plugin.Payments;
using Sitecore.Commerce.Sample.Console;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Scenarios
{
    public static class BuyGameSystemAndSubscription
    {
        public static string ScenarioName = "BuyGameSystemAndSubscription";

        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    //Habitat NextCube-V Game Cube 1TB 
                    var gameSystemLine =
                        Proxy.DoCommand(container.AddCartLine(cartId, "Habitat_Master|6042432|56042432", 1));

                    //Habitat NextCube Now  6 month On-Demand multi game Subscription
                    var subscriptionLine =
                        Proxy.DoCommand(container.AddCartLine(cartId, "Habitat_Master|6042456|56042456", 1));

                    Proxy.DoCommand(
                        container.SetCartLineFulfillment(
                            cartId,
                            gameSystemLine.Models.OfType<LineAdded>().FirstOrDefault().LineId,
                            context.Components.OfType<PhysicalFulfillmentComponent>().First()));

                    Proxy.DoCommand(
                        container.SetCartLineFulfillment(
                            cartId,
                            subscriptionLine.Models.OfType<LineAdded>().FirstOrDefault().LineId,
                            context.Components.OfType<ElectronicFulfillmentComponent>().First()));

                    var cart = Carts.GetCart(cartId, context);
                    cart.Should().NotBeNull();

                    var paymentComponent = context.Components.OfType<FederatedPaymentComponent>().First();
                    paymentComponent.Amount = cart.Totals.GrandTotal;
                    Proxy.DoCommand(container.AddFederatedPayment(cartId, paymentComponent));

                    var order = Orders.CreateAndValidateOrder(container, cartId, context);
                    order.Status.Should().NotBe("Problem");
                    order.Totals.GrandTotal.Amount.Should().Be(cart.Totals.GrandTotal.Amount);

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
