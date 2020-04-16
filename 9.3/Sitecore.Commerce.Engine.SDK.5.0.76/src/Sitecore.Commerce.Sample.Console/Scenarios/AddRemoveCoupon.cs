using System;
using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Coupons;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Commerce.Plugin.Payments;
using Sitecore.Commerce.Sample.Console;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Scenarios
{
    public static class AddRemoveCoupon
    {
        public static string ScenarioName = "AddRemoveCoupon";

        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    //Add Cart Line with Variant
                    var commandResult = Proxy.DoCommand(
                        container.AddCartLine(cartId, "Adventure Works Catalog|AW098 04|5", 1));
                    var cartLineId = commandResult.Models.OfType<LineAdded>().FirstOrDefault()?.LineId;

                    //Add Cart Line without Variant
                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));

                    //Show adding an invalid coupon
                    Proxy.DoCommand(container.AddCouponToCart(cartId, "InvalidCouponCode"));
                    ConsoleExtensions.WriteExpectedError();

                    // Add a valid coupon, remove it and add again
                    Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNC10P"));

                    var cart = Carts.GetCart(cartId, context);
                    cart.Components.OfType<CartCouponsComponent>().FirstOrDefault().Should().NotBeNull();

                    Proxy.DoCommand(container.RemoveCouponFromCart(cartId, "RTRNC10P"));
                    cart = Carts.GetCart(cartId, context);
                    cart.Components.OfType<CartCouponsComponent>().FirstOrDefault().Should().BeNull();

                    Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNC10P"));

                    Proxy.DoCommand(container.RemoveCartLine(cartId, "BadLineId"));
                    ConsoleExtensions.WriteExpectedError();

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

                    order.Totals.GrandTotal.Amount.Should().Be(1109.00M);

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
