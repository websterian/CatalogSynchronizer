using System;
using System.Linq;
using Bogus;
using FluentAssertions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Availability;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Customers;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Commerce.Plugin.GiftCards;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Commerce.Sample.Console.Properties;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.Sample.Scenarios;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Orders
    {
        private static int _requestedTestRuns = 1;
        private static string _createdGiftCard = string.Empty;

        private static decimal _totalOrderDollars;
        private static int _totalOrders;

        public static void RunScenarios()
        {
            var value = Settings.Default.RequestedTestRuns;
            _requestedTestRuns = value;

            var opsUser = new DevOpAndre();
            Randomizer.Seed = new Random(3897234); // Set the randomizer seed if you wish to generate repeatable data sets.
            var testParties = new Faker<Party>()
                .RuleFor(u => u.FirstName, f => f.Name.FirstName())
                .RuleFor(u => u.LastName, f => f.Name.LastName())
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
                .RuleFor(u => u.AddressName, f => "FulfillmentPartyName")
                .RuleFor(u => u.Address1, f => f.Address.StreetAddress(true))
                .RuleFor(u => u.City, f => f.Address.City())
                .RuleFor(u => u.StateCode, f => "WA")
                .RuleFor(u => u.State, f => "Washington")
                .RuleFor(u => u.ZipPostalCode, f => "93612")
                .RuleFor(u => u.CountryCode, f => "US")
                .RuleFor(u => u.Country, f => "United States")
                .FinishWith((f, u) => { System.Console.WriteLine($"BogusUser Address1={u.Address1}|City={u.City}"); });

            using (new SampleScenarioScope("Orders"))
            {
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("---------------------------------------------------");
                System.Console.WriteLine($"Requested Runs: {_requestedTestRuns}");
                System.Console.WriteLine("---------------------------------------------------");
                System.Console.ForegroundColor = ConsoleColor.Cyan;

                for (var i = 1; i <= _requestedTestRuns; i++)
                {
                    try
                    {
                        //Anonymous Customer Jeff Order Samples
                        var bogusParty = testParties.Generate();
                        var jeff = new AnonymousCustomerJeff();
                        jeff.Context.Components.OfType<ElectronicFulfillmentComponent>().First().EmailAddress =
                            bogusParty.Email;
                        var physicalFulfillmentComponent = jeff.Context.Components
                            .OfType<PhysicalFulfillmentComponent>()
                            .FirstOrDefault();
                        if (physicalFulfillmentComponent != null)
                        {
                            physicalFulfillmentComponent.ShippingParty = bogusParty;
                        }

                        var danaHab = new RegisteredHabitatCustomerDana();
                        bogusParty = testParties.Generate();
                        danaHab.Context.Components.OfType<ElectronicFulfillmentComponent>().First().EmailAddress =
                            bogusParty.Email;
                        physicalFulfillmentComponent = danaHab.Context.Components.OfType<PhysicalFulfillmentComponent>()
                            .FirstOrDefault();
                        if (physicalFulfillmentComponent != null)
                        {
                            physicalFulfillmentComponent.ShippingParty = bogusParty;
                        }

                        danaHab.Context.CustomerId = AddCustomer(
                            danaHab.Context.Components.OfType<ElectronicFulfillmentComponent>().First().EmailAddress,
                            danaHab.Context,
                            bogusParty.FirstName,
                            bogusParty.LastName);

                        // RegisteredCustomer Dana Order Samples
                        var danaAdv = new RegisteredCustomerDana();
                        bogusParty = testParties.Generate();
                        danaAdv.Context.Components.OfType<ElectronicFulfillmentComponent>().First().EmailAddress =
                            bogusParty.Email;
                        danaAdv.Context.CustomerId = AddCustomer(
                            danaAdv.Context.Components.OfType<ElectronicFulfillmentComponent>().First().EmailAddress,
                            danaAdv.Context,
                            bogusParty.FirstName,
                            bogusParty.LastName);

                        var lastSimplePhysical = SimplePhysical2Items.Run(jeff.Context);
                        var lastGiftCardOrder = BuyGiftCards.Run(jeff.Context);
                        _createdGiftCard = jeff.Context.GiftCards.First();

                        var lastBuyWithGiftCard = BuyWithGiftCard.Run(jeff.Context);

                        SimplePhysicalRtrn15Coupon.Run(jeff.Context);
                        AddRemovePayment.Run(jeff.Context);
                        AddRemoveCoupon.Run(jeff.Context);

                        UseLimitedCouponMoreThanOnce.Run(jeff.Context);

                        OnSaleItem.Run(jeff.Context);
                        BuyBackOrderedItem.Run(jeff.Context);
                        BuyPreOrderableItem.Run(jeff.Context);
                        BuyAvailabilitySplitItem.Run(jeff.Context);
                        AddRemoveCartLine.Run(jeff.Context);
                        var lastSplitShipment = BuySplitShipment.Run(jeff.Context);

                        ////International
                        var katrina = new InternationalShopperKatrina();

                        var result = Proxy.GetValue(
                            katrina.Context.ShopsContainer()
                                .SellableItems.ByKey("Adventure Works Catalog,AW055 01,33")
                                .Expand(
                                    "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                        result.Should().NotBeNull();
                        result.ListPrice.CurrencyCode.Should().Be("EUR");
                        result.ListPrice.Amount.Should().Be(1.0M);

                        ////These samples leverage EntityViews and EntityActions
                        ////For post order management
                        ////Place a Split shipment order and then put it on hold
                        SplitShipmentOnHold.Run(jeff.Context);
                        ////Place a Split shipment order, place it on hold and delete one of the lines
                        SplitShipmentThenDeleteLine.Run(jeff.Context);

                        ////Retrieve the Business User "View" for the SplitShipment end order.  The Order should still be in a Pending State
                        Proxy.GetValue(
                            jeff.Context.ShopsContainer()
                                .GetEntityView(lastSplitShipment, "Master", string.Empty, string.Empty));

                        var cancelOrderId = Buy3Items.Run(jeff.Context);
                        if (!string.IsNullOrEmpty(cancelOrderId))
                        {
                            CancelOrder(cancelOrderId);
                        }

                        ////RegisteredCustomer Dana Order Samples
                        SimplePhysical2Items.Run(danaAdv.Context);
                        BuyGiftCards.Run(danaAdv.Context);
                        BuyWarranty.Run(danaHab.Context, 1);

                        danaHab.GoShopping();

                        //Force the pending orders Minion to run
                        RunPendingOrdersMinion(opsUser.Context);

                        //Force the Released Orders Minion to run
                        RunReleasedOrdersMinion(opsUser.Context);

                        //The Following represent examples of using the Business User (BizOps) Api to handle Orders.
                        //At this stage, the orders have been released and only actions allowed on Released or Problem orders can occur

                        //Get the last SimplePhysical order to show how it is changed by the Minions
                        var orderMaster = Proxy.GetEntityView(
                            jeff.Context.ShopsContainer(),
                            lastSimplePhysical,
                            "Master",
                            string.Empty,
                            string.Empty);
                        if (!orderMaster.ChildViews.Any(p => p.Name.EqualsIgnoreCase("Shipments")))
                        {
                            ConsoleExtensions.WriteErrorLine($"LastSimplePhysical.MissingShipments: OrderId={lastSimplePhysical}");
                        }

                        ////Get the last SimplePhysical order to show how it is changed by the Minions
                        var lastGiftCardMaster = Proxy.GetEntityView(
                            jeff.Context.ShopsContainer(),
                            lastGiftCardOrder,
                            "Master",
                            string.Empty,
                            string.Empty);

                        ////There should not be a Shipments child EntityView because this was a Digital order
                        lastGiftCardMaster.ChildViews.Count(p => p.Name == "Shipments").Should().Be(0);

                        var giftCardNew = jeff.Context.ShopsContainer()
                            .GiftCards.ByKey("Entity-GiftCard-" + _createdGiftCard)
                            .Expand("Components($expand=ChildComponents)")
                            .GetValue() as GiftCard;
                        giftCardNew.Should().NotBeNull();

                        var lastBuyWithGiftCardView = Proxy.GetEntityView(
                            jeff.Context.ShopsContainer(),
                            lastBuyWithGiftCard,
                            "Master",
                            string.Empty,
                            string.Empty);
                        var salesActivities =
                            lastBuyWithGiftCardView.ChildViews.First(p => p.Name == "SalesActivities") as EntityView;
                        salesActivities.ChildViews.Count.Should().Be(2);

                        // Example of updating the Order Status from via a service.  This is for external systems to push status updates
                        SetOrderStatus();
                    }
                    catch (Exception ex)
                    {
                        ConsoleExtensions.WriteColoredLine(ConsoleColor.Red, $"Test Exception - {ex.Message}");
                        ConsoleExtensions.WriteColoredLine(ConsoleColor.Red, $"Test Exception - {ex.StackTrace}");
                    }

                    System.Console.ForegroundColor = ConsoleColor.Cyan;
                    System.Console.WriteLine("---------------------------------------------------");
                    System.Console.WriteLine($"Test pass {i} of {_requestedTestRuns}");
                    System.Console.WriteLine($"Orders:{_totalOrders} Dollars:{_totalOrderDollars}");
                    System.Console.ForegroundColor = ConsoleColor.Cyan;
                }
            }
        }

        public static string AddCustomer(string email = "", ShopperContext context = null, string firstName = "FirstName", string lastName = "LastName")
        {
            var container = context.ShopsContainer();
            var view = Proxy.GetValue(container.GetEntityView(string.Empty, "Details", "AddCustomer", string.Empty));
            view.Should().NotBeNull();
            view.Properties.Should().NotBeEmpty();
            view.Action.Should().Be("AddCustomer");
            view.Properties.Should().NotBeEmpty();
            view.Properties.FirstOrDefault(p => p.Name.Equals("Email")).Value = email;
            view.Properties.FirstOrDefault(p => p.Name.Equals("FirstName")).Value = firstName;
            view.Properties.FirstOrDefault(p => p.Name.Equals("LastName")).Value = lastName;
            view.Properties.FirstOrDefault(p => p.Name.Equals("LoginName")).Value = lastName;
            view.Properties.FirstOrDefault(p => p.Name.Equals("Domain")).Value = "CommerceUsers";
            view.Properties.FirstOrDefault(p => p.Name.Equals("AccountStatus")).Value = "ActiveAccount";

            var action = Proxy.DoCommand(container.DoAction(view));
            action.Models.OfType<CustomerAdded>().FirstOrDefault().Should().NotBeNull();

            return action.Models.OfType<CustomerAdded>().FirstOrDefault()?.CustomerId;
        }

        public static Order GetOrder(Container container, string orderId)
        {
            var order = Proxy.GetValue(
                container.Orders.ByKey(orderId).Expand("Lines($expand=CartLineComponents),Components"));

            return order;
        }

        public static CommerceCommand CreateOrder(Container container, string cartId, ShopperContext context)
        {
            var resolveEmail = "no@email.com";
            var electronicFulfillmentComponent = context.Components.OfType<ElectronicFulfillmentComponent>().FirstOrDefault();
            if (electronicFulfillmentComponent != null)
            {
                resolveEmail = electronicFulfillmentComponent.EmailAddress;
            }

            var command = Proxy.DoCommand(container.CreateOrder(cartId, resolveEmail));
            var totals = command.Models.OfType<Totals>().FirstOrDefault();
            if (totals == null)
            {
                ConsoleExtensions.WriteErrorLine("CreateOrder - NoTotals");
            }
            else
            {
                _totalOrderDollars = _totalOrderDollars + totals.GrandTotal.Amount;
                _totalOrders += 1;
            }

            return command;
        }

        public static Order CreateAndValidateOrder(Container container, string cartId, ShopperContext context)
        {
            var commandResponse = CreateOrder(container, cartId, context);
            var order = GetOrder(container, commandResponse.Models.OfType<CreatedOrder>().First().OrderId);

            ValidateOrder(order);

            return order;
        }

        public static Order ValidateOrder(Order order)
        {
            order.Id.Should().NotBeNull();
            order.OrderConfirmationId.Should().NotBeNull();
            order.Lines.Should().NotBeEmpty();
            order.Status.Should().Be("Pending");

            order.Totals.PaymentsTotal.Amount.Should().Be(order.Totals.GrandTotal.Amount);

            foreach (var line in order.Lines)
            {
                line.ItemId.Should().NotBeNullOrEmpty();
                line.Quantity.Should().NotBe(0);

                line.Totals.GrandTotal.Amount.Should().BeGreaterThan(0);

                var cartProductComponent = line.CartLineComponents.OfType<CartProductComponent>().FirstOrDefault();
                cartProductComponent.Should().NotBeNull();

                var itemAvailabilityComponent = line.CartLineComponents.OfType<ItemAvailabilityComponent>().FirstOrDefault();
                if (cartProductComponent.Policies.OfType<AvailabilityAlwaysPolicy>().Any())
                {
                    // This item is always available and will have no ItemAvailabilityComponent
                    (itemAvailabilityComponent == null).Should().BeTrue($"ItemAvailabilityComponent Should be null.");
                }
                else
                {
                    itemAvailabilityComponent.Should().NotBeNull();
                    if (itemAvailabilityComponent.ItemId != line.ItemId)
                    {
                        ConsoleExtensions.WriteColoredLine(ConsoleColor.Red, $"ItemAvailability.ItemId does not match: Expected:{line.ItemId} Received:{itemAvailabilityComponent.ItemId}");
                    }
                }

                line.Policies.OfType<PurchaseOptionMoneyPolicy>().Should().HaveCount(1);

                var purchaseOptionMoneyPolicy = line.Policies.OfType<PurchaseOptionMoneyPolicy>().First();

                purchaseOptionMoneyPolicy.SellPrice.Amount.Should().BeGreaterThan(0);
            }

            return order;
        }

        public static void RunPendingOrdersMinion(ShopperContext context)
        {
            EngineExtensions.RunMinionWithRetry(
                "Sitecore.Commerce.Plugin.Orders.PendingOrdersMinionBoss, Sitecore.Commerce.Plugin.Orders",
                ResolveMinionEnvironment(context),
                true);
        }

        public static void RunReleasedOrdersMinion(ShopperContext context)
        {
            EngineExtensions.RunMinionWithRetry(
                "Sitecore.Commerce.Plugin.Orders.ReleasedOrdersMinion, Sitecore.Commerce.Plugin.Orders",
                ResolveMinionEnvironment(context));
        }

        private static void CancelOrder(string orderId)
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(new AnonymousCustomerJeff().Context.ShopsContainer().CancelOrder(orderId));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void SetOrderStatus()
        {
            using (new SampleMethodScope())
            {
                var container = new AnonymousCustomerJeff();

                var orderId = Buy3Items.Run(container.Context);
                var result = Proxy.DoCommand(
                    new AnonymousCustomerJeff().Context.ShopsContainer().SetOrderStatus(orderId, "CustomStatus"));
                result.Messages.Should().NotContainErrors();

                var order = GetOrder(container.Context.ShopsContainer(), orderId);
                order.Should().NotBeNull();
                order.Status.Should().Be("CustomStatus");
            }
        }

        private static string ResolveMinionEnvironment(ShopperContext context)
        {
            switch (context.Environment)
            {
                case EnvironmentConstants.AdventureWorksShops:
                    return EnvironmentConstants.AdventureWorksMinions;
                case EnvironmentConstants.AdventureWorksAuthoring:
                    return EnvironmentConstants.AdventureWorksMinions;
                case EnvironmentConstants.HabitatAuthoring:
                    return EnvironmentConstants.HabitatMinions;
                case EnvironmentConstants.HabitatShops:
                    return EnvironmentConstants.HabitatMinions;
            }

            throw new InvalidOperationException($"{context.Environment} is not a valid environment.");
        }
    }
}
