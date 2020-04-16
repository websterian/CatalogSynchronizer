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
    public static class Simple2PhysicalDigitalItems
    {
        public static string Run(ShopperContext context)
        {
            using (new SampleBuyScenarioScope())
            {
                try
                {
                    var container = context.ShopsContainer();

                    var cartId = Carts.GenerateCartId();

                    var commandResponse = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|22565422120|100", 1));
                    var firstLineId = commandResponse.Models.OfType<LineAdded>().FirstOrDefault()?.LineId;

                    commandResponse = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW188 06|19", 1));
                    var secondLineId = commandResponse.Models.OfType<LineAdded>().FirstOrDefault()?.LineId;

                    Proxy.DoCommand(container.SetCartLineFulfillment(cartId, firstLineId, context.Components.OfType<ElectronicFulfillmentComponent>().First()));
                    commandResponse = Proxy.DoCommand(container.SetCartLineFulfillment(cartId, secondLineId, context.Components.OfType<PhysicalFulfillmentComponent>().First()));

                    var totals = commandResponse.Models.OfType<Totals>().First();
                    totals.AdjustmentsTotal.Amount.Should().Be(0M);
                    totals.GrandTotal.Amount.Should().Be(229M);

                    // Add a Payment
                    var paymentComponent = context.Components.OfType<FederatedPaymentComponent>().First();
                    paymentComponent.Amount = Money.CreateMoney(totals.GrandTotal.Amount - totals.PaymentsTotal.Amount);
                    commandResponse = Proxy.DoCommand(container.AddFederatedPayment(cartId, paymentComponent));
                    totals = commandResponse.Models.OfType<Totals>().First();
                    totals.PaymentsTotal.Amount.Should().Be(229M);

                    // Get the cart one last time before creating the order
                    var cart = Carts.GetCart(cartId, context);
                    cart.Version.Should().Be(5);
                    var order = Orders.CreateAndValidateOrder(container, cartId, context);
                    order.Status.Should().NotBe("Problem");
                    order.Totals.GrandTotal.Amount.Should().Be(229.0M);

                    return order.Id;
                }
                catch (Exception ex)
                {
                    ConsoleExtensions.WriteErrorLine($"Exception in Scenario {nameof(Simple2PhysicalDigitalItems)} (${ex.Message}) : Stack={ex.StackTrace}");
                    return null;
                }
            }
        }
    }
}
