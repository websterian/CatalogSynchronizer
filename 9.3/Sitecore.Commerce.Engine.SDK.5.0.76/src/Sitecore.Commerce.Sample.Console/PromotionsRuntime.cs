using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class PromotionsRuntime
    {
        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Promotions Runtime"))
            {
                ApplyingOrderOfPromotionsAndCouponPromotions();

                ApplyingCartAndLineExclusivePromotions();

                ApplyingCartExclusiveMixedPromotions();
                ApplyingCartExclusivePromotions();
                ApplyingCartExclusiveCouponsPromotions();

                ApplyingLineExclusiveMixedPromotions();
                ApplyingLineExclusiveCouponsPromotions();
                ApplyingLinePromotions();

                PromotionCalculationLinePercentAndAmount();
                PromotionCalculationLineAmountAndPercent();
                PromotionCalculationCartAllApply();
                PromotionCalculationLineAllApply();

                DisabledPromotions();

                FixedPrice();
            }
        }

        private static void FixedPrice()
        {
            var bob = new AnonymousCustomerBob
            {
                Context =
                {
                    Environment = EnvironmentConstants.AdventureWorksShops
                }
            };
            var container = bob.Context.ShopsContainer();
            var cartId = Carts.GenerateCartId();

            var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW055 01|33", 1));
            result.Messages.Should().NotContainErrors();
            result.Models.OfType<Totals>().FirstOrDefault()?.GrandTotal.Amount.Should().Be(9);

            var cart = Proxy.GetValue(container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
            cart.Lines.FirstOrDefault()?.Policies.OfType<PurchaseOptionMoneyPolicy>().FirstOrDefault()?.SellPrice.Amount.Should().Be(9);
            cart.Lines.FirstOrDefault()?.Policies.OfType<PurchaseOptionMoneyPolicy>().FirstOrDefault()?.FixedSellPrice.Should().BeTrue();

            result = Proxy.DoCommand(container.AddCouponToCart(cartId, "AWSELLPRICE"));
            result.Messages.Should().NotContainErrors();
            result.Models.OfType<Totals>().FirstOrDefault()?.GrandTotal.Amount.Should().Be(5);

            cart = Proxy.GetValue(container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
            cart.Lines.FirstOrDefault()?.Policies.OfType<PurchaseOptionMoneyPolicy>().FirstOrDefault()?.SellPrice.Amount.Should().Be(5);
            cart.Lines.FirstOrDefault()?.Policies.OfType<PurchaseOptionMoneyPolicy>().FirstOrDefault()?.FixedSellPrice.Should().BeTrue();

            result = Proxy.DoCommand(container.RemoveCouponFromCart(cartId, "AWSELLPRICE"));
            result.Messages.Should().NotContainErrors();
            result.Models.OfType<Totals>().FirstOrDefault()?.GrandTotal.Amount.Should().Be(9);

            cart = Proxy.GetValue(container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
            cart.Lines.FirstOrDefault()?.Policies.OfType<PurchaseOptionMoneyPolicy>().FirstOrDefault()?.SellPrice.Amount.Should().Be(9);
            cart.Lines.FirstOrDefault()?.Policies.OfType<PurchaseOptionMoneyPolicy>().FirstOrDefault()?.FixedSellPrice.Should().BeTrue();
        }

        private static void ApplyingOrderOfPromotionsAndCouponPromotions()
        {
            using (new SampleMethodScope())
            {
                var bob = new AnonymousCustomerBob();
                var container = bob.Context.ShopsContainer();
                var cartId = Carts.GenerateCartId();

                // add to cart scoutpride
                var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 4));
                result.Messages.Should().NotContainErrors();

                var cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeFalse();

                // add coupon 15% off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNC15P"));
                result.Messages.Should().NotContainErrors();

                cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeTrue();

                // add coupon 10% off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNC10P"));
                result.Messages.Should().NotContainErrors();

                cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeTrue();

                result = Proxy.DoCommand(
                    container.SetCartFulfillment(
                        cartId,
                        bob.Context.Components.OfType<PhysicalFulfillmentComponent>().First()));
                result.Messages.Should().NotContainErrors();

                cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeTrue();
                var messages = cart.Components.OfType<MessagesComponent>().FirstOrDefault();
                messages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                messages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(3);
                messages?.Messages.ToList()
                    .FindIndex(
                        m => m.Text.Equals(
                            "PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-CartFreeShippingPromotion"))
                    .Should()
                    .Be(0);
                messages?.Messages.ToList()
                    .FindIndex(
                        m => m.Text.Equals(
                            "PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-Cart15PctOffCouponPromotion"))
                    .Should()
                    .Be(1);
                messages?.Messages.ToList()
                    .FindIndex(
                        m => m.Text.Equals(
                            "PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-Cart10PctOffCouponPromotion"))
                    .Should()
                    .Be(2);
            }
        }

        private static void ApplyingCartAndLineExclusivePromotions()
        {
            using (new SampleMethodScope())
            {
                var bob = new AnonymousCustomerBob();
                var container = bob.Context.ShopsContainer();
                var cartId = Carts.GenerateCartId();

                // add to cart scoutpride
                var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));
                result.Messages.Should().NotContainErrors();

                // add coupon cart exclusive 5% off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNEC5P"));
                result.Messages.Should().NotContainErrors();

                // add coupon line exclusive 20% off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNEL20P"));
                result.Messages.Should().NotContainErrors();

                var cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeTrue();
                var cartMessages = cart.Components.OfType<MessagesComponent>().FirstOrDefault();
                cartMessages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                cartMessages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(1);
                cartMessages?.Messages.FirstOrDefault()
                    ?.Text.Should()
                    .Be(
                        "PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-Cart5PctOffExclusiveCouponPromotion");
                var lineMessages = cart.Lines.FirstOrDefault()
                    ?.CartLineComponents.OfType<MessagesComponent>()
                    .FirstOrDefault();
                lineMessages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                lineMessages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(1);
                lineMessages?.Messages.LastOrDefault(m => m.Code.Equals("Promotions"))
                    ?.Text.Should()
                    .Be(
                        "PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-Line20PctOffExclusiveCouponPromotion");
            }
        }

        private static void ApplyingCartExclusiveMixedPromotions()
        {
            using (new SampleMethodScope())
            {
                var bob = new AnonymousCustomerBob();
                var container = bob.Context.ShopsContainer();
                var cartId = Carts.GenerateCartId();

                // add to cart galaxy
                var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW535 11|12", 1));
                result.Messages.Should().NotContainErrors();

                var cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeTrue();
                var messages = cart.Components.OfType<MessagesComponent>().FirstOrDefault();
                messages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                messages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(1);
                messages?.Messages.FirstOrDefault()
                    ?.Text.Should()
                    .Be(
                        "PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-CartGalaxyTentExclusivePromotion");

                // add coupon cart exclusive 5$ off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNEC5A"));
                result.Messages.Should().NotContainErrors();

                cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeTrue();
                messages = cart.Components.OfType<MessagesComponent>().FirstOrDefault();
                messages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                messages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(2);
                messages?.Messages.FirstOrDefault(m => m.Code.Equals("Promotions"))
                    ?.Text.Should()
                    .Be(
                        "PromotionExcluded: Entity-Promotion-AdventureWorksPromotionBook-Cart5OffExclusiveCouponPromotion");
                messages?.Messages.LastOrDefault(m => m.Code.Equals("Promotions"))
                    ?.Text.Should()
                    .Be(
                        "PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-CartGalaxyTentExclusivePromotion");
            }
        }

        private static void ApplyingCartExclusivePromotions()
        {
            using (new SampleMethodScope())
            {
                var bob = new AnonymousCustomerBob();
                var container = bob.Context.ShopsContainer();
                var cartId = Carts.GenerateCartId();

                // add to cart galaxy
                var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW535 11|12", 1));
                result.Messages.Should().NotContainErrors();

                var cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeTrue();
                var messages = cart.Components.OfType<MessagesComponent>().FirstOrDefault();
                messages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                messages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(1);
                messages?.Messages.FirstOrDefault()
                    ?.Text.Should()
                    .Be(
                        "PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-CartGalaxyTentExclusivePromotion");

                // add fulfillment
                result = Proxy.DoCommand(
                    container.SetCartFulfillment(
                        cartId,
                        bob.Context.Components.OfType<PhysicalFulfillmentComponent>().First()));
                result.Messages.Should().NotContainErrors();

                cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeTrue();
                messages = cart.Components.OfType<MessagesComponent>().FirstOrDefault();
                messages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                messages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(2);
                messages?.Messages.FirstOrDefault(m => m.Code.Equals("Promotions"))
                    ?.Text.Should()
                    .Be("PromotionExcluded: Entity-Promotion-AdventureWorksPromotionBook-CartFreeShippingPromotion");
                messages?.Messages.LastOrDefault(m => m.Code.Equals("Promotions"))
                    ?.Text.Should()
                    .Be(
                        "PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-CartGalaxyTentExclusivePromotion");
            }
        }

        private static void ApplyingCartExclusiveCouponsPromotions()
        {
            using (new SampleMethodScope())
            {
                var bob = new AnonymousCustomerBob();
                var container = bob.Context.ShopsContainer();
                var cartId = Carts.GenerateCartId();

                // add to cart scoutpride
                var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 4));
                result.Messages.Should().NotContainErrors();

                // add coupon cart exclusive 5% off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNEC5P"));
                result.Messages.Should().NotContainErrors();

                // add coupon cart exclusive 5% off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNEC5A"));
                result.Messages.Should().NotContainErrors();

                var cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeTrue();
                var messages = cart.Components.OfType<MessagesComponent>().FirstOrDefault();
                messages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                messages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(2);
                messages?.Messages.FirstOrDefault(m => m.Code.Equals("Promotions"))
                    ?.Text.Should()
                    .Be(
                        "PromotionExcluded: Entity-Promotion-AdventureWorksPromotionBook-Cart5OffExclusiveCouponPromotion");
                messages?.Messages.LastOrDefault(m => m.Code.Equals("Promotions"))
                    ?.Text.Should()
                    .Be(
                        "PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-Cart5PctOffExclusiveCouponPromotion");
            }
        }

        private static void PromotionCalculationCartAllApply()
        {
            using (new SampleMethodScope())
            {
                var bob = new AnonymousCustomerBob();
                var container = bob.Context.ShopsContainer();
                var cartId = Carts.GenerateCartId();

                // add to cart scoutpride
                var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));
                result.Messages.Should().NotContainErrors();

                // add to cart petzl spirit
                result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW014 08|", 1));
                result.Messages.Should().NotContainErrors();

                // add coupon cart 10$ off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNC10A"));
                result.Messages.Should().NotContainErrors();

                // add coupon cart 10% off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNC10P"));
                result.Messages.Should().NotContainErrors();

                var cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                var cartTotals = cart.Totals;
                cartTotals?.SubTotal.Amount.Should().Be(69M);
                cartTotals?.AdjustmentsTotal.Amount.Should().Be(-15.90M);
                cartTotals?.GrandTotal.Amount.Should().Be(53.10M);
                var cartPercentAdjustmentTotal =
                    cart.Adjustments.FirstOrDefault(a => a.AwardingBlock.Equals("CartSubtotalAmountOffAction"));
                cartPercentAdjustmentTotal.Should().NotBeNull();
                cartPercentAdjustmentTotal?.Adjustment.Amount.Should().Be(-10M);
                var cartAmountAdjustment =
                    cart.Adjustments.FirstOrDefault(a => a.AwardingBlock.Equals("CartSubtotalPercentOffAction"));
                cartAmountAdjustment.Should().NotBeNull();
                cartAmountAdjustment?.Adjustment.Amount.Should().Be(-5.90M);
            }
        }

        private static void DisabledPromotions()
        {
            using (new SampleMethodScope())
            {
                var bob = new AnonymousCustomerBob();
                var container = bob.Context.ShopsContainer();
                var cartId = Carts.GenerateCartId();

                // add to cart scoutpride
                var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 1));
                result.Messages.Should().NotContainErrors();

                var cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeFalse();
            }
        }

        private static void ApplyingLineExclusiveMixedPromotions()
        {
            using (new SampleMethodScope())
            {
                var bob = new AnonymousCustomerBob();
                var container = bob.Context.ShopsContainer();
                var cartId = Carts.GenerateCartId();

                // add to cart alpine
                var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW188 06|19", 1));
                result.Messages.Should().NotContainErrors();

                var cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeFalse();
                cart.Lines.FirstOrDefault().Should().NotBeNull();
                var lineMessages = cart.Lines.FirstOrDefault()
                    ?.CartLineComponents.OfType<MessagesComponent>()
                    .FirstOrDefault();
                lineMessages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                lineMessages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(1);
                lineMessages?.Messages.FirstOrDefault(m => m.Code.Equals("Promotions"))
                    ?.Text.Should()
                    .Be(
                        "PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-LineAlpineParkaExclusivePromotion");

                // add coupon line exclusive 20% off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNEL20P"));
                result.Messages.Should().NotContainErrors();

                cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeTrue();
                var cartMessages = cart.Components.OfType<MessagesComponent>().FirstOrDefault();
                cartMessages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                cartMessages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(1);
                cartMessages?.Messages.FirstOrDefault()
                    ?.Text.Should()
                    .Be(
                        "PromotionExcluded: Entity-Promotion-AdventureWorksPromotionBook-LineAlpineParkaExclusivePromotion");
                lineMessages = cart.Lines.FirstOrDefault()
                    ?.CartLineComponents.OfType<MessagesComponent>()
                    .FirstOrDefault();
                lineMessages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                lineMessages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(1);
                lineMessages?.Messages.LastOrDefault(m => m.Code.Equals("Promotions"))
                    ?.Text.Should()
                    .Be(
                        "PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-Line20PctOffExclusiveCouponPromotion");
            }
        }

        private static void ApplyingLineExclusiveCouponsPromotions()
        {
            using (new SampleMethodScope())
            {
                var bob = new AnonymousCustomerBob();
                var container = bob.Context.ShopsContainer();
                var cartId = Carts.GenerateCartId();

                // add to cart scoutpride
                var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 4));
                result.Messages.Should().NotContainErrors();

                // add coupon line exclusive 20% off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNEL20P"));
                result.Messages.Should().NotContainErrors();

                // add coupon line exclusive 20$ off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNEL20A"));
                result.Messages.Should().NotContainErrors();

                var cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                cart.Components.OfType<MessagesComponent>().Any().Should().BeTrue();
                var cartMessages = cart.Components.OfType<MessagesComponent>().FirstOrDefault();
                cartMessages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                cartMessages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(1);
                cartMessages?.Messages.FirstOrDefault()
                    ?.Text.Should()
                    .Be(
                        "PromotionExcluded: Entity-Promotion-AdventureWorksPromotionBook-Line20OffExclusiveCouponPromotion");
                var lineMessages = cart.Lines.FirstOrDefault()
                    ?.CartLineComponents.OfType<MessagesComponent>()
                    .FirstOrDefault();
                lineMessages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                lineMessages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(1);
                lineMessages?.Messages.LastOrDefault(m => m.Code.Equals("Promotions"))
                    ?.Text.Should()
                    .Be(
                        "PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-Line20PctOffExclusiveCouponPromotion");
            }
        }

        private static void ApplyingLinePromotions()
        {
            using (new SampleMethodScope())
            {
                var bob = new AnonymousCustomerBob();
                var container = bob.Context.ShopsContainer();
                var cartId = Carts.GenerateCartId();

                // add to cart sahara
                var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW114 06|23", 1));
                result.Messages.Should().NotContainErrors();

                var cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                var lineTotals = cart.Lines.FirstOrDefault()?.Totals;
                lineTotals?.SubTotal.Amount.Should().Be(120M);
                lineTotals?.AdjustmentsTotal.Amount.Should().Be(-62.5M);
                lineTotals?.GrandTotal.Amount.Should().Be(57.5M);
                var sahara5Off = cart.Lines.FirstOrDefault()
                    ?.Adjustments.FirstOrDefault(a => a.AwardingBlock.Equals("CartItemSubtotalAmountOffAction"));
                sahara5Off.Should().NotBeNull();
                sahara5Off?.Adjustment.Amount.Should().Be(-5M);
                var sahara50PctOff = cart.Lines.FirstOrDefault()
                    ?.Adjustments.FirstOrDefault(a => a.AwardingBlock.Equals("CartItemSubtotalPercentOffAction"));
                sahara50PctOff.Should().NotBeNull();
                sahara50PctOff?.Adjustment.Amount.Should().Be(-57.5M);
                var lineMessages = cart.Lines.FirstOrDefault()
                    ?.CartLineComponents.OfType<MessagesComponent>()
                    .FirstOrDefault();
                lineMessages?.Messages.Any(m => m.Code.Equals("Promotions")).Should().BeTrue();
                lineMessages?.Messages.Count(m => m.Code.Equals("Promotions")).Should().Be(2);
                lineMessages?.Messages.FirstOrDefault(m => m.Code.Equals("Promotions"))
                    ?.Text.Should()
                    .Be("PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-LineSaharaJacket5OffPromotion");
                lineMessages?.Messages.LastOrDefault(m => m.Code.Equals("Promotions"))
                    ?.Text.Should()
                    .Be("PromotionApplied: Entity-Promotion-AdventureWorksPromotionBook-LineSaharaJacketPromotion");
            }
        }

        private static void PromotionCalculationLinePercentAndAmount()
        {
            using (new SampleMethodScope())
            {
                var bob = new AnonymousCustomerBob();
                var container = bob.Context.ShopsContainer();
                var cartId = Carts.GenerateCartId();

                // add to cart scoutpride
                var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 2));
                result.Messages.Should().NotContainErrors();

                // add coupon line 5% off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNL5P"));
                result.Messages.Should().NotContainErrors();

                // add coupon line 5$ off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNL5A"));
                result.Messages.Should().NotContainErrors();

                var cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                var lineTotals = cart.Lines.FirstOrDefault()?.Totals;
                lineTotals?.SubTotal.Amount.Should().Be(118M);
                lineTotals?.AdjustmentsTotal.Amount.Should().Be(-10.90M);
                lineTotals?.GrandTotal.Amount.Should().Be(107.10M);
                var linePercentAdjustment = cart.Lines.FirstOrDefault()
                    ?.Adjustments.FirstOrDefault(a => a.AwardingBlock.Equals("CartAnyItemSubtotalPercentOffAction"));
                linePercentAdjustment.Should().NotBeNull();
                linePercentAdjustment?.Adjustment.Amount.Should().Be(-5.90M);
                var lineAmountAdjustment = cart.Lines.FirstOrDefault()
                    ?.Adjustments.FirstOrDefault(a => a.AwardingBlock.Equals("CartAnyItemSubtotalAmountOffAction"));
                lineAmountAdjustment.Should().NotBeNull();
                lineAmountAdjustment?.Adjustment.Amount.Should().Be(-5M);
            }
        }

        private static void PromotionCalculationLineAmountAndPercent()
        {
            using (new SampleMethodScope())
            {
                var bob = new AnonymousCustomerBob();
                var container = bob.Context.ShopsContainer();
                var cartId = Carts.GenerateCartId();

                // add to cart scoutpride
                var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW475 14|", 2));
                result.Messages.Should().NotContainErrors();

                // add coupon line 5$ off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNL5A"));
                result.Messages.Should().NotContainErrors();

                // add coupon line 5% off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNL5P"));
                result.Messages.Should().NotContainErrors();

                var cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                var lineTotals = cart.Lines.FirstOrDefault()?.Totals;
                lineTotals?.SubTotal.Amount.Should().Be(118M);
                lineTotals?.AdjustmentsTotal.Amount.Should().Be(-10.65M);
                lineTotals?.GrandTotal.Amount.Should().Be(107.35M);
                var linePercentAdjustment = cart.Lines.FirstOrDefault()
                    ?.Adjustments.FirstOrDefault(a => a.AwardingBlock.Equals("CartAnyItemSubtotalPercentOffAction"));
                linePercentAdjustment.Should().NotBeNull();
                linePercentAdjustment?.Adjustment.Amount.Should().Be(-5.65M);
                var lineAmountAdjustment = cart.Lines.FirstOrDefault()
                    ?.Adjustments.FirstOrDefault(a => a.AwardingBlock.Equals("CartAnyItemSubtotalAmountOffAction"));
                lineAmountAdjustment.Should().NotBeNull();
                lineAmountAdjustment?.Adjustment.Amount.Should().Be(-5M);
            }
        }

        private static void PromotionCalculationLineAllApply()
        {
            using (new SampleMethodScope())
            {
                var bob = new AnonymousCustomerBob();
                var container = bob.Context.ShopsContainer();
                var cartId = Carts.GenerateCartId();

                // add to cart petzl spirit
                var result = Proxy.DoCommand(container.AddCartLine(cartId, "Adventure Works Catalog|AW014 08|", 1));
                result.Messages.Should().NotContainErrors();

                // add coupon line 5$ off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNL5A"));
                result.Messages.Should().NotContainErrors();

                // add coupon line 5% off
                result = Proxy.DoCommand(container.AddCouponToCart(cartId, "RTRNL5P"));
                result.Messages.Should().NotContainErrors();

                var cart = Proxy.GetValue(
                    container.Carts.ByKey(cartId).Expand("Lines($expand=CartLineComponents),Components"));
                cart.Should().NotBeNull();
                var lineTotals = cart.Lines.FirstOrDefault()?.Totals;
                lineTotals?.SubTotal.Amount.Should().Be(10M);
                lineTotals?.AdjustmentsTotal.Amount.Should().Be(-5.25M);
                lineTotals?.GrandTotal.Amount.Should().Be(4.75M);
                var linePercentAdjustment = cart.Lines.FirstOrDefault()
                    ?.Adjustments.FirstOrDefault(a => a.AwardingBlock.Equals("CartAnyItemSubtotalPercentOffAction"));
                linePercentAdjustment.Should().NotBeNull();
                linePercentAdjustment?.Adjustment.Amount.Should().Be(-0.25M);
                var lineAmountAdjustment = cart.Lines.FirstOrDefault()
                    ?.Adjustments.FirstOrDefault(a => a.AwardingBlock.Equals("CartAnyItemSubtotalAmountOffAction"));
                lineAmountAdjustment.Should().NotBeNull();
                lineAmountAdjustment?.Adjustment.Amount.Should().Be(-5M);
            }
        }
    }
}
