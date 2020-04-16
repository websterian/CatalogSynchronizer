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
    /// <summary>
    /// Try to use a limited single use coupon more than once.
    /// </summary>
    public static class UseLimitedCouponMoreThanOnce
    {
        public static string ScenarioName = "UseLimitedCouponMoreThanOnce";

        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    // Add Cart Line with Variant
                    var commandResult = Proxy.DoCommand(
                        container.AddCartLine(cartId, "Adventure Works Catalog|AW098 04|5", 1));
                    var cartLineId = commandResult.Models.OfType<LineAdded>().FirstOrDefault()?.LineId;

                    // Add Cart Line without Variant
                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));

                    // Add a valid coupon
                    Proxy.DoCommand(container.AddCouponToCart(cartId, "SingleUseCouponCode"));

                    Proxy.DoCommand(container.UpdateCartLine(cartId, cartLineId, 10));

                    commandResult = Proxy.DoCommand(
                        container.SetCartFulfillment(
                            cartId,
                            context.Components.OfType<PhysicalFulfillmentComponent>().First()));

                    var totals = commandResult.Models.OfType<Totals>().First();

                    var paymentComponent = context.Components.OfType<FederatedPaymentComponent>().First();
                    paymentComponent.Amount = Money.CreateMoney(totals.GrandTotal.Amount);

                    Proxy.DoCommand(container.AddFederatedPayment(cartId, paymentComponent));

                    var order = Orders.CreateAndValidateOrder(container, cartId, context);
                    order.Totals.GrandTotal.Amount.Should().Be(1219.90M);

                    cartId = Guid.NewGuid().ToString("B");

                    // Add Cart Line with Variant
                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW098 04|5", 1));

                    // Add Cart Line without Variant
                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));

                    // Add a valid coupon
                    commandResult = Proxy.DoCommand(container.AddCouponToCart(cartId, "SingleUseCouponCode"));
                    commandResult.ResponseCode.Should()
                        .NotBe(
                            "Ok",
                            "Expecting failure as this coupon code is single use only and has been used prior");
                    ConsoleExtensions.WriteExpectedError();

                    return order.Id;
                }
                catch (Exception ex)
                {
                    ConsoleExtensions.WriteColoredLine(ConsoleColor.Red, $"Exception in Scenario {ScenarioName} (${ex.Message}) : Stack={ex.StackTrace}");
                    return null;
                }
            }
        }
    }
}
