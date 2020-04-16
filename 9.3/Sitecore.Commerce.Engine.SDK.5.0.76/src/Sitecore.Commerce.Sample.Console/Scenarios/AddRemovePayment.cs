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
    public static class AddRemovePayment
    {
        public static string ScenarioName = "AddRemovePayment";

        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    //Add Cart Line with Variant
                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW098 04|5", 1));

                    //Add Cart Line without Variant
                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));

                    var commandResult = Proxy.DoCommand(
                        container.SetCartFulfillment(
                            cartId,
                            context.Components.OfType<PhysicalFulfillmentComponent>().First()));

                    var totals = commandResult.Models.OfType<Totals>().First();

                    var paymentComponent = context.Components.OfType<FederatedPaymentComponent>().First();
                    paymentComponent.Amount = Money.CreateMoney(totals.GrandTotal.Amount);

                    Proxy.DoCommand(
                        container.AddFederatedPayment(
                            cartId,
                            paymentComponent));

                    var cart = Proxy.GetValue(
                        container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));

                    var federatedPaymentComponent = cart.Components.OfType<FederatedPaymentComponent>().First();

                    container.RemovePayment(cartId, federatedPaymentComponent.Id).GetValue();

                    cart = Proxy.GetValue(
                        container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));

                    paymentComponent = context.Components.OfType<FederatedPaymentComponent>().First();
                    paymentComponent.Amount = Money.CreateMoney(cart.Totals.GrandTotal.Amount);

                    Proxy.DoCommand(container.AddFederatedPayment(cartId, paymentComponent));

                    var order = Orders.CreateAndValidateOrder(container, cartId, context);

                    order.Totals.GrandTotal.Amount.Should().Be(180.40M);

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
