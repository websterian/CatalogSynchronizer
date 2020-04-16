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
    public static class BuyCameraAndGiftWrap
    {
        public static string ScenarioName = "BuyCameraAndGiftWrap";

        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    //Optix 18.0MP DSLR Camera with 18-55mm Lens 
                    var phoneLine =
                        Proxy.DoCommand(container.AddCartLine(cartId, "Habitat_Master|7042074|57042074", 1));

                    //Habitat Gift Wrapping (Style1)
                    var gwLine = Proxy.DoCommand(container.AddCartLine(cartId, "Habitat_Master|6042989|56042989", 1));

                    Proxy.DoCommand(
                        container.SetCartLineFulfillment(
                            cartId,
                            phoneLine.Models.OfType<LineAdded>().FirstOrDefault().LineId,
                            context.Components.OfType<PhysicalFulfillmentComponent>().First()));

                    Proxy.DoCommand(
                        container.SetCartLineFulfillment(
                            cartId,
                            gwLine.Models.OfType<LineAdded>().FirstOrDefault().LineId,
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
