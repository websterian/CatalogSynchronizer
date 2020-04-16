using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Availability;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Coupons;
using Sitecore.Commerce.Plugin.ManagedLists;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;
using Sitecore.Commerce.ServiceProxy.Extensions;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Carts
    {
        private const string UpdatedCartExpands = "UpdatedCart($expand= Lines($expand = CartLineComponents, CartSubLineComponents($expand = CartLineComponents)), Components)";
        private static readonly ShopperContext AwShopperContext = new AnonymousCustomerJeff().Context;
        private static readonly Container AwShopsContainer = AwShopperContext.ShopsContainer();
        private static readonly Container HabitatShopsContainer = new AnonymousCustomerSteve().Context.ShopsContainer();

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Carts"))
            {
                AddToCartPhysical();
                AddToCartPhysicalAndDigital();
                AddToCartDigital();

                MergeCarts_BothHasLines();
                MergeCarts_LinesRollup();
                MergeCarts_FromWithComponents();
                MergeCarts_ToWithComponents();

                RunPurgeCartsMinion();

                GetCart_DoNotCalculate();
                GetCart_DoNotCalculate_EnsureCartProperties();
                AddToCart_GetCart_ProductAndBundle();
                RemoveLineFromCart_UpdatedCart();
                UpdateCartLine_UpdatedCart();
            }
        }

        public static void GetCart_DoNotCalculate_EnsureCartProperties()
        {
            using (new SampleMethodScope())
            {
                // First test an empty cart, not persisted
                var cartId = GenerateCartId();
                var cart = GetCart(cartId, AwShopperContext.ShopsContainer());

                var contactComponent = cart.Components.OfType<ContactComponent>().FirstOrDefault();

                // Get the cart again - specify do not calculate
                var originalPolicyKeys = AwShopperContext.PolicyKeys;
                AwShopperContext.PolicyKeys = originalPolicyKeys + "|DoNotCalculateCart";

                var cart2 = GetCart(cartId, AwShopperContext.ShopsContainer());
                var contactComponent2 = cart2.Components.OfType<ContactComponent>().FirstOrDefault();

                cart.Should().NotBeNull();
                contactComponent.Should().NotBeNull();
                cart2.Should().NotBeNull();
                contactComponent2.Should().NotBeNull();

                cart.Components.Count.Should().Be(2);
                cart2.Components.Count.Should().Be(2);

                cart.CreatedBy.Should().Be(cart2.CreatedBy);
                cart.DateCreated.Should().Be(cart2.DateCreated);
                cart.DateUpdated.Should().Be(cart2.DateUpdated);
                cart.DisplayName.Should().Be(cart2.DisplayName);
                cart.EntityVersion.Should().Be(cart2.EntityVersion);
                cart.ShopName.Should().Be(cart2.ShopName);
                cart.Version.Should().Be(cart2.Version);
                cart.Policies.Count.Should().Be(cart2.Policies.Count);

                contactComponent2.Currency.Should().Be(contactComponent.Currency);
                contactComponent2.CustomerId.Should().Be(contactComponent.CustomerId);
                contactComponent2.Email.Should().Be(contactComponent.Email);
                contactComponent2.Name.Should().Be(contactComponent.Name);
                contactComponent2.ShopperId.Should().Be(contactComponent.ShopperId);
                contactComponent2.IpAddress.Should().Be(contactComponent.IpAddress);

                var listMembershipComponent = cart.Components.OfType<ListMembershipsComponent>().FirstOrDefault();
                var listMembershipComponent2 = cart2.Components.OfType<ListMembershipsComponent>().FirstOrDefault();
                listMembershipComponent.Memberships.Count.Should().Be(listMembershipComponent2.Memberships.Count);

                // Add a product to the cart
                Proxy.DoCommand(AwShopperContext.ShopsContainer().AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));

                // Re-query the carts
                AwShopperContext.PolicyKeys = originalPolicyKeys;
                cart = GetCart(cartId, AwShopperContext.ShopsContainer());

                AwShopperContext.PolicyKeys = originalPolicyKeys + "|DoNotCalculateCart";
                cart2 = GetCart(cartId, AwShopperContext.ShopsContainer());

                cart.Should().NotBeNull();
                cart2.Should().NotBeNull();

                contactComponent = cart.Components.OfType<ContactComponent>().FirstOrDefault();
                contactComponent2 = cart2.Components.OfType<ContactComponent>().FirstOrDefault();

                contactComponent.Should().NotBeNull();
                contactComponent2.Should().NotBeNull();

                cart.Components.Count.Should().Be(2);
                cart2.Components.Count.Should().Be(2);

                cart.CreatedBy.Should().Be(cart2.CreatedBy);
                cart.DateCreated.Should().Be(cart2.DateCreated);
                cart.DateUpdated.Should().Be(cart2.DateUpdated);
                cart.DisplayName.Should().Be(cart2.DisplayName);
                cart.EntityVersion.Should().Be(cart2.EntityVersion);
                cart.ShopName.Should().Be(cart2.ShopName);
                cart.Version.Should().Be(cart2.Version);

                contactComponent2.Currency.Should().Be(contactComponent.Currency);
                contactComponent2.CustomerId.Should().Be(contactComponent.CustomerId);
                contactComponent2.Email.Should().Be(contactComponent.Email);
                contactComponent2.Name.Should().Be(contactComponent.Name);
                contactComponent2.ShopperId.Should().Be(contactComponent.ShopperId);
                contactComponent2.IpAddress.Should().Be(contactComponent.IpAddress);

                cart.Lines.Count.Should().Be(1);
                cart2.Lines.Count.Should().Be(1);

                var lineItem = cart.Lines[0];
                var lineItem2 = cart2.Lines[0];

                lineItem.Id.Should().Be(lineItem2.Id);
                lineItem.ItemId.Should().Be(lineItem2.ItemId);
                lineItem.Comments.Should().Be(lineItem2.Comments);
                lineItem.Name.Should().Be(lineItem2.Name);
                lineItem.ParentId.Should().Be(lineItem2.ParentId);
                lineItem.Quantity.Should().Be(lineItem2.Quantity);
                lineItem.UnitListPrice.Should().Be(lineItem2.UnitListPrice);
                lineItem.CartLineComponents.Count.Should().Be(lineItem2.CartLineComponents.Count);

                listMembershipComponent = cart.Components.OfType<ListMembershipsComponent>().FirstOrDefault();
                listMembershipComponent2 = cart2.Components.OfType<ListMembershipsComponent>().FirstOrDefault();
                listMembershipComponent.Memberships.Count.Should().Be(listMembershipComponent2.Memberships.Count);
            }
        }

        public static void AddToCart_GetCart_ProductAndBundle()
        {
            using (new SampleMethodScope())
            {
                var cameraId = "Habitat_Master|7042124|57042124";
                var wifiBundleId = "Habitat_Master|6001001|";
                var subLine1 = "Habitat_Master|6042964|56042964";
                var subLine2 = "Habitat_Master|6042971|56042971";

                // Add the camera product to cart
                var cartId = GenerateCartId();
                var command = (AddCartLineCommand) Proxy.DoCommand(HabitatShopsContainer.AddCartLine(cartId, cameraId, 1, UpdatedCartExpands));

                // Ensure Cart is returned in response and that it contains expected lines and components
                command.UpdatedCart.Should().NotBeNull();
                command.UpdatedCart.ItemCount.Should().Be(1);
                command.UpdatedCart.Lines.Count.Should().Be(1);
                command.UpdatedCart.Components.Should().Contain(c => c.GetType() == typeof(ContactComponent));
                command.UpdatedCart.Lines.Should().Contain(l => l.ItemId.Equals(cameraId));

                // Add wifi bundle to the cart
                var actionQuery = HabitatShopsContainer.AddCartLineWithSubLines(
                    cartId,
                    wifiBundleId,
                    1,
                    new List<CartSubLine>()
                    {
                        new CartSubLine()
                        {
                            ItemId = subLine1
                        },
                        new CartSubLine()
                        {
                            ItemId = subLine2
                        }
                    },
                    UpdatedCartExpands);

                command = (AddCartLineCommand) Proxy.DoCommand(actionQuery);

                // Ensure Cart is returned in response and that it contains expected lines and components
                command.UpdatedCart.Should().NotBeNull();
                command.UpdatedCart.ItemCount.Should().Be(2);
                command.UpdatedCart.Lines.Count.Should().Be(2);
                command.UpdatedCart.Components.Should().Contain(c => c.GetType() == typeof(ContactComponent));
                command.UpdatedCart.Lines.Should().Contain(l => l.ItemId.Equals(cameraId));
                command.UpdatedCart.Lines[1].CartSubLineComponents.Should().NotBeNull();
                command.UpdatedCart.Lines[1].CartSubLineComponents.Count.Should().Be(2);
            }
        }

        public static void RemoveLineFromCart_UpdatedCart()
        {
            using (new SampleMethodScope())
            {
                var cameraId = "Habitat_Master|7042124|57042124";
                var wifiBundleId = "Habitat_Master|6001001|";
                var subLine1 = "Habitat_Master|6042964|56042964";
                var subLine2 = "Habitat_Master|6042971|56042971";

                // Add a product and a bundle to the cart
                var cartId = GenerateCartId();
                Proxy.DoCommand(HabitatShopsContainer.AddCartLine(cartId, cameraId, 1));

                // Add wifi bundle to the cart
                var actionQuery = HabitatShopsContainer.AddCartLineWithSubLines(
                    cartId,
                    wifiBundleId,
                    1,
                    new List<CartSubLine>()
                    {
                        new CartSubLine()
                        {
                            ItemId = subLine1
                        },
                        new CartSubLine()
                        {
                            ItemId = subLine2
                        }
                    },
                    UpdatedCartExpands);

                var addCommand = (AddCartLineCommand) Proxy.DoCommand(actionQuery);

                // Ensure Cart is returned in response and that it contains expected lines and components
                addCommand.UpdatedCart.Should().NotBeNull();
                addCommand.UpdatedCart.ItemCount.Should().Be(2);
                addCommand.UpdatedCart.Lines.Count.Should().Be(2);
                addCommand.UpdatedCart.Components.Should().Contain(c => c.GetType() == typeof(ContactComponent));
                addCommand.UpdatedCart.Lines.Should().Contain(l => l.ItemId.Equals(cameraId));
                addCommand.UpdatedCart.Lines[1].CartSubLineComponents.Should().NotBeNull();
                addCommand.UpdatedCart.Lines[1].CartSubLineComponents.Count.Should().Be(2);

                var cameraLineId = addCommand.UpdatedCart.Lines[0].Id;
                var bundleLineId = addCommand.UpdatedCart.Lines[1].Id;

                var removeActionQuery = HabitatShopsContainer.RemoveCartLine(cartId, bundleLineId, UpdatedCartExpands);
                var removeCommand = (RemoveCartLineCommand) Proxy.DoCommand(removeActionQuery);

                removeCommand.UpdatedCart.Should().NotBeNull();
                removeCommand.UpdatedCart.ItemCount.Should().Be(1);
                removeCommand.UpdatedCart.Lines.Count.Should().Be(1);
                removeCommand.UpdatedCart.Components.Should().Contain(c => c.GetType() == typeof(ContactComponent));
                removeCommand.UpdatedCart.Lines.Should().Contain(l => l.ItemId.Equals(cameraId));

                removeActionQuery = HabitatShopsContainer.RemoveCartLine(cartId, cameraLineId, UpdatedCartExpands);
                removeCommand = (RemoveCartLineCommand) Proxy.DoCommand(removeActionQuery);

                removeCommand.UpdatedCart.Should().NotBeNull();
                removeCommand.UpdatedCart.ItemCount.Should().Be(0);
                removeCommand.UpdatedCart.Lines.Count.Should().Be(0);
                removeCommand.UpdatedCart.Components.Should().Contain(c => c.GetType() == typeof(ContactComponent));
            }
        }

        public static void UpdateCartLine_UpdatedCart()
        {
            using (new SampleMethodScope())
            {
                var cameraId = "Habitat_Master|7042124|57042124";

                // Add a camera to the cart
                var cartId = GenerateCartId();
                var actionQuery = HabitatShopsContainer.AddCartLineWithSubLines(
                    cartId,
                    cameraId,
                    1,
                    null,
                    UpdatedCartExpands);

                var addCommand = (AddCartLineCommand) Proxy.DoCommand(actionQuery);

                // Ensure Cart is returned in response and that it contains expected lines and components
                addCommand.UpdatedCart.Should().NotBeNull();
                addCommand.UpdatedCart.ItemCount.Should().Be(1);
                addCommand.UpdatedCart.Lines.Count.Should().Be(1);
                addCommand.UpdatedCart.Components.Should().Contain(c => c.GetType() == typeof(ContactComponent));
                addCommand.UpdatedCart.Lines.Should().Contain(l => l.ItemId.Equals(cameraId));

                var cameraLineId = addCommand.UpdatedCart.Lines[0].Id;

                var cameraLineComponent = addCommand.UpdatedCart.Lines.FirstOrDefault(l => l.Id.Equals(cameraLineId, StringComparison.OrdinalIgnoreCase));
                cameraLineComponent.Should().NotBeNull();
                cameraLineComponent.Quantity.Should().Be(1);

                var updateLineCommand = (UpdateCartLineCommand) Proxy.DoCommand(HabitatShopsContainer.UpdateCartLine(cartId, cameraLineId, 10, UpdatedCartExpands));
                updateLineCommand.UpdatedCart.Should().NotBeNull();
                updateLineCommand.UpdatedCart.ItemCount.Should().Be(1);
                updateLineCommand.UpdatedCart.Lines.Count.Should().Be(1);
                updateLineCommand.UpdatedCart.Components.Should().Contain(c => c.GetType() == typeof(ContactComponent));
                updateLineCommand.UpdatedCart.Lines.Should().Contain(l => l.ItemId.Equals(cameraId, StringComparison.OrdinalIgnoreCase));

                cameraLineComponent = updateLineCommand.UpdatedCart.Lines.FirstOrDefault(l => l.Id.Equals(cameraLineId, StringComparison.OrdinalIgnoreCase));
                cameraLineComponent.Should().NotBeNull();
                cameraLineComponent.Quantity.Should().Be(10);
            }
        }

        public static string GenerateCartId()
        {
            return $"ConsoleCart-{Guid.NewGuid():B}";
        }

        public static Cart GetCart(string cartId, ShopperContext context = null)
        {
            var container = context != null ? context.ShopsContainer() : AwShopsContainer;
            var cart = Proxy.GetValue(container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
            return cart;
        }

        public static Cart GetCart(string cartId, Container container = null)
        {
            if (container == null)
            {
                container = AwShopsContainer;
            }

            var cart = Proxy.GetValue(container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
            return cart;
        }

        public static Cart ValidateCart(Cart cart)
        {
            cart.Id.Should().NotBeNullOrEmpty();
            cart.Name.Should().NotBeNullOrEmpty();
            cart.ShopName.Should().NotBeNullOrEmpty();
            cart.ItemCount.Should().BeGreaterThan(0);
            cart.Totals.SubTotal.Amount.Should().BeGreaterThan(0);
            cart.Totals.GrandTotal.Amount.Should().BeGreaterThan(0);

            cart.Lines.Should().NotBeEmpty();
            foreach (var line in cart.Lines)
            {
                line.ItemId.Should().NotBeNullOrEmpty();
                line.Quantity.Should().NotBe(0);
                line.Totals.SubTotal.Amount.Should().BeGreaterThan(0);
                line.Totals.GrandTotal.Amount.Should().BeGreaterThan(0);

                // components
                line.CartLineComponents.Should().NotBeEmpty();
                line.CartLineComponents.OfType<MessagesComponent>().Should().HaveCount(1);
                var cartProductComponent = line.CartLineComponents.OfType<CartProductComponent>().FirstOrDefault();
                cartProductComponent.Should().NotBeNull();
                var itemAvailabilityComponent = line.CartLineComponents.OfType<ItemAvailabilityComponent>().FirstOrDefault();
                if (cartProductComponent.Policies.OfType<AvailabilityAlwaysPolicy>().Any())
                {
                    (itemAvailabilityComponent == null).Should().BeTrue("ItemAvailabilityComponent should be null.");
                }
                else
                {
                    itemAvailabilityComponent.Should().NotBeNull();
                    itemAvailabilityComponent?.ItemId.EqualsIgnoreCase(line.ItemId)
                        .Should().BeTrue($"ItemAvailability.ItemId does not match: Expected:{line.ItemId} Received:{itemAvailabilityComponent.ItemId}");
                }

                // pricing
                line.UnitListPrice.Amount.Should().BeGreaterThan(0);
                line.Policies.OfType<PurchaseOptionMoneyPolicy>().Should().HaveCount(1);
                var purchaseOptionMoneyPolicy = line.Policies.OfType<PurchaseOptionMoneyPolicy>().FirstOrDefault();
                purchaseOptionMoneyPolicy.Should().NotBeNull();
                purchaseOptionMoneyPolicy?.SellPrice.Amount.Should().BeGreaterThan(0);
            }

            cart.Components.Should().NotBeEmpty();
            cart.Components.OfType<ListMembershipsComponent>().Should().HaveCount(1);
            cart.Components.OfType<ContactComponent>().Should().HaveCount(1);

            return cart;
        }

        public static string AddCartLineWithVariant(string cartId)
        {
            var commandResult = Proxy.DoCommand(
                AwShopsContainer.AddCartLine(cartId, "Adventure Works Catalog|AW098 04|5", 1));

            return commandResult.Models.OfType<LineAdded>().FirstOrDefault()?.LineId;
        }

        public static string AddCartLineWithoutVariant(string cartId)
        {
            var commandResult = Proxy.DoCommand(
                AwShopsContainer.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));

            return commandResult.Models.OfType<LineAdded>().FirstOrDefault()?.LineId;
        }

        public static string AddCartLine(string cartId, string itemId, decimal quantity)
        {
            var commandResult = Proxy.DoCommand(
                AwShopsContainer.AddCartLine(cartId, itemId, quantity));

            return commandResult.Models.OfType<LineAdded>().FirstOrDefault()?.LineId;
        }

        public static string AddCartLineGiftCard(string cartId)
        {
            var commandResult = Proxy.DoCommand(
                AwShopsContainer.AddCartLine(cartId, "Adventure Works Catalog|22565422120|100", 1));

            return commandResult.Models.OfType<LineAdded>().FirstOrDefault()?.LineId;
        }

        public static string AddCartLineGiftCard50(string cartId)
        {
            var commandResult = Proxy.DoCommand(
                AwShopsContainer.AddCartLine(cartId, "Adventure Works Catalog|22565422120|050", 1));

            return commandResult.Models.OfType<LineAdded>().FirstOrDefault()?.LineId;
        }

        public static void UpdateCartLine(string cartId, string lineId)
        {
            Proxy.DoCommand(
                AwShopsContainer.UpdateCartLine(cartId, lineId, 10));
        }

        public static void DeleteCart(string cartId)
        {
            var cart = AwShopsContainer.Carts.Where(p => p.Id == cartId).SingleOrDefault();
            AwShopsContainer.DeleteObject(cart);
            AwShopsContainer.SaveChanges();
        }

        public static void RunPurgeCartsMinion()
        {
            EngineExtensions.RunMinionWithRetry(
                "Sitecore.Commerce.Plugin.Carts.PurgeCartsMinion, Sitecore.Commerce.Plugin.Carts",
                EnvironmentConstants.HabitatMinions,
                true);
            EngineExtensions.RunMinionWithRetry(
                "Sitecore.Commerce.Plugin.Carts.PurgeCartsMinion, Sitecore.Commerce.Plugin.Carts",
                EnvironmentConstants.AdventureWorksMinions,
                true);
        }

        private static void AddToCartDigital()
        {
            using (new SampleMethodScope())
            {
                var cartId = GenerateCartId();
                Proxy.DoCommand(HabitatShopsContainer.AddCartLine(cartId, "Habitat_Master|7042259|57042259", 2));

                var cart = GetCart(cartId, HabitatShopsContainer);
                ValidateCart(cart);
                cart.Totals.AdjustmentsTotal.Amount.Should().Be(0);
                cart.Totals.PaymentsTotal.Amount.Should().Be(0);

                cart.Lines.Count.Should().Be(2);
                foreach (var line in cart.Lines)
                {
                    line.CartLineComponents.OfType<ItemVariationSelectedComponent>().Should().HaveCount(1);
                    line.Totals.AdjustmentsTotal.Amount.Should().Be(0);
                    line.Totals.PaymentsTotal.Amount.Should().Be(0);
                }
            }
        }

        private static void AddToCartPhysicalAndDigital()
        {
            using (new SampleMethodScope())
            {
                var cartId = GenerateCartId();
                Proxy.DoCommand(HabitatShopsContainer.AddCartLine(cartId, "Habitat_Master|7042259|57042259", 1));
                Proxy.DoCommand(HabitatShopsContainer.AddCartLine(cartId, "Habitat_Master|6042567|56042568", 1));

                var cart = GetCart(cartId, HabitatShopsContainer);
                ValidateCart(cart);
                cart.Totals.AdjustmentsTotal.Amount.Should().Be(0);
                cart.Totals.PaymentsTotal.Amount.Should().Be(0);

                cart.Lines.Count.Should().Be(2);
                foreach (var line in cart.Lines)
                {
                    line.CartLineComponents.OfType<ItemVariationSelectedComponent>().Should().HaveCount(1);
                    line.Totals.AdjustmentsTotal.Amount.Should().Be(0);
                    line.Totals.PaymentsTotal.Amount.Should().Be(0);
                }
            }
        }

        private static void AddToCartPhysical()
        {
            using (new SampleMethodScope())
            {
                var cartId = GenerateCartId();
                Proxy.DoCommand(HabitatShopsContainer.AddCartLine(cartId, "Habitat_Master|6042567|56042568", 2));

                var cart = GetCart(cartId, HabitatShopsContainer);
                ValidateCart(cart);
                cart.Totals.AdjustmentsTotal.Amount.Should().Be(0);
                cart.Totals.PaymentsTotal.Amount.Should().Be(0);

                cart.Lines.Count.Should().Be(1);
                foreach (var line in cart.Lines)
                {
                    line.CartLineComponents.OfType<ItemVariationSelectedComponent>().Should().HaveCount(1);
                    line.Totals.AdjustmentsTotal.Amount.Should().Be(0);
                    line.Totals.PaymentsTotal.Amount.Should().Be(0);
                }
            }
        }

        private static void MergeCarts_BothHasLines()
        {
            using (new SampleMethodScope())
            {
                var fromCart = Proxy.GetValue(AwShopsContainer.Carts.ByKey("consolemergecart1"));
                AddCartLine(fromCart.Id, "Adventure Works Catalog|AW098 04|5", 1);
                fromCart = GetCart(fromCart.Id, AwShopsContainer);

                var toCart = Proxy.GetValue(AwShopsContainer.Carts.ByKey("consolemergecart2"));
                AddCartLine(toCart.Id, "Adventure Works Catalog|AW475 14|", 1);

                var commandResult = Proxy.DoCommand(AwShopsContainer.MergeCarts(fromCart.Id, toCart.Id));
                var model = commandResult.Models.OfType<PersistedEntityModel>().FirstOrDefault();
                model.Should().NotBeNull();
                var mergedCartId = model.EntityId;
                var mergedCart = GetCart(mergedCartId, AwShopsContainer);
                mergedCart.Should().NotBeNull();
                mergedCart.Lines.Should().NotBeEmpty();
                mergedCart.Lines.Count.Should().Be(2);
                fromCart.Lines.ToList()
                    .ForEach(
                        fl =>
                        {
                            mergedCart.Lines
                                .FirstOrDefault(ml => ml.ItemId.Equals(fl.ItemId) && ml.Quantity == fl.Quantity)
                                .Should()
                                .NotBeNull();
                        });
                mergedCart.Components.Should().NotBeEmpty();

                DeleteCart(mergedCart.Id);
            }
        }

        private static void MergeCarts_LinesRollup()
        {
            using (new SampleMethodScope())
            {
                var fromCart = Proxy.GetValue(AwShopsContainer.Carts.ByKey("consolemergecartlinesrollup1"));
                AddCartLine(fromCart.Id, "Adventure Works Catalog|AW098 04|5", 1);
                fromCart = GetCart(fromCart.Id, AwShopsContainer);

                var toCart = Proxy.GetValue(AwShopsContainer.Carts.ByKey("consolemergecartlinesrollup2"));
                AddCartLine(toCart.Id, "Adventure Works Catalog|AW098 04|5", 1);

                var commandResult = Proxy.DoCommand(AwShopsContainer.MergeCarts(fromCart.Id, toCart.Id));
                var model = commandResult.Models.OfType<PersistedEntityModel>().FirstOrDefault();
                model.Should().NotBeNull();
                var mergedCartId = model.EntityId;
                var mergedCart = GetCart(mergedCartId, AwShopsContainer);
                mergedCart.Should().NotBeNull();
                mergedCart.Lines.Should().NotBeEmpty();
                mergedCart.Lines.Count.Should().Be(1);
                mergedCart.Lines.ToList()
                    .ForEach(
                        ml =>
                        {
                            fromCart.Lines
                                .FirstOrDefault(fl => fl.ItemId.Equals(ml.ItemId) && ml.Quantity == fl.Quantity * 2)
                                .Should()
                                .NotBeNull();
                        });
                mergedCart.Components.Should().NotBeEmpty();

                DeleteCart(mergedCart.Id);
            }
        }

        private static void MergeCarts_FromWithComponents()
        {
            using (new SampleMethodScope())
            {
                var fromCart = Proxy.GetValue(AwShopsContainer.Carts.ByKey("consolemergecartfromwithcomponents1"));
                AddCartLine(fromCart.Id, "Adventure Works Catalog|AW098 04|5", 1);
                Coupons.AddCouponToCart(fromCart.Id, "RTRNC15P");
                fromCart = GetCart(fromCart.Id, AwShopsContainer);

                var toCart = Proxy.GetValue(AwShopsContainer.Carts.ByKey("consolemergecartfromwithcomponents2"));
                AddCartLine(toCart.Id, "Adventure Works Catalog|AW475 14|", 1);

                var commandResult = Proxy.DoCommand(AwShopsContainer.MergeCarts(fromCart.Id, toCart.Id));
                var model = commandResult.Models.OfType<PersistedEntityModel>().FirstOrDefault();
                model.Should().NotBeNull();
                var mergedCartId = model.EntityId;
                var mergedCart = GetCart(mergedCartId, AwShopsContainer);
                mergedCart.Should().NotBeNull();
                mergedCart.Lines.Should().NotBeEmpty();
                mergedCart.Components.Should().NotBeEmpty();
                fromCart.Components.ToList()
                    .ForEach(
                        fc =>
                        {
                            mergedCart.Components.ToList()
                                .FirstOrDefault(mc => mc.Id.Equals(fc.Id))
                                .Should()
                                .NotBeNull();
                        });

                DeleteCart(mergedCart.Id);
            }
        }

        private static void MergeCarts_ToWithComponents()
        {
            using (new SampleMethodScope())
            {
                var fromCart = Proxy.GetValue(AwShopsContainer.Carts.ByKey("consolemergecarttowithcomponents1"));
                AddCartLine(fromCart.Id, "Adventure Works Catalog|AW098 04|5", 1);
                fromCart = GetCart(fromCart.Id, AwShopsContainer);

                var toCart = Proxy.GetValue(AwShopsContainer.Carts.ByKey("consolemergecarttowithcomponents2"));
                AddCartLine(toCart.Id, "Adventure Works Catalog|AW475 14|", 1);
                Coupons.AddCouponToCart(toCart.Id, "RTRNC15P");

                var commandResult = Proxy.DoCommand(AwShopsContainer.MergeCarts(fromCart.Id, toCart.Id));
                var model = commandResult.Models.OfType<PersistedEntityModel>().FirstOrDefault();
                model.Should().NotBeNull();
                var mergedCartId = model.EntityId;
                var mergedCart = GetCart(mergedCartId, AwShopsContainer);
                mergedCart.Should().NotBeNull();
                mergedCart.Lines.Should().NotBeEmpty();
                mergedCart.Components.Should().NotBeEmpty();
                toCart.Components.ToList()
                    .ForEach(
                        fc =>
                        {
                            mergedCart.Components.ToList()
                                .FirstOrDefault(mc => mc.Id.Equals(fc.Id))
                                .Should()
                                .NotBeNull();
                        });
                mergedCart.Components.ToList().OfType<CartCouponsComponent>().FirstOrDefault().Should().NotBeNull();

                DeleteCart(mergedCart.Id);
            }
        }

        private static void GetCart_DoNotCalculate()
        {
            using (new SampleMethodScope())
            {
                // Get the original list price of the product
                var sellableItem = Proxy.GetValue(
                    AwShopperContext.ShopsContainer().SellableItems.ByKey("Adventure Works Catalog,AW475 14,")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));

                sellableItem.Should().NotBeNull();

                // Save the original list price
                var originalListPrice = sellableItem.ListPrice;

                // Get a cart and add a product to cart that uses list pricing
                var cartId = GenerateCartId();
                Proxy.DoCommand(AwShopperContext.ShopsContainer().AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));

                // Add policy to cart to so that other factors are not considered when deciding to calculate cart
                var calculateCartPolicy = CalculateCartPolicy.CreateCalculateCartPolicy(0, true);
                var result = Proxy.DoCommand(
                    AwShopperContext.ShopsContainer().AddPolicy(
                        cartId,
                        "Sitecore.Commerce.Plugin.Carts.CalculateCartPolicy, Sitecore.Commerce.Plugin.Carts",
                        calculateCartPolicy));

                // Get the cart - cart total will be calculated
                var cart = GetCart(cartId, AwShopperContext.ShopsContainer());
                var originalGrandTotal = cart.Totals.GrandTotal;

                // Change the list price of the product
                var updatedListPrice = Money.CreateMoney(0M);
                updatedListPrice.CurrencyCode = "USD";

                result = Proxy.DoCommand(
                    AwShopperContext.ShopsContainer().UpdateListPrices(
                        "Adventure Works Catalog|AW475 14|",
                        new List<Money>
                        {
                            updatedListPrice
                        }));
                result.Should().NotBeNull();
                result.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase)).Should().BeFalse();

                sellableItem = Proxy.GetValue(
                    AwShopperContext.ShopsContainer().SellableItems.ByKey("Adventure Works Catalog,AW475 14,")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                sellableItem.ListPrice.Amount.Should().Be(updatedListPrice.Amount);

                // Get the cart again - specify do not calculate
                var originalPolicyKeys = AwShopperContext.PolicyKeys;
                AwShopperContext.PolicyKeys = originalPolicyKeys + "|DoNotCalculateCart";
                cart = GetCart(cartId, AwShopperContext.ShopsContainer());

                // Assert that the cart totals have not changed despite change of product list price
                cart.Totals.GrandTotal.Amount.Should().Be(originalGrandTotal.Amount);
                cart.Totals.GrandTotal.CurrencyCode.Should().Be(originalGrandTotal.CurrencyCode);

                // Get the cart normally - cart totals should now be updated to reflect price change of product
                AwShopperContext.PolicyKeys = originalPolicyKeys;
                cart = GetCart(cartId, AwShopperContext.ShopsContainer());

                // Assert that the cart totals now reflect the updated list price of the product
                cart.Totals.GrandTotal.Amount.Should().NotBe(originalGrandTotal.Amount);
                cart.Totals.GrandTotal.Amount.Should().Be(0);

                // Clean-up - reset product to original list price
                result = Proxy.DoCommand(
                    AwShopperContext.ShopsContainer().UpdateListPrices(
                        "Adventure Works Catalog|AW475 14|",
                        new List<Money>
                        {
                            originalListPrice
                        }));
                result.Should().NotBeNull();
                result.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase)).Should().BeFalse();
            }
        }
    }
}
