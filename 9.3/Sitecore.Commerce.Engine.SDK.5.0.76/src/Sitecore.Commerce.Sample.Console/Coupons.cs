using FluentAssertions;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Coupons;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Coupons
    {
        private static readonly Container ShopsContainer = new AnonymousCustomerJeff().Context.ShopsContainer();

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Coupons"))
            {
                GetCoupon();
                GetPrivateCouponGroup();
            }
        }

        public static void AddCouponToCart(string cartId, string coupon)
        {
            var commandResult = Proxy.DoCommand(ShopsContainer.AddCouponToCart(cartId, coupon));

            if (commandResult.ResponseCode != "Ok" && coupon != "InvalidCoupon")
            {
                System.Console.WriteLine($"AddCouponToCart_Fail:{commandResult.ResponseCode}");
            }
        }

        public static Coupon GetCoupon(string couponFriendlyId = "")
        {
            using (new SampleMethodScope())
            {
                var friendlyId = string.IsNullOrEmpty(couponFriendlyId)
                    ? "RTRNEC5P"
                    : couponFriendlyId;

                var result = Proxy.GetValue(ShopsContainer.Coupons.ByKey(friendlyId).Expand("Components"));
                result.Should().NotBeNull();
                result.Components.Should().NotBeEmpty();

                return result;
            }
        }

        private static void GetPrivateCouponGroup(string groupFriendlyId = "")
        {
            using (new SampleMethodScope())
            {
                var friendlyId = string.IsNullOrEmpty(groupFriendlyId)
                    ? "SPCP_-_22"
                    : groupFriendlyId;

                var result = Proxy.GetValue(ShopsContainer.PrivateCouponGroups.ByKey(friendlyId).Expand("Components"));
                result.Should().NotBeNull();
                result.AllocatedCount.Should().Be(0);
                result.Description.Should().Be("Sample Private Coupon Promotion");
                result.DisplayName.Should().Be("Sample Private Coupon Promotion");
                result.Name.Should().Be("SamplePrivateCouponPromotion");
                result.Prefix.Should().Be("SPCP_");
                result.Suffix.Should().Be("_22");
                result.Total.Should().Be(15);
            }
        }
    }
}
