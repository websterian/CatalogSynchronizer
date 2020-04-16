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
    public static class BuyWarranty
    {
        public static string ScenarioName = "BuyWarranty";

        public static string Run(ShopperContext context, decimal quantity)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    Proxy.DoCommand(container.AddCartLine(cartId, "Habitat_Master|7042259|57042259", quantity));

                    var result = Proxy.DoCommand(
                        container.SetCartFulfillment(
                            cartId,
                            new ElectronicFulfillmentComponent
                            {
                                FulfillmentMethod = new EntityReference
                                {
                                    EntityTarget = "8A23234F-8163-4609-BD32-32D9DD6E32F5",
                                    Name = "Email"
                                },
                                EmailAddress = "g@g.com",
                                EmailContent = "this is the content of the email"
                            }));
                    var totals = result.Models.OfType<Totals>().FirstOrDefault();
                    totals.Should().NotBeNull();
                    totals?.GrandTotal.Should().NotBeNull();
                    totals?.GrandTotal.Amount.Should().NotBe(0);
                    var paymentComponent = context.Components.OfType<FederatedPaymentComponent>().First();
                    paymentComponent.Amount = Money.CreateMoney(totals.GrandTotal.Amount);
                    result = Proxy.DoCommand(container.AddFederatedPayment(cartId, paymentComponent));
                    totals = result.Models.OfType<Totals>().FirstOrDefault();
                    totals.Should().NotBeNull();
                    totals?.GrandTotal.Should().NotBeNull();
                    totals?.GrandTotal.Amount.Should().NotBe(0);
                    totals?.PaymentsTotal.Should().NotBeNull();
                    totals?.PaymentsTotal.Amount.Should().NotBe(0);

                    var order = Orders.CreateAndValidateOrder(container, cartId, context);
                    order.Status.Should().NotBe("Problem");
                    order.Totals.GrandTotal.Amount.Should().Be(totals.GrandTotal.Amount);

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
