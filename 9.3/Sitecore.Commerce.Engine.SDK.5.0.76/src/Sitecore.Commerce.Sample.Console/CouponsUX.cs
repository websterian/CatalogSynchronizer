using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Coupons;
using Sitecore.Commerce.Plugin.Promotions;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class CouponsUX
    {
        private static readonly Container ShopsContainer = new CsrSheila().Context.ShopsContainer();
        private static string _bookId;
        private static Guid _bookUId;
        private static string _promotionId;
        private static Guid _promotionUId;
        private static string _publicCouponId;
        private static string _privateCouponGroupId;
        private static Guid _privateCouponGroupUId;
        private static Promotion _promotion;
        private static string _partial;

        private static string _couponCode;
        private static string _couponPrefix;
        private static string _couponSuffix;

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Coupons UX"))
            {
                _partial = $"{Guid.NewGuid()}:N".Substring(0, 3);
                AddPromotionBook();
                AddPromotion();

                PublicCouponsView();
                PrivateCouponsView();

                _couponCode = $"PublicCoupon_{_partial}";
                _couponPrefix = $"{_partial}before_";
                _couponSuffix = $"_after{_partial}";

                AddPublicCoupon();
                AddPrivateCoupon();

                PublicCouponsViewCheck();
                PrivateCouponsViewCheck();

                NewAllocation();

                PublicCouponsViewCheck();
                PrivateCouponsViewCheck();

                // error scenarios
                AddPublicCouponAgain();
                AddPrivateCouponAgain();
                AllocateTooMuch();

                PublicCouponsViewCheck();
                PrivateCouponsViewCheck();
            }
        }

        private static void AddPromotionBook()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        string.Empty,
                        "Details",
                        "AddPromotionBook",
                        string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = $"ConsoleCoupons{_partial}"
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = "displayname"
                    },
                    new ViewProperty
                    {
                        Name = "Description",
                        Value = "description"
                    }
                };
                var result = Proxy.DoCommand(ShopsContainer.DoAction(view));
                result.Messages.Should().NotContainMessageCode("error");
                var persistedModel = result.Models.OfType<PersistedEntityModel>().FirstOrDefault();
                persistedModel.Should().NotBeNull();
                _bookId = persistedModel.EntityId;
                _bookUId = persistedModel.EntityUniqueId;
            }
        }

        private static void AddPromotion()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _bookId,
                        "Details",
                        "AddPromotion",
                        string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));

                var fromDate = DateTimeOffset.Now;
                var toDate = DateTimeOffset.Now.AddDays(30);
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = "ConsoleCoupons"
                    },
                    new ViewProperty
                    {
                        Name = "Description",
                        Value = "promotion's description"
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = "promotion's display name"
                    },
                    new ViewProperty
                    {
                        Name = "DisplayText",
                        Value = "promotion's text"
                    },
                    new ViewProperty
                    {
                        Name = "DisplayCartText",
                        Value = "promotion's cart text"
                    },
                    new ViewProperty
                    {
                        Name = "ValidFrom",
                        Value = fromDate.ToString(CultureInfo.InvariantCulture)
                    },
                    new ViewProperty
                    {
                        Name = "ValidTo",
                        Value = toDate.ToString(CultureInfo.InvariantCulture)
                    },
                    new ViewProperty
                    {
                        Name = "IsExclusive",
                        Value = "true"
                    },
                    version
                };
                var result = Proxy.DoCommand(ShopsContainer.DoAction(view));
                result.Messages.Should().NotContainMessageCode("error");
                result.Models.OfType<PromotionAdded>().FirstOrDefault().Should().NotBeNull();
                var persistedModel = result.Models.OfType<PersistedEntityModel>().FirstOrDefault(m => m.Name.Equals("Sitecore.Commerce.Plugin.Promotions.Promotion"));
                persistedModel.Should().NotBeNull();
                _promotionId = persistedModel.EntityId;
                _promotionUId = persistedModel.EntityUniqueId;

                var promotionFriendlyId = result.Models.OfType<PromotionAdded>().FirstOrDefault()?.PromotionFriendlyId;
                _promotion = Proxy.GetValue(
                    ShopsContainer.Promotions.ByKey(promotionFriendlyId).Expand("Components"));
                _promotion.Should().NotBeNull();
                _promotion.ValidFrom.Should().BeCloseTo(fromDate, 1000);
                _promotion.ValidTo.Should().BeCloseTo(toDate, 1000);
            }
        }

        private static void PublicCouponsView()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _promotionId,
                        "PublicCoupons",
                        string.Empty,
                        string.Empty));
                result.Should().NotBeNull();
                result.Policies.Should().NotBeEmpty();
                result.Properties.Should().NotBeEmpty();
                result.ChildViews.Should().BeEmpty();
            }
        }

        private static void PrivateCouponsView()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _promotionId,
                        "PrivateCoupons",
                        string.Empty,
                        string.Empty));
                result.Should().NotBeNull();
                result.Policies.Should().NotBeEmpty();
                result.Properties.Should().NotBeEmpty();
                result.ChildViews.Should().BeEmpty();
            }
        }

        private static void PublicCouponsViewCheck()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _promotionId,
                        "PublicCoupons",
                        string.Empty,
                        string.Empty));
                result.Should().NotBeNull();
                result.Policies.Should().NotBeEmpty();
                result.Properties.Should().NotBeEmpty();
                result.ChildViews.Should().NotBeEmpty();
                result.ChildViews.Count.Should().Be(1);
                result.Name.Should().Be("PublicCoupons");
                result.DisplayName.Should().Be("Public Coupons");
                result.UiHint.Should().Be("Table");
                result.Policies.Count.Should().Be(1);
                result.Properties.Count.Should().Be(1);
                result.ChildViews[0].Should().BeOfType(typeof(EntityView));

                var couponDetails = result.ChildViews[0] as EntityView;
                couponDetails.Should().NotBeNull();
                couponDetails.ChildViews?.Count.Should().Be(0);
                couponDetails.Action.Should().BeNullOrEmpty();
                couponDetails.DisplayName.Should().Be("Coupon Details");
                couponDetails.Name.Should().Be("CouponDetails");
                couponDetails.Properties.Count.Should().Be(2);
            }
        }

        private static void PrivateCouponsViewCheck()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _promotionId,
                        "PrivateCoupons",
                        string.Empty,
                        string.Empty));
                result.Should().NotBeNull();
                result.Policies.Should().NotBeEmpty();
                result.Properties.Should().NotBeEmpty();
                result.ChildViews.Should().NotBeEmpty();
                result.Name.Should().Be("PrivateCoupons");
                result.DisplayName.Should().Be("Private Coupons");
                result.UiHint.Should().Be("Table");
                result.ChildViews.Count.Should().Be(1);
                result.Policies.Count.Should().Be(1);
                result.Properties.Count.Should().Be(1);
                result.ChildViews[0].Should().BeOfType(typeof(EntityView));

                var couponDetails = result.ChildViews[0] as EntityView;
                couponDetails.Should().NotBeNull();
                couponDetails.ChildViews.Count.Should().Be(0);
                couponDetails.Action.Should().BeNullOrEmpty();
                couponDetails.DisplayName.Should().Be("Coupon Details");
                couponDetails.Name.Should().Be("CouponDetails");
                couponDetails.Properties.Count.Should().Be(4);
            }
        }

        private static void AddPublicCoupon()
        {
            using (new SampleMethodScope())
            {
                var addView = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _promotionId,
                        "PublicCoupons",
                        "AddPublicCoupon",
                        string.Empty));
                addView.Should().NotBeNull();
                addView.Policies.Should().BeEmpty();
                addView.Properties.Should().NotBeEmpty();
                addView.Properties.Count.Should().Be(2); // Version & Code
                addView.ChildViews.Should().BeEmpty();
                var version = addView.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                addView.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Code",
                        Value = _couponCode
                    },
                    version
                };

                var addAction = Proxy.DoCommand(ShopsContainer.DoAction(addView));
                addAction.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeFalse();
                addAction.Models.OfType<PublicCouponAdded>().FirstOrDefault().Should().NotBeNull();
                _publicCouponId = addAction.Models.OfType<PublicCouponAdded>().FirstOrDefault()?.CouponFriendlyId;
                _publicCouponId.Should().NotBeNullOrEmpty();
                var message = addAction.Models.OfType<PublicCouponAdded>().FirstOrDefault()?.Name;
                ConsoleExtensions.WriteColoredLine(ConsoleColor.Green, $"Created public coupon code: {message}");
            }
        }

        private static void AddPublicCouponAgain()
        {
            using (new SampleMethodScope())
            {
                var addView = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _promotionId,
                        "PublicCoupons",
                        "AddPublicCoupon",
                        string.Empty));
                addView.Should().NotBeNull();
                addView.Policies.Should().BeEmpty();
                addView.Properties.Should().NotBeEmpty();
                addView.Properties.Count.Should().Be(2); // Version & Code
                addView.ChildViews.Should().BeEmpty();
                var version = addView.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                addView.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Code",
                        Value = _couponCode
                    },
                    version
                };

                var addAction = Proxy.DoCommand(ShopsContainer.DoAction(addView));
                ConsoleExtensions.WriteExpectedError();
                addAction.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
                addAction.Models.OfType<PublicCouponAdded>().FirstOrDefault().Should().BeNull();
            }
        }

        private static void AddPrivateCoupon()
        {
            using (new SampleMethodScope())
            {
                var addView = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _promotionId,
                        "CouponDetails",
                        "AddPrivateCoupon",
                        string.Empty));
                addView.Should().NotBeNull();
                addView.Policies.Should().BeEmpty();
                addView.Properties.Should().NotBeEmpty();
                addView.Properties.Count.Should().Be(4); // Version, Prefix, Suffix & Total
                addView.ChildViews.Should().BeEmpty();
                var version = addView.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                addView.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Prefix",
                        Value = _couponPrefix
                    },
                    new ViewProperty
                    {
                        Name = "Suffix",
                        Value = _couponSuffix
                    },
                    new ViewProperty
                    {
                        Name = "Total",
                        Value = "20"
                    },
                    version
                };

                var addAction = Proxy.DoCommand(ShopsContainer.DoAction(addView));
                addAction.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeFalse();
                var persistedModel = addAction.Models.OfType<PersistedEntityModel>()
                    .FirstOrDefault(m => m.Name.Equals("Sitecore.Commerce.Plugin.Coupons.PrivateCouponGroup"));
                persistedModel.Should().NotBeNull();
                _privateCouponGroupId = persistedModel.EntityId;
                _privateCouponGroupUId = persistedModel.EntityUniqueId;

                var message = addAction.Messages.FirstOrDefault(
                    m => m.Code.Equals("information", StringComparison.OrdinalIgnoreCase));
                message.Should().NotBeNull();

                ConsoleExtensions.WriteColoredLine(ConsoleColor.Green, message.Text);
            }
        }

        private static void AddPrivateCouponAgain()
        {
            using (new SampleMethodScope())
            {
                var addView = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _promotionId,
                        "CouponDetails",
                        "AddPrivateCoupon",
                        string.Empty));
                addView.Should().NotBeNull();
                addView.Policies.Should().BeEmpty();
                addView.Properties.Should().NotBeEmpty();
                addView.Properties.Count.Should().Be(4); // Version, Prefix, Suffix & Total
                addView.ChildViews.Should().BeEmpty();
                var version = addView.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                addView.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Prefix",
                        Value = _couponPrefix
                    },
                    new ViewProperty
                    {
                        Name = "Suffix",
                        Value = _couponSuffix
                    },
                    new ViewProperty
                    {
                        Name = "Total",
                        Value = "20"
                    },
                    version
                };

                var addAction = Proxy.DoCommand(ShopsContainer.DoAction(addView));
                ConsoleExtensions.WriteWarningLine("Expecting private group exists error");
                addAction.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
                addAction.Models.OfType<PrivateCouponGroupAdded>().FirstOrDefault().Should().BeNull();
            }
        }

        private static void NewAllocation()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _privateCouponGroupId,
                        "AllocationDetails",
                        "NewAllocation",
                        string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.Properties.Count.Should().Be(2); // Version & Count
                view.ChildViews.Should().BeEmpty();
                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Count",
                        Value = "15"
                    },
                    version
                };

                var newAllocationAction = Proxy.DoCommand(ShopsContainer.DoAction(view));
                newAllocationAction.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeFalse();
                newAllocationAction.Models.OfType<PrivateCouponList>().FirstOrDefault().Should().NotBeNull();
                var privateCouponList = newAllocationAction.Models.OfType<PrivateCouponList>().FirstOrDefault();
                privateCouponList.Should().NotBeNull();
                privateCouponList.GroupFriendlyId.Should().NotBeNullOrEmpty();

                ConsoleExtensions.WriteColoredLine(ConsoleColor.Green, "Allocated the following coupon codes");
                foreach (var code in privateCouponList.CouponCodes)
                {
                    System.Console.WriteLine(code);
                }
            }
        }

        private static void AllocateTooMuch()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _privateCouponGroupId,
                        "AllocationDetails",
                        "NewAllocation",
                        string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.Properties.Count.Should().Be(2); // Version & Count
                view.ChildViews.Should().BeEmpty();
                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"))?.Value;
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Count",
                        Value = "15"
                    },
                    new ViewProperty
                    {
                        Name = "Version",
                        Value = version
                    },
                };

                var newAllocationAction = Proxy.DoCommand(ShopsContainer.DoAction(view));
                newAllocationAction.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
                ConsoleExtensions.WriteExpectedError();
                newAllocationAction.Models.OfType<PrivateCouponList>().FirstOrDefault().Should().BeNull();
            }
        }
    }
}
