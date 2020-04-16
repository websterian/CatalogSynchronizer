using System;
using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Commerce.Plugin.GiftCards;
using Sitecore.Commerce.Plugin.Payments;
using Sitecore.Commerce.Sample.Console;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Scenarios
{
    public static class BuyWithGiftCard
    {
        public static string ScenarioName = "BuyWithGiftCard";

        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    // Add Cart Line with Variant
                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW098 04|5", 1));

                    // Add Cart Line without Variant
                    var commandResult = Proxy.DoCommand(
                        container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));

                    var totals = commandResult.Models.OfType<Totals>().First();

                    Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNC15P"));

                    commandResult = Proxy.DoCommand(
                        container.SetCartFulfillment(
                            cartId,
                            context.Components.OfType<PhysicalFulfillmentComponent>().First()));
                    totals = commandResult.Models.OfType<Totals>().First();

                    var giftCardToUse = context.GiftCards.First();

                    commandResult = Proxy.DoCommand(
                        container.AddGiftCardPayment(
                            cartId,
                            new GiftCardPaymentComponent
                            {
                                PaymentMethod = new EntityReference
                                {
                                    EntityTarget = "B5E5464E-C851-4C3C-8086-A4A874DD2DB0",
                                    Name = "GiftCard"
                                },
                                GiftCardCode = giftCardToUse,
                                Amount = Money.CreateMoney(50),
                            }));

                    totals = commandResult.Models.OfType<Totals>().First();

                    var paymentComponent = context.Components.OfType<FederatedPaymentComponent>().First();
                    paymentComponent.Amount = Money.CreateMoney(totals.GrandTotal.Amount - totals.PaymentsTotal.Amount);
                    commandResult = Proxy.DoCommand(container.AddFederatedPayment(cartId, paymentComponent));

                    commandResult.Models.OfType<Totals>().First();

                    var order = Orders.CreateAndValidateOrder(container, cartId, context);

                    order.Totals.GrandTotal.Amount.Should().Be(155.80M);

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
