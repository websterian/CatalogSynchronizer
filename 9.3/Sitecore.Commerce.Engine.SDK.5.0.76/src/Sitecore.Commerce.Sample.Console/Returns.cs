using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Commerce.Plugin.Payments;
using Sitecore.Commerce.Plugin.Returns;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.Sample.Scenarios;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Returns
    {
        private static ShopperContext _context;

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Returns"))
            {
                var steve = new AnonymousCustomerSteve();
                _context = steve.Context;

                var order = CreateCompletedOrder();
                var lineId = order.Lines.FirstOrDefault()?.Id;
                RequestRmaLineValidation(order.Id, "invalidlineid", 1); // invalid line id
                RequestRmaLineValidation(order.Id, lineId, -1); // returning -1 out of 1 available
                RequestRmaLineValidation(order.Id, lineId, 0); // returning 0 out of 1 available
                var rmaFriendlyId = RequestRma(order.Id, lineId, 1);
                RequestRmaLineValidation(order.Id, lineId, 3); // returning 3 out of 2 available

                ReturnedItemReceivedValidation("invalidrmaid"); // invalid rma id
                ReturnedItemReceived(_context.ShopsContainer(), rmaFriendlyId, order.Id, lineId);
                ReturnedItemReceivedValidation(rmaFriendlyId); // rma invalid status

                RunRefundRmasMinion(_context.ShopsContainer(), $"Entity-ReturnMerchandiseAuthorization-{rmaFriendlyId}", EnvironmentConstants.HabitatMinions);

                rmaFriendlyId = RequestRma(order.Id, lineId, 2);

                RequestRmaLineValidation(order.Id, order.Lines.FirstOrDefault()?.Id, 1); // returning 1 out of 0 available
                ReturnedItemReceived(_context.ShopsContainer(), rmaFriendlyId, order.Id, lineId);

                RequestDigitalRma();
            }
        }

        public static void ReturnedItemReceived(
            Container container,
            string rmaFriendlyId,
            string orderId,
            string lineId)
        {
            var result = Proxy.DoCommand(container.ReturnedItemReceived(rmaFriendlyId));
            result.Should().NotBeNull();
            result.Messages.Should().NotContainErrors();
            var rmaId = result.Models.OfType<PersistedEntityModel>().FirstOrDefault(m => m.Name.Equals(typeof(ReturnMerchandiseAuthorization).FullName))?.EntityId;
            rmaId.Should().NotBeNullOrEmpty();

            var rma = GetRma(container, rmaId);
            rma.Status.Should().Be("RefundPending");
            rma.ItemsReturnedDate.Should().BeCloseTo(DateTimeOffset.UtcNow, 5000);

            var order = Orders.GetOrder(container, orderId);
            order.Should().NotBeNull();
            order.Components.OfType<OrderRmasComponent>().FirstOrDefault().Should().NotBeNull();
            order.Components.OfType<OrderRmasComponent>().FirstOrDefault()?.Returns.Should().NotBeEmpty();
            order.Lines.FirstOrDefault(l => l.Id.Equals(lineId)).Should().NotBeNull();
            order.Lines.FirstOrDefault(l => l.Id.Equals(lineId))
                ?.CartLineComponents.OfType<ItemReturnedComponent>()
                .Any()
                .Should()
                .BeTrue();
            order.Lines.FirstOrDefault(l => l.Id.Equals(lineId))
                ?.CartLineComponents.OfType<ItemReturnedComponent>()
                .FirstOrDefault()
                ?.Returns.Should()
                .NotBeEmpty();
            order.Lines.FirstOrDefault(l => l.Id.Equals(lineId))
                ?.CartLineComponents.OfType<ItemReturnedComponent>()
                .FirstOrDefault()
                ?.Returns.Count.Should()
                .Be(order.Components.OfType<OrderRmasComponent>().FirstOrDefault()?.Returns.Count);
            order.Lines.FirstOrDefault(l => l.Id.Equals(lineId))
                ?.CartLineComponents.OfType<ItemReturnedComponent>()
                .FirstOrDefault()
                ?.Returns.OrderByDescending(r => r.ReturnedDate)
                .FirstOrDefault()
                .Should()
                .NotBeNull();
            order.Lines.FirstOrDefault(l => l.Id.Equals(lineId))
                ?.CartLineComponents.OfType<ItemReturnedComponent>()
                .FirstOrDefault()
                ?.Returns.OrderByDescending(r => r.ReturnedDate)
                .FirstOrDefault()
                ?.Quantity.Should()
                .Be(rma.Lines.FirstOrDefault()?.Quantity);
            order.Lines.FirstOrDefault(l => l.Id.Equals(lineId))
                ?.CartLineComponents.OfType<ItemReturnedComponent>()
                .FirstOrDefault()
                ?.Returns.OrderByDescending(r => r.ReturnedDate)
                .FirstOrDefault()
                ?.ReturnedDate.Should()
                .BeCloseTo(DateTimeOffset.UtcNow, 5000);
        }

        public static void RunRefundRmasMinion(Container container, string rmaId, string environmentName = "AdventureWorksMinions")
        {
            EngineExtensions.RunMinionWithRetry(
                "Sitecore.Commerce.Plugin.Returns.RefundRmasMinion, Sitecore.Commerce.Plugin.Returns",
                environmentName);

            var rma = GetRma(container, rmaId);
            rma.Status.Should().Be("Completed");
        }

        private static void RequestRmaLineValidation(string orderId, string lineId, decimal quantity)
        {
            using (new SampleMethodScope())
            {
                var result =
                    Proxy.DoCommand(
                        _context.ShopsContainer()
                            .RequestRma(
                                orderId,
                                "ConsoleWrongItem",
                                new List<RmaLineComponent>
                                {
                                    new RmaLineComponent
                                    {
                                        LineId = lineId,
                                        Quantity = quantity
                                    }
                                }));
                result.Should().NotBeNull();
                result.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
                result.Models.OfType<RmaAdded>().Any().Should().BeFalse();
                ConsoleExtensions.WriteExpectedError();
            }
        }

        private static string RequestRma(string orderId, string lineId, decimal quantity)
        {
            using (new SampleMethodScope())
            {
                var result =
                    Proxy.DoCommand(
                        _context.ShopsContainer()
                            .RequestRma(
                                orderId,
                                "ConsoleWrongItem",
                                lines: new List<RmaLineComponent>
                                {
                                    new RmaLineComponent
                                    {
                                        LineId = lineId,
                                        Quantity = quantity
                                    }
                                }));
                result.Should().NotBeNull();
                result.Messages.Should().NotContainErrors();
                var rmaId = result.Models.OfType<PersistedEntityModel>().FirstOrDefault(m => m.Name.Equals(typeof(ReturnMerchandiseAuthorization).FullName))?.EntityId;
                rmaId.Should().NotBeNullOrEmpty();
                var rmaFriendlyId = result.Models.OfType<PersistedEntityModel>().FirstOrDefault(m => m.Name.Equals(typeof(ReturnMerchandiseAuthorization).FullName))?.EntityFriendlyId;
                rmaFriendlyId.Should().NotBeNullOrEmpty();

                var rma = GetRma(_context.ShopsContainer(), rmaId);
                rma.Status.Should().Be("Pending");
                rma.Lines.Should().Contain(l => l.LineId.Equals(lineId));
                rma.Order.EntityTarget.Should().Be(orderId);
                rma.ItemsReturnedDate.Should().Be(DateTimeOffset.MinValue);
                rma.RefundPaymentId.Should().NotBeEmpty();

                var order = Orders.GetOrder(_context.ShopsContainer(), orderId);
                order.Should().NotBeNull();
                order.Components.OfType<OrderRmasComponent>().FirstOrDefault().Should().NotBeNull();
                order.Components.OfType<OrderRmasComponent>().FirstOrDefault()?.Returns.Should().NotBeEmpty();
                order.Components.OfType<OrderRmasComponent>()
                    .FirstOrDefault()
                    ?.Returns.FirstOrDefault(r => r.Rma.EntityTarget.Equals(rma.Id))
                    .Should()
                    .NotBeNull();
                order.Components.OfType<OrderRmasComponent>()
                    .FirstOrDefault()
                    ?.Returns.FirstOrDefault(r => r.Rma.EntityTarget.Equals(rma.Id))
                    ?.Lines.Should()
                    .NotBeEmpty();
                order.Components.OfType<OrderRmasComponent>()
                    .FirstOrDefault()
                    ?.Returns.FirstOrDefault(r => r.Rma.EntityTarget.Equals(rma.Id))
                    ?.Lines.Count.Should()
                    .Be(rma.Lines.Count);
                order.Components.OfType<OrderRmasComponent>()
                    .FirstOrDefault()
                    ?.Returns.FirstOrDefault(r => r.Rma.EntityTarget.Equals(rma.Id))
                    ?.Lines.Should()
                    .Contain(rma.Lines.Select(l => l.LineId));

                return rmaFriendlyId;
            }
        }

        private static ReturnMerchandiseAuthorization GetRma(Container container, string rmaId)
        {
            var result = Proxy.GetValue(container.ReturnMerchandiseAuthorizations.ByKey(rmaId).Expand("Lines($expand=ChildComponents),Components"));
            result.Should().NotBeNull();
            result.Status.Should().NotBeNullOrEmpty();
            result.Lines.Should().NotBeEmpty();
            result.Order.EntityTarget.Should().NotBeNullOrEmpty();
            result.ReturnReason.Should().NotBeNullOrEmpty();

            var result2 = Proxy.GetValue(container.ReturnMerchandiseAuthorizations.ByKey($"{result.UniqueId}").Expand("Lines($expand=ChildComponents),Components"));
            result2.Should().NotBeNull();

            return result;
        }

        private static void ReturnedItemReceivedValidation(string rmaFriendlyId)
        {
            var result = Proxy.DoCommand(_context.ShopsContainer().ReturnedItemReceived(rmaFriendlyId));
            result.Should().NotBeNull();
            result.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase) || m.Code.Equals("validationerror", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
            ConsoleExtensions.WriteExpectedError();
        }

        private static Order CreateCompletedOrder()
        {
            var orderId = Buy1ItemMultipleQuantity.Run(_context);
            orderId.Should().NotBeNullOrEmpty();

            var order = Orders.GetOrder(_context.ShopsContainer(), orderId);
            order.Should().NotBeNull();
            order.Lines.Should().NotBeEmpty();
            order.Lines.FirstOrDefault().Should().NotBeNull();

            var lineId = order.Lines.FirstOrDefault()?.Id;
            lineId.Should().NotBeNullOrEmpty();

            var paymentId = order.Components.OfType<PaymentComponent>().FirstOrDefault()?.Id;
            paymentId.Should().NotBeNullOrEmpty();

            Orders.RunPendingOrdersMinion(_context);
            Orders.RunReleasedOrdersMinion(_context);

            return order;
        }

        private static void RequestDigitalRma()
        {
            using (new SampleMethodScope())
            {
                var order = CreateDigitalCompletedOrder();
                var orderId = order.Id;
                var lineId = order.Lines.FirstOrDefault()?.Id;

                var result =
                    Proxy.DoCommand(
                        _context.ShopsContainer()
                            .RequestRma(
                                orderId,
                                "ConsoleWrongItem",
                                new List<RmaLineComponent>
                                {
                                    new RmaLineComponent
                                    {
                                        LineId = lineId,
                                        Quantity = 1
                                    }
                                }));

                result.Should().NotBeNull();
                result.Messages.Should().NotContainErrors();
                var rmaId = result.Models.OfType<PersistedEntityModel>().FirstOrDefault(m => m.Name.Equals(typeof(ReturnMerchandiseAuthorization).FullName))?.EntityId;
                rmaId.Should().NotBeNullOrEmpty();
                var rmaFriendlyId = result.Models.OfType<PersistedEntityModel>().FirstOrDefault(m => m.Name.Equals(typeof(ReturnMerchandiseAuthorization).FullName))?.EntityFriendlyId;
                rmaFriendlyId.Should().NotBeNullOrEmpty();

                var rma = GetRma(_context.ShopsContainer(), rmaId);
                rma.Status.Should().Be("RefundPending");
                rma.Lines.Should().Contain(l => l.LineId.Equals(lineId));
                rma.Order.EntityTarget.Should().Be(orderId);
                rma.ItemsReturnedDate.Should().NotBe(DateTimeOffset.MinValue);

                order = Orders.GetOrder(_context.ShopsContainer(), orderId);
                order.Should().NotBeNull();
                order.Components.OfType<OrderRmasComponent>().FirstOrDefault().Should().NotBeNull();
                order.Components.OfType<OrderRmasComponent>().FirstOrDefault()?.Returns.Should().NotBeEmpty();
                order.Components.OfType<OrderRmasComponent>()
                    .FirstOrDefault()
                    ?.Returns.FirstOrDefault(r => r.Rma.EntityTarget.Equals(rma.Id))
                    .Should()
                    .NotBeNull();
                order.Components.OfType<OrderRmasComponent>()
                    .FirstOrDefault()
                    ?.Returns.FirstOrDefault(r => r.Rma.EntityTarget.Equals(rma.Id))
                    ?.Lines.Should()
                    .NotBeEmpty();
                order.Components.OfType<OrderRmasComponent>()
                    .FirstOrDefault()
                    ?.Returns.FirstOrDefault(r => r.Rma.EntityTarget.Equals(rma.Id))
                    ?.Lines.Count.Should()
                    .Be(rma.Lines.Count);
                order.Components.OfType<OrderRmasComponent>()
                    .FirstOrDefault()
                    ?.Returns.FirstOrDefault(r => r.Rma.EntityTarget.Equals(rma.Id))
                    ?.Lines.Should()
                    .Contain(rma.Lines.Select(l => l.LineId));
            }
        }

        private static Order CreateDigitalCompletedOrder()
        {
            var orderId = BuyWarranty.Run(_context, 2);
            orderId.Should().NotBeNullOrEmpty();

            var order = Orders.GetOrder(_context.ShopsContainer(), orderId);
            order.Should().NotBeNull();

            Orders.RunPendingOrdersMinion(_context);
            Orders.RunReleasedOrdersMinion(_context);

            return order;
        }
    }
}
