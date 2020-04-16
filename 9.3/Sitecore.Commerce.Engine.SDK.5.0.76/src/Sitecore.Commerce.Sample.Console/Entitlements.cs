using System;
using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Commerce.Plugin.DigitalItems;
using Sitecore.Commerce.Plugin.Entitlements;
using Sitecore.Commerce.Plugin.GiftCards;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.Sample.Scenarios;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Entitlements
    {
        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Entitlements"))
            {
                // Adventure Works
                var jeff = new AnonymousCustomerJeff();

                // Habitat
                var steve = new AnonymousCustomerSteve();

                BuyPhysicalAndGiftCard(jeff.Context);
                BuyGiftCard(jeff.Context);
                BuyGiftCards(jeff.Context);
                BuyWarranty(steve.Context);
                BuyInstallation(steve.Context);
                BuySubscription(steve.Context);
                BuyOneOfEach(steve.Context);
                BuyGiftCardAuthenticated(jeff.Context);
            }
        }

        private static void BuyPhysicalAndGiftCard(ShopperContext context)
        {
            using (new SampleMethodScope())
            {
                var orderId = Simple2PhysicalDigitalItems.Run(context);
                orderId.Should().NotBeNull();

                RunMinions(context);

                var order = ValidateOrder(context, orderId);
                ValidateEntitlements(context, order, null, 1, typeof(GiftCard));
            }
        }

        private static void BuyGiftCard(ShopperContext context)
        {
            using (new SampleMethodScope())
            {
                var orderId = Scenarios.BuyGiftCard.Run(context, 2);
                orderId.Should().NotBeNull();

                RunMinions(context);

                var order = ValidateOrder(context, orderId);
                ValidateEntitlements(context, order, null, 2, typeof(GiftCard));
            }
        }

        private static void BuyGiftCardAuthenticated(ShopperContext context)
        {
            using (new SampleMethodScope())
            {
                var customerId = CustomersUX.AddCustomer(CustomersUX.GenerateRandomUserName());
                customerId.Should().NotBeNullOrEmpty();

                context.CustomerId = customerId;
                context.ShopperId = customerId;
                context.IsRegistered = true;

                var orderId = Scenarios.BuyGiftCard.Run(context, 1);
                orderId.Should().NotBeNull();

                RunMinions(context);

                var order = ValidateOrder(context, orderId);
                var customer = ValidateCustomer(context, customerId);
                ValidateEntitlements(context, order, customer, 1, typeof(GiftCard));
            }
        }

        private static void BuyGiftCards(ShopperContext context)
        {
            using (new SampleMethodScope())
            {
                var orderId = Scenarios.BuyGiftCards.Run(context);
                orderId.Should().NotBeNull();

                RunMinions(context);

                var order = ValidateOrder(context, orderId);
                ValidateEntitlements(context, order, null, 2, typeof(GiftCard));
            }
        }

        private static void BuyWarranty(ShopperContext context)
        {
            using (new SampleMethodScope())
            {
                var orderId = Scenarios.BuyWarranty.Run(context, 1);
                orderId.Should().NotBeNull();

                RunMinions(context);

                var order = ValidateOrder(context, orderId);
                ValidateEntitlements(context, order, null, 1, typeof(Warranty));
            }
        }

        private static void BuyInstallation(ShopperContext context)
        {
            using (new SampleMethodScope())
            {
                var orderId = Scenarios.BuyInstallation.Run(context, 1);
                orderId.Should().NotBeNull();

                RunMinions(context);

                var order = ValidateOrder(context, orderId);
                ValidateEntitlements(context, order, null, 1, typeof(Installation));
            }
        }

        private static void BuySubscription(ShopperContext context)
        {
            using (new SampleMethodScope())
            {
                var orderId = Scenarios.BuySubscription.Run(context, 1);
                orderId.Should().NotBeNull();

                RunMinions(context);

                var order = ValidateOrder(context, orderId);
                ValidateEntitlements(context, order, null, 1, typeof(DigitalProduct));
            }
        }

        private static void BuyOneOfEach(ShopperContext context)
        {
            using (new SampleMethodScope())
            {
                var orderId = BuyAllDigitals.Run(context, 1);
                orderId.Should().NotBeNull();

                RunMinions(context);

                var order = ValidateOrder(context, orderId);
                ValidateEntitlements(context, order, null, 4);
            }
        }

        private static void RunMinions(ShopperContext context)
        {
            Orders.RunPendingOrdersMinion(context);
            Orders.RunReleasedOrdersMinion(context);
        }

        private static Order ValidateOrder(ShopperContext context, string orderId)
        {
            var order = Orders.GetOrder(context.ShopsContainer(), orderId);
            order.Should().NotBeNull();
            order.Status.Should().Be("Completed");

            order.Components.OfType<EntitlementsComponent>().Any().Should().BeTrue();
            var entitlementsComponent = order.Components.OfType<EntitlementsComponent>().FirstOrDefault();
            entitlementsComponent.Should().NotBeNull();
            entitlementsComponent?.Entitlements.Should().NotBeEmpty();
            entitlementsComponent?.Entitlements.All(e => !string.IsNullOrEmpty(e.EntityTarget)).Should().BeTrue();

            return order;
        }

        private static Customer ValidateCustomer(ShopperContext context, string customerId)
        {
            var customer = CustomersUX.GetCustomer(context.ShopsContainer(), customerId);
            customer.Should().NotBeNull();

            customer.Components.OfType<EntitlementsComponent>().Any().Should().BeTrue();
            var entitlementsComponent = customer.Components.OfType<EntitlementsComponent>().FirstOrDefault();
            entitlementsComponent.Should().NotBeNull();
            entitlementsComponent?.Entitlements.Should().NotBeEmpty();
            entitlementsComponent?.Entitlements.All(e => !string.IsNullOrEmpty(e.EntityTarget)).Should().BeTrue();

            return customer;
        }

        private static void ValidateEntitlements(ShopperContext context, Order order, Customer customer, int count, Type type = null)
        {
            var entitlementsComponent = order.Components.OfType<EntitlementsComponent>().FirstOrDefault();
            entitlementsComponent.Should().NotBeNull();
            entitlementsComponent?.Entitlements.Should().NotBeEmpty();
            entitlementsComponent?.Entitlements.Count.Should().Be(count);

            if (customer != null)
            {
                entitlementsComponent = customer.Components.OfType<EntitlementsComponent>().FirstOrDefault();
                entitlementsComponent.Should().NotBeNull();
                entitlementsComponent?.Entitlements.Should().NotBeEmpty();
                entitlementsComponent?.Entitlements.Count.Should().Be(count);
            }

            foreach (var entitlementReference in entitlementsComponent.Entitlements)
            {
                Entitlement entitlement = null;

                if (entitlementReference.EntityTarget.StartsWith("Entity-GiftCard-"))
                {
                    entitlement = Proxy.GetValue(context.ShopsContainer().GiftCards.ByKey(entitlementReference.EntityTarget));
                }
                else if (entitlementReference.EntityTarget.StartsWith("Entity-DigitalProduct-"))
                {
                    entitlement = Proxy.GetValue(context.ShopsContainer().DigitalProducts.ByKey(entitlementReference.EntityTarget));
                }
                else if (entitlementReference.EntityTarget.StartsWith("Entity-Installation-"))
                {
                    entitlement = Proxy.GetValue(context.ShopsContainer().Installations.ByKey(entitlementReference.EntityTarget));
                }
                else if (entitlementReference.EntityTarget.StartsWith("Entity-Warranty-"))
                {
                    entitlement = Proxy.GetValue(context.ShopsContainer().Warranties.ByKey(entitlementReference.EntityTarget));
                }

                entitlement.Should().NotBeNull();
                entitlement.Order.Should().NotBeNull();
                entitlement.Order?.EntityTarget.Should().Be(order.Id);

                if (type != null)
                {
                    (entitlement.GetType() == type).Should().BeTrue();
                }

                if (customer != null)
                {
                    entitlement.Customer.Should().NotBeNull();
                    entitlement.Customer?.EntityTarget.Should().Be(customer.Id);
                }
                else if (type != null && type == typeof(GiftCard) || type == null && entitlementReference.EntityTarget.StartsWith("Entity-GiftCard-"))
                {
                    entitlement.Customer.Should().NotBeNull();
                    entitlement.Customer?.EntityTarget.Should().Be("DefaultUser");
                }
                else
                {
                    entitlement.Customer.Should().BeNull();
                }
            }
        }
    }
}
