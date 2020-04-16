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
    public static class OnSaleItem
    {
        public static string ScenarioName = "OnSaleItem";

        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW188 06|20", 1));
                    Proxy.DoCommand(
                        container.SetCartFulfillment(
                            cartId,
                            context.Components.OfType<PhysicalFulfillmentComponent>().First()));

                    var giftCardToUse = context.GiftCards.First();

                    var commandResult = Proxy.DoCommand(
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

                    var totals = commandResult.Models.OfType<Totals>().First();

                    var paymentComponent = context.Components.OfType<FederatedPaymentComponent>().First();
                    paymentComponent.Amount = Money.CreateMoney(totals.GrandTotal.Amount - totals.PaymentsTotal.Amount);
                    commandResult = Proxy.DoCommand(
                        container.AddFederatedPayment(
                            cartId,
                            paymentComponent));

                    totals = commandResult.Models.OfType<Totals>().First();

                    totals.PaymentsTotal.Amount.Should().Be(totals.GrandTotal.Amount);

                    var order = Orders.CreateAndValidateOrder(container, cartId, context);
                    order.Totals.GrandTotal.Amount.Should().Be(119.50M);

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
