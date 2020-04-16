using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using Microsoft.OData.Client;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Availability;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Inventory;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class SellableItems
    {
        private static readonly Container Container = new CsrSheila().Context.ShopsContainer();

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("SellableItems"))
            {
                GetSellableItemsByParent();
                GetSellableItemsByParent_Page();
                GetSellableItemsByParent_InvalidParent();

                GetSellableItem_NoVariants();
                GetSellableItem_WithVariants();
                GetSellableItem_WithVariant();
                GetBulkPrices();
                UpdateListPrices_SellableItem();
                UpdateListPrices_Variation();
                RemoveListPrices_SellableItem();
                RemoveListPrices_Variation();

                SellableItemPricing_BackInTime();
                SellableItemPricing_ForwardInTime();

                GetSellableItemAllLanguages();
                GetSellableItemsAllLanguages();

                GetSellableItemsSummary();
            }
        }

        private static void GetSellableItemsByParent()
        {
            using (new SampleMethodScope())
            {
                var container = new AnonymousCustomerJeff(EnvironmentConstants.HabitatShops).Context.ShopsContainer();

                var result = Proxy.Execute(container.GetSellableItemsByParent("Entity-Category-Habitat_Master-FeaturedProducts", 0, 100));

                result.Should().NotBeNull();
                result.Should().HaveCount(11);
            }
        }

        private static void GetSellableItemsByParent_Page()
        {
            using (new SampleMethodScope())
            {
                var container = new AnonymousCustomerJeff(EnvironmentConstants.HabitatShops).Context.ShopsContainer();

                var result = Proxy.Execute(container.GetSellableItemsByParent("Entity-Category-Habitat_Master-FeaturedProducts", 0, 5));

                result.Should().NotBeNull();
                result.Should().HaveCount(5);
            }
        }

        private static void GetSellableItemsByParent_InvalidParent()
        {
            using (new SampleMethodScope())
            {
                var container = new AnonymousCustomerJeff(EnvironmentConstants.HabitatShops).Context.ShopsContainer();

                try
                {
                    var result = Proxy.Execute(container.GetSellableItemsByParent("InvalidParent", 0, 100));
                    ConsoleExtensions.WriteExpectedError();
                }
                catch (DataServiceQueryException ex)
                {
                    ex.Response.StatusCode.Should().Be((int) HttpStatusCode.NotFound);
                }
            }
        }

        private static void GetSellableItemsSummary()
        {
            using (new SampleMethodScope())
            {
                var container = new AnonymousCustomerJeff(EnvironmentConstants.HabitatShops).Context.ShopsContainer();

                var result = Proxy.Execute(container.GetSellableItemsSummary(new List<string>
                {
                    "Habitat_Master|6001001|",
                    "Habitat_Master|6042134|",
                    "Habitat_Master|6042134|56042137"
                }, true));

                var sellableItemSummaries = result as SellableItemSummary[] ?? result.ToArray();
                sellableItemSummaries.Should().NotBeNull();
                sellableItemSummaries.Should().NotBeEmpty();
                foreach (var summary in sellableItemSummaries)
                {
                    summary.ItemId.Should().NotBeNullOrEmpty();
                    summary.Summaries.Should().NotBeEmpty();
                    summary.Summaries.OfType<SellableItemPricing>().Any().Should().BeTrue();
                    summary.Summaries.OfType<ConnectItemAvailability>().Any().Should().BeTrue();
                }
            }
        }

        private static void GetSellableItem_NoVariants()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    Container.SellableItems.ByKey("Adventure Works Catalog,AW014 08,")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                result.Should().NotBeNull();

                result.Policies.OfType<PriceCardPolicy>().First().PriceCardName.Should().Be("AdventureWorksPriceCard");
                result.Policies.OfType<ListPricingPolicy>().First().Prices.First().Amount.Should().Be(14);
                result.Policies.OfType<PurchaseOptionMoneyPolicy>().First().SellPrice.Amount.Should().Be(10.0M);

                result.Components.OfType<CatalogsComponent>()
                    .First()
                    .ChildComponents.OfType<CatalogComponent>()
                    .First()
                    .Name.Should()
                    .Be("Adventure Works Catalog");
                result.Components.OfType<ImagesComponent>()
                    .First()
                    .Images.First()
                    .Should()
                    .Be("c4885306-5e14-4b99-9e9a-03351f277631");
                result.Components.OfType<PriceSnapshotComponent>().First().Tiers.First().Price.Should().Be(10.0M);
                result.Components.OfType<ItemAvailabilityComponent>().First().IsAvailable.Should().Be(true);
                result.Components.OfType<ItemAvailabilityComponent>()
                    .First()
                    .ItemId.Should()
                    .Be("Adventure Works Catalog|AW014 08|");
            }
        }

        private static void GetSellableItem_WithVariants()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    Container.SellableItems.ByKey("Adventure Works Catalog,AW055 01,")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                result.Should().NotBeNull();

                result.Policies.OfType<PriceCardPolicy>().First().PriceCardName.Should().Be("AdventureWorksPriceCard");
                result.Policies.OfType<ListPricingPolicy>().First().Prices.First().Amount.Should().Be(58);
                result.Policies.OfType<PurchaseOptionMoneyPolicy>().First().SellPrice.Amount.Should().Be(10.0M);

                result.Components.OfType<CatalogsComponent>()
                    .First()
                    .ChildComponents.OfType<CatalogComponent>()
                    .First()
                    .Name.Should()
                    .Be("Adventure Works Catalog");
                result.Components.OfType<ImagesComponent>()
                    .First()
                    .Images.First()
                    .Should()
                    .Be("f9e25bba-4020-4306-bc13-1819d18b7390");
                result.Components.OfType<PriceSnapshotComponent>().First().Tiers.First().Price.Should().Be(10.0M);

                result.Components.OfType<ItemVariationsComponent>().First().ChildComponents.Should().NotBeEmpty();
                result.Components.OfType<ItemVariationsComponent>().First().ChildComponents.Count.Should().Be(6);
                result.Components.OfType<ItemVariationsComponent>()
                    .First()
                    .ChildComponents.Cast<ItemVariationComponent>()
                    .ToList()
                    .ForEach(
                        v =>
                        {
                            v.Policies.OfType<PriceCardPolicy>().Should().NotBeNull();
                            v.Policies.OfType<ListPricingPolicy>().Should().NotBeNull();
                            v.Policies.OfType<PurchaseOptionMoneyPolicy>().Should().NotBeNull();

                            v.ChildComponents.OfType<ImagesComponent>().Should().NotBeNull();
                            v.ChildComponents.OfType<PriceSnapshotComponent>().Should().NotBeNull();
                            v.ChildComponents.OfType<ItemAvailabilityComponent>().Should().NotBeNull();
                        });
            }
        }

        private static void GetSellableItem_WithVariant()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    Container.SellableItems.ByKey("Adventure Works Catalog,AW055 01,33")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                result.Should().NotBeNull();

                result.Policies.OfType<PriceCardPolicy>().First().PriceCardName.Should().Be("AdventureWorksPriceCard");
                result.Policies.OfType<ListPricingPolicy>().First().Prices.First().Amount.Should().Be(58);
                result.Policies.OfType<PurchaseOptionMoneyPolicy>().First().SellPrice.Amount.Should().Be(10.0M);

                result.Components.OfType<CatalogsComponent>()
                    .First()
                    .ChildComponents.OfType<CatalogComponent>()
                    .First()
                    .Name.Should()
                    .Be("Adventure Works Catalog");
                result.Components.OfType<ImagesComponent>()
                    .First()
                    .Images.First()
                    .Should()
                    .Be("f9e25bba-4020-4306-bc13-1819d18b7390");
                result.Components.OfType<PriceSnapshotComponent>().First().Tiers.First().Price.Should().Be(10.0M);

                result.Components.OfType<ItemVariationsComponent>().First().ChildComponents.Should().NotBeEmpty();
                result.Components.OfType<ItemVariationsComponent>().First().ChildComponents.Count.Should().Be(1);
                result.Components.OfType<ItemVariationsComponent>()
                    .First()
                    .ChildComponents.Cast<ItemVariationComponent>()
                    .ToList()
                    .ForEach(
                        v =>
                        {
                            v.Policies.OfType<PriceCardPolicy>().Should().NotBeNull();
                            v.Policies.OfType<ListPricingPolicy>().Should().NotBeNull();
                            v.Policies.OfType<PurchaseOptionMoneyPolicy>().Should().NotBeNull();
                            v.ChildComponents.OfType<PriceSnapshotComponent>().Should().NotBeNull();
                            v.ChildComponents.OfType<ItemAvailabilityComponent>().Should().NotBeNull();
                        });
            }
        }

        private static void GetBulkPrices()
        {
            using (new SampleMethodScope())
            {
                var itemIds = new List<string>
                {
                    "Adventure Works Catalog|AW055 01|",
                    "Adventure Works Catalog|AW053 10|",
                    "Adventure Works Catalog|AW072 10|"
                };
                var result = Proxy.Execute(Container.GetBulkPrices(itemIds));

                var itemPricings = result as IList<SellableItemPricing> ?? result.ToList();
                itemPricings.Should().NotBeNull();
                itemPricings.Should().NotBeEmpty();

                foreach (var item in itemPricings)
                {
                    item.ListPrice.Should().NotBeNull();
                    item.SellPrice.Should().NotBeNull();
                    item.ItemId.Should().NotBeNullOrEmpty();

                    if (item.Variations.Any())
                    {
                        item.Variations.ToList()
                            .ForEach(
                                v =>
                                {
                                    v.ListPrice.Should().NotBeNull();
                                    v.SellPrice.Should().NotBeNull();
                                    v.ItemId.Should().NotBeNullOrEmpty();
                                });
                    }
                }
            }
        }

        private static void GetSellableItemAllLanguages()
        {
            using (new SampleMethodScope())
            {
                var jeff = new AnonymousCustomerJeff
                {
                    Context =
                    {
                        Language = "en"
                    }
                };
                var container = jeff.Context.ShopsContainer();
                container.MergeOption = MergeOption.NoTracking;

                var result = Proxy.Execute(container.GetCatalogItemAllLanguages($"{typeof(SellableItem).FullName}, Sitecore.Commerce.Plugin.Catalog", "Adventure Works Catalog|AW072 10|").Expand("Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))")).ToList();
                result.Should().NotBeNullOrEmpty();
                result.Count.Should().Be(4);

                foreach (var item in result)
                {
                    item.Key.Should().NotBeNullOrEmpty();
                    item.Key.Split('_')[0].Should().BeOneOf("en", "fr-FR", "de-DE", "ja-JP");
                    item.Value.Should().NotBeNull();
                    item.Value.Should().BeOfType(typeof(SellableItem));
                    item.Key.Should().EndWith(item.Value.FriendlyId);
                }
            }
        }

        private static void GetSellableItemsAllLanguages()
        {
            using (new SampleMethodScope())
            {
                var jeff = new AnonymousCustomerJeff
                {
                    Context =
                    {
                        Language = "en"
                    }
                };
                var container = jeff.Context.ShopsContainer();
                container.MergeOption = MergeOption.NoTracking;

                var result = Proxy.Execute(container.GetCatalogItemsAllLanguages($"{typeof(SellableItem).FullName}, Sitecore.Commerce.Plugin.Catalog", "SellableItems", 0, 5).Expand("Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))")).ToList();
                result.Should().NotBeNullOrEmpty();

                result.Count.Should().Be(20);

                foreach (var item in result)
                {
                    item.Key.Should().NotBeNullOrEmpty();
                    item.Key.Split('_')[0].Should().BeOneOf("en", "fr-FR", "de-DE", "ja-JP");
                    item.Value.Should().NotBeNull();
                    item.Value.Should().BeOfType(typeof(SellableItem));
                    item.Key.Should().EndWith(item.Value.FriendlyId);
                }
            }
        }

        private static void SellableItemPricing_BackInTime()
        {
            using (new SampleMethodScope())
            {
                // NOW REQUEST
                var result = Proxy.GetValue(
                    Container.SellableItems.ByKey("Adventure Works Catalog,AW055 01,")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                result.Should().NotBeNull();

                result.Policies.OfType<PriceCardPolicy>().Should().NotBeNull();
                result.Policies.OfType<PriceCardPolicy>().First().PriceCardName.Should().Be("AdventureWorksPriceCard");

                result.Policies.OfType<ListPricingPolicy>().Should().NotBeNull();
                result.Policies.OfType<ListPricingPolicy>().First().Prices.First().Amount.Should().Be(58);
                result.Policies.OfType<ListPricingPolicy>().First().Prices.First().CurrencyCode.Should().Be("USD");
                result.ListPrice.Amount.Should().Be(58);

                result.Policies.OfType<PurchaseOptionMoneyPolicy>().Should().NotBeNull();
                result.Policies.OfType<PurchaseOptionMoneyPolicy>().First().SellPrice.Amount.Should().Be(10.0M);
                result.Policies.OfType<PurchaseOptionMoneyPolicy>().First().SellPrice.CurrencyCode.Should().Be("USD");

                result.Components.OfType<PriceSnapshotComponent>().Should().NotBeNull();
                result.Components.OfType<PriceSnapshotComponent>()
                    .First()
                    .Tiers.FirstOrDefault(t => t.Quantity == 1)
                    ?.Price.Should()
                    .Be(10.0M);

                result.Components.OfType<ItemVariationsComponent>().Should().NotBeNull();
                result.Components.OfType<ItemVariationsComponent>().First().ChildComponents.Should().NotBeNull();
                result.Components.OfType<ItemVariationsComponent>()
                    .First()
                    .ChildComponents.Cast<ItemVariationComponent>()
                    .ToList()
                    .ForEach(
                        v =>
                        {
                            v.Policies.OfType<ListPricingPolicy>().Should().NotBeNull();
                            v.Policies.OfType<ListPricingPolicy>()
                                .First()
                                .Prices.First(p => p.CurrencyCode.Equals("USD"))
                                .Amount.Should()
                                .Be(58M);
                            v.ListPrice.Amount.Should().Be(58M);

                            v.Policies.OfType<PurchaseOptionMoneyPolicy>().Should().NotBeNull();
                            v.Policies.OfType<PurchaseOptionMoneyPolicy>()
                                .First()
                                .SellPrice.Amount.Should()
                                .BeInRange(9M, 10M);
                            v.Policies.OfType<PurchaseOptionMoneyPolicy>()
                                .First()
                                .SellPrice.CurrencyCode.Should()
                                .Be("USD");

                            v.ChildComponents.OfType<PriceSnapshotComponent>().Should().NotBeNull();
                            result.Components.OfType<PriceSnapshotComponent>()
                                .First()
                                .Tiers.FirstOrDefault(t => t.Quantity == 1)
                                ?.Price.Should()
                                .BeInRange(9M, 10.0M);
                        });

                // BACK IN TIME REQUEST
                var jeff = new AnonymousCustomerJeff
                {
                    Context =
                    {
                        EffectiveDate = DateTimeOffset.Now.AddDays(-30)
                    }
                };
                var container = jeff.Context.ShopsContainer();
                result = Proxy.GetValue(
                    container.SellableItems.ByKey("Adventure Works Catalog,AW055 01,")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                result.Should().NotBeNull();

                result.Policies.OfType<PriceCardPolicy>().Should().NotBeNull();
                result.Policies.OfType<PriceCardPolicy>().First().PriceCardName.Should().Be("AdventureWorksPriceCard");

                result.Policies.OfType<ListPricingPolicy>().Should().NotBeNull();
                result.Policies.OfType<ListPricingPolicy>()
                    .First()
                    .Prices.First(p => p.CurrencyCode.Equals("USD"))
                    .Amount.Should()
                    .Be(58M);
                result.ListPrice.Amount.Should().Be(58M);

                result.Policies.OfType<PurchaseOptionMoneyPolicy>().Should().NotBeNull();
                result.Policies.OfType<PurchaseOptionMoneyPolicy>().First().SellPrice.Amount.Should().Be(58M);
                result.Policies.OfType<PurchaseOptionMoneyPolicy>().First().SellPrice.CurrencyCode.Should().Be("USD");

                result.Components.OfType<PriceSnapshotComponent>().Any().Should().BeFalse();

                result.Components.OfType<ItemVariationsComponent>().Should().NotBeNull();
                result.Components.OfType<ItemVariationsComponent>().First().ChildComponents.Should().NotBeNull();
                result.Components.OfType<ItemVariationsComponent>()
                    .First()
                    .ChildComponents.Cast<ItemVariationComponent>()
                    .ToList()
                    .ForEach(
                        v =>
                        {
                            v.Policies.OfType<ListPricingPolicy>().Should().NotBeNull();
                            v.Policies.OfType<ListPricingPolicy>()
                                .First()
                                .Prices.First(p => p.CurrencyCode.Equals("USD"))
                                .Amount.Should()
                                .Be(58M);
                            v.ListPrice.Amount.Should().Be(58M);

                            v.Policies.OfType<PurchaseOptionMoneyPolicy>().Should().NotBeNull();
                            v.Policies.OfType<PurchaseOptionMoneyPolicy>().First().SellPrice.Amount.Should().Be(58M);
                            v.Policies.OfType<PurchaseOptionMoneyPolicy>()
                                .First()
                                .SellPrice.CurrencyCode.Should()
                                .Be("USD");

                            v.ChildComponents.OfType<PriceSnapshotComponent>().Any().Should().BeFalse();
                        });
            }
        }

        private static void SellableItemPricing_ForwardInTime()
        {
            using (new SampleMethodScope())
            {
                // NOW REQUEST
                var result = Proxy.GetValue(
                    Container.SellableItems.ByKey("Adventure Works Catalog,AW055 01,")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                result.Should().NotBeNull();

                result.Policies.OfType<PriceCardPolicy>().Should().NotBeNull();
                result.Policies.OfType<PriceCardPolicy>().First().PriceCardName.Should().Be("AdventureWorksPriceCard");

                result.Policies.OfType<ListPricingPolicy>().Should().NotBeNull();
                result.Policies.OfType<ListPricingPolicy>().First().Prices.First().Amount.Should().Be(58);
                result.Policies.OfType<ListPricingPolicy>().First().Prices.First().CurrencyCode.Should().Be("USD");
                result.ListPrice.Amount.Should().Be(58);

                result.Policies.OfType<PurchaseOptionMoneyPolicy>().Should().NotBeNull();
                result.Policies.OfType<PurchaseOptionMoneyPolicy>().First().SellPrice.Amount.Should().Be(10.0M);
                result.Policies.OfType<PurchaseOptionMoneyPolicy>().First().SellPrice.CurrencyCode.Should().Be("USD");

                result.Components.OfType<PriceSnapshotComponent>().Should().NotBeNull();
                result.Components.OfType<PriceSnapshotComponent>()
                    .First()
                    .Tiers.FirstOrDefault(t => t.Quantity == 1)
                    ?.Price.Should()
                    .Be(10.0M);

                result.Components.OfType<ItemVariationsComponent>().Should().NotBeNull();
                result.Components.OfType<ItemVariationsComponent>().First().ChildComponents.Should().NotBeNull();
                result.Components.OfType<ItemVariationsComponent>()
                    .First()
                    .ChildComponents.Cast<ItemVariationComponent>()
                    .ToList()
                    .ForEach(
                        v =>
                        {
                            v.Policies.OfType<ListPricingPolicy>().Should().NotBeNull();
                            v.Policies.OfType<ListPricingPolicy>()
                                .First()
                                .Prices.First(p => p.CurrencyCode.Equals("USD"))
                                .Amount.Should()
                                .Be(58M);
                            v.ListPrice.Amount.Should().Be(58M);

                            v.Policies.OfType<PurchaseOptionMoneyPolicy>().Should().NotBeNull();
                            v.Policies.OfType<PurchaseOptionMoneyPolicy>()
                                .First()
                                .SellPrice.Amount.Should()
                                .BeInRange(9M, 10M);
                            v.Policies.OfType<PurchaseOptionMoneyPolicy>()
                                .First()
                                .SellPrice.CurrencyCode.Should()
                                .Be("USD");

                            v.ChildComponents.OfType<PriceSnapshotComponent>().Should().NotBeNull();
                            result.Components.OfType<PriceSnapshotComponent>()
                                .First()
                                .Tiers.FirstOrDefault(t => t.Quantity == 1)
                                ?.Price.Should()
                                .BeInRange(9M, 10.0M);
                        });

                // BACK IN TIME REQUEST
                var jeff = new AnonymousCustomerJeff
                {
                    Context =
                    {
                        EffectiveDate = DateTimeOffset.Now.AddDays(30)
                    }
                };
                var container = jeff.Context.ShopsContainer();
                result = Proxy.GetValue(
                    container.SellableItems.ByKey("Adventure Works Catalog,AW055 01,")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                result.Should().NotBeNull();

                result.Policies.OfType<PriceCardPolicy>().Should().NotBeNull();
                result.Policies.OfType<PriceCardPolicy>().First().PriceCardName.Should().Be("AdventureWorksPriceCard");

                result.Policies.OfType<ListPricingPolicy>().Should().NotBeNull();
                result.Policies.OfType<ListPricingPolicy>().First().Prices.First().Amount.Should().Be(58M);
                result.Policies.OfType<ListPricingPolicy>().First().Prices.First().CurrencyCode.Should().Be("USD");
                result.ListPrice.Amount.Should().Be(58);

                result.Policies.OfType<PurchaseOptionMoneyPolicy>().Should().NotBeNull();
                result.Policies.OfType<PurchaseOptionMoneyPolicy>().First().SellPrice.Amount.Should().Be(7M);
                result.Policies.OfType<PurchaseOptionMoneyPolicy>().First().SellPrice.CurrencyCode.Should().Be("USD");

                result.Components.OfType<PriceSnapshotComponent>().Should().NotBeNull();
                result.Components.OfType<PriceSnapshotComponent>()
                    .First()
                    .Tiers.FirstOrDefault(t => t.Quantity == 1)
                    ?.Price.Should()
                    .Be(7M);

                result.Components.OfType<ItemVariationsComponent>().Should().NotBeNull();
                result.Components.OfType<ItemVariationsComponent>().First().ChildComponents.Should().NotBeNull();
                result.Components.OfType<ItemVariationsComponent>()
                    .First()
                    .ChildComponents.Cast<ItemVariationComponent>()
                    .ToList()
                    .ForEach(
                        v =>
                        {
                            v.Policies.OfType<ListPricingPolicy>().Should().NotBeNull();
                            v.Policies.OfType<ListPricingPolicy>()
                                .First()
                                .Prices.First(p => p.CurrencyCode.Equals("USD"))
                                .Amount.Should()
                                .Be(58M);
                            v.ListPrice.Amount.Should().Be(58M);

                            v.Policies.OfType<PurchaseOptionMoneyPolicy>().Should().NotBeNull();
                            v.Policies.OfType<PurchaseOptionMoneyPolicy>()
                                .First()
                                .SellPrice.Amount.Should()
                                .BeInRange(7M, 10M);
                            v.Policies.OfType<PurchaseOptionMoneyPolicy>()
                                .First()
                                .SellPrice.CurrencyCode.Should()
                                .Be("USD");

                            v.ChildComponents.OfType<PriceSnapshotComponent>().Should().NotBeNull();
                            result.Components.OfType<PriceSnapshotComponent>()
                                .First()
                                .Tiers.FirstOrDefault(t => t.Quantity == 1)
                                ?.Price.Should()
                                .Be(7M);
                        });
            }
        }

        private static void UpdateListPrices_SellableItem()
        {
            using (new SampleMethodScope())
            {
                var sellableItemBefore = Proxy.GetValue(
                    Container.SellableItems.ByKey("Adventure Works Catalog,AW140 13,")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                sellableItemBefore.Should().NotBeNull();

                var price1 = Money.CreateMoney(10M);
                price1.CurrencyCode = "USD";
                var price2 = Money.CreateMoney(12M);
                price2.CurrencyCode = "CAD";

                var result = Proxy.DoCommand(
                    Container.UpdateListPrices(
                        "Adventure Works Catalog|AW140 13|",
                        new List<Money>
                        {
                            price1,
                            price2
                        }));
                result.Should().NotBeNull();
                result.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase)).Should().BeFalse();

                var sellableItem = Proxy.GetValue(
                    Container.SellableItems.ByKey("Adventure Works Catalog,AW140 13,")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                sellableItem.Should().NotBeNull();
                sellableItem.Policies.OfType<ListPricingPolicy>().FirstOrDefault()?.Prices.Should().NotBeNullOrEmpty();
                sellableItem.Policies.OfType<ListPricingPolicy>()
                    .FirstOrDefault()
                    ?.Prices.FirstOrDefault(p => p.CurrencyCode.Equals(price1.CurrencyCode))
                    .Should()
                    .NotBeNull();
                sellableItem.Policies.OfType<ListPricingPolicy>()
                    .FirstOrDefault()
                    ?.Prices.FirstOrDefault(p => p.CurrencyCode.Equals(price1.CurrencyCode))
                    ?.Amount.Should()
                    .Be(price1.Amount);
                sellableItem.Policies.OfType<ListPricingPolicy>()
                    .FirstOrDefault()
                    ?.Prices.FirstOrDefault(p => p.CurrencyCode.Equals(price2.CurrencyCode))
                    .Should()
                    .NotBeNull();
                sellableItem.Policies.OfType<ListPricingPolicy>()
                    .FirstOrDefault()
                    ?.Prices.FirstOrDefault(p => p.CurrencyCode.Equals(price2.CurrencyCode))
                    ?.Amount.Should()
                    .Be(price2.Amount);

                result = Proxy.DoCommand(
                    Container.UpdateListPrices(
                        "Adventure Works Catalog|AW140 13|",
                        new List<Money>
                        {
                            Money.CreateMoney(10)
                        }));
                result.Messages.Any(m => m.Code.Equals("validationerror", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
                ConsoleExtensions.WriteColoredLine(ConsoleColor.Yellow, "Expected error");
            }
        }

        private static void UpdateListPrices_Variation()
        {
            using (new SampleMethodScope())
            {
                var price1 = Money.CreateMoney(10M);
                price1.CurrencyCode = "USD";
                var price2 = Money.CreateMoney(12M);
                price2.CurrencyCode = "CAD";

                var result = Proxy.DoCommand(
                    Container.UpdateListPrices(
                        "Adventure Works Catalog|AW140 13|41",
                        new List<Money>
                        {
                            price1,
                            price2
                        }));
                result.Should().NotBeNull();
                result.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase)).Should().BeFalse();

                var sellableItem = Proxy.GetValue(
                    Container.SellableItems.ByKey("Adventure Works Catalog,AW140 13,41")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                sellableItem.Should().NotBeNull();
                var variation = sellableItem.Components.OfType<ItemVariationsComponent>()
                    .FirstOrDefault()
                    ?
                    .ChildComponents.OfType<ItemVariationComponent>()
                    .FirstOrDefault();
                variation.Should().NotBeNull();
                variation?.Policies.OfType<ListPricingPolicy>().FirstOrDefault()?.Prices.Should().NotBeNullOrEmpty();
                variation?.Policies.OfType<ListPricingPolicy>()
                    .FirstOrDefault()
                    ?.Prices.FirstOrDefault(p => p.CurrencyCode.Equals(price1.CurrencyCode))
                    .Should()
                    .NotBeNull();
                variation?.Policies.OfType<ListPricingPolicy>()
                    .FirstOrDefault()
                    ?.Prices.FirstOrDefault(p => p.CurrencyCode.Equals(price1.CurrencyCode))
                    ?.Amount.Should()
                    .Be(price1.Amount);
                variation?.Policies.OfType<ListPricingPolicy>()
                    .FirstOrDefault()
                    ?.Prices.FirstOrDefault(p => p.CurrencyCode.Equals(price2.CurrencyCode))
                    .Should()
                    .NotBeNull();
                variation?.Policies.OfType<ListPricingPolicy>()
                    .FirstOrDefault()
                    ?.Prices.FirstOrDefault(p => p.CurrencyCode.Equals(price2.CurrencyCode))
                    ?.Amount.Should()
                    .Be(price2.Amount);

                result = Proxy.DoCommand(
                    Container.UpdateListPrices(
                        "Adventure Works Catalog|AW140 13|41",
                        new List<Money>
                        {
                            Money.CreateMoney(10)
                        }));
                result.Messages.Any(m => m.Code.Equals("validationerror", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
                ConsoleExtensions.WriteColoredLine(ConsoleColor.Yellow, "Expected error");
            }
        }

        private static void RemoveListPrices_SellableItem()
        {
            using (new SampleMethodScope())
            {
                var price1 = Money.CreateMoney(10M);
                price1.CurrencyCode = "USD";
                var price2 = Money.CreateMoney(12M);
                price2.CurrencyCode = "CAD";

                var result = Proxy.DoCommand(
                    Container.UpdateListPrices(
                        "Adventure Works Catalog|AW140 13|",
                        new List<Money>
                        {
                            price1,
                            price2
                        }));
                result.Should().NotBeNull();

                result = Proxy.DoCommand(
                    Container.RemoveListPrices("Adventure Works Catalog|AW140 13|", new List<Money>
                    {
                        price1
                    }));
                result.Should().NotBeNull();
                result.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase)).Should().BeFalse();

                var sellableItem = Proxy.GetValue(
                    Container.SellableItems.ByKey("Adventure Works Catalog,AW140 13,")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                sellableItem.Should().NotBeNull();
                sellableItem.Policies.OfType<ListPricingPolicy>().FirstOrDefault()?.Prices.Should().NotBeNullOrEmpty();
                sellableItem.Policies.OfType<ListPricingPolicy>()
                    .FirstOrDefault()
                    ?.Prices.Any(p => p.CurrencyCode.Equals(price1.CurrencyCode))
                    .Should()
                    .BeFalse();
                sellableItem.Policies.OfType<ListPricingPolicy>()
                    .FirstOrDefault()
                    ?.Prices.Any(p => p.CurrencyCode.Equals(price2.CurrencyCode))
                    .Should()
                    .BeTrue();

                result = Proxy.DoCommand(
                    Container.RemoveListPrices(
                        "Adventure Works Catalog|AW140 13|",
                        new List<Money>
                        {
                            Money.CreateMoney(10)
                        }));
                result.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
                ConsoleExtensions.WriteColoredLine(ConsoleColor.Yellow, "Expected error");
            }
        }

        private static void RemoveListPrices_Variation()
        {
            using (new SampleMethodScope())
            {
                var price1 = Money.CreateMoney(10M);
                price1.CurrencyCode = "USD";
                var price2 = Money.CreateMoney(12M);
                price2.CurrencyCode = "CAD";

                var result = Proxy.DoCommand(
                    Container.UpdateListPrices(
                        "Adventure Works Catalog|AW140 13|41",
                        new List<Money>
                        {
                            price1,
                            price2
                        }));
                result.Should().NotBeNull();

                result = Proxy.DoCommand(
                    Container.RemoveListPrices("Adventure Works Catalog|AW140 13|41", new List<Money>
                    {
                        price1
                    }));
                result.Should().NotBeNull();
                result.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase)).Should().BeFalse();

                var sellableItem = Proxy.GetValue(
                    Container.SellableItems.ByKey("Adventure Works Catalog,AW140 13,41")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                sellableItem.Should().NotBeNull();
                var variation = sellableItem.Components.OfType<ItemVariationsComponent>()
                    .FirstOrDefault()
                    ?
                    .ChildComponents.OfType<ItemVariationComponent>()
                    .FirstOrDefault();
                variation.Should().NotBeNull();
                variation?.Policies.OfType<ListPricingPolicy>().FirstOrDefault()?.Prices.Should().NotBeNullOrEmpty();
                variation?.Policies.OfType<ListPricingPolicy>()
                    .FirstOrDefault()
                    ?.Prices.Any(p => p.CurrencyCode.Equals(price1.CurrencyCode))
                    .Should()
                    .BeFalse();
                variation?.Policies.OfType<ListPricingPolicy>()
                    .FirstOrDefault()
                    ?.Prices.Any(p => p.CurrencyCode.Equals(price2.CurrencyCode))
                    .Should()
                    .BeTrue();

                result = Proxy.DoCommand(
                    Container.RemoveListPrices(
                        "Adventure Works Catalog|AW140 13|41",
                        new List<Money>
                        {
                            Money.CreateMoney(10)
                        }));
                result.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
                ConsoleExtensions.WriteColoredLine(ConsoleColor.Yellow, "Expected error");
            }
        }
    }
}
