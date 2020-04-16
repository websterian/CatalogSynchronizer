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
    public static class BuyFridgeAndWarranty
    {
        public static string ScenarioName = "BuyFridgeAndWarranty";

        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    //Fridge - 
                    var fridgeLine =
                        Proxy.DoCommand(container.AddCartLine(cartId, "Habitat_Master|6042567|56042568", 1));

                    //Microwave
                    var microwaveLine =
                        Proxy.DoCommand(container.AddCartLine(cartId, "Habitat_Master|6042757|56042758", 1));

                    //3-year warranty
                    var warrantyLine =
                        Proxy.DoCommand(container.AddCartLine(cartId, "Habitat_Master|7042259|57042259", 1));

                    //HealthTracker
                    var healthTrackerLine =
                        Proxy.DoCommand(container.AddCartLine(cartId, "Habitat_Master|6042886|56042887", 1));

                    Proxy.DoCommand(
                        container.SetCartLineFulfillment(
                            cartId,
                            fridgeLine.Models.OfType<LineAdded>().FirstOrDefault().LineId,
                            context.Components.OfType<PhysicalFulfillmentComponent>().First()));

                    var microwaveLineAdded = microwaveLine.Models.OfType<LineAdded>().FirstOrDefault();

                    Proxy.DoCommand(
                        container.SetCartLineFulfillment(
                            cartId,
                            microwaveLineAdded.LineId,
                            context.Components.OfType<PhysicalFulfillmentComponent>().First()));

                    Proxy.DoCommand(
                        container.SetCartLineFulfillment(
                            cartId,
                            warrantyLine.Models.OfType<LineAdded>().FirstOrDefault().LineId,
                            context.Components.OfType<ElectronicFulfillmentComponent>().First()));

                    Proxy.DoCommand(
                        container.SetCartLineFulfillment(
                            cartId,
                            healthTrackerLine.Models.OfType<LineAdded>().FirstOrDefault().LineId,
                            context.Components.OfType<PhysicalFulfillmentComponent>().First()));

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
