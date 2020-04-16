using System.Linq;
using FluentAssertions;
using Microsoft.OData.Client;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Categories
    {
        private static readonly Container ShopsContainer = new AnonymousCustomerJeff().Context.ShopsContainer();

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Categories"))
            {
                GetCategory();
                GetCategories();
                GetCategoryAllLanguages();
                GetCategoriesAllLanguages();
            }
        }

        private static void GetCategory()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(ShopsContainer.Categories.ByKey("Adventure Works Catalog-Backpacks"));
                result.Should().NotBeNull();
                result.Name.Should().Be("Backpacks");
                result.Description.Should().Be("Catalog Section for Backpack");
            }
        }

        private static void GetCategories()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.Execute(ShopsContainer.Categories);
                result.Should().NotBeNull();
            }
        }

        private static void GetCategoryAllLanguages()
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

                var result = Proxy.Execute(container.GetCatalogItemAllLanguages($"{typeof(Category).FullName}, Sitecore.Commerce.Plugin.Catalog", "Adventure Works Catalog-Backpacks").Expand("Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))")).ToList();
                result.Should().NotBeNullOrEmpty();
                result.Count.Should().Be(4);

                foreach (var item in result)
                {
                    item.Key.Should().NotBeNullOrEmpty();
                    item.Key.Split('_')[0].Should().BeOneOf("en", "fr-FR", "de-DE", "ja-JP");
                    item.Value.Should().NotBeNull();
                    item.Value.Should().BeOfType(typeof(Category));
                    item.Key.Should().EndWith(item.Value.FriendlyId);
                }
            }
        }

        private static void GetCategoriesAllLanguages()
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

                var result = Proxy.Execute(container.GetCatalogItemsAllLanguages($"{typeof(Category).FullName}, Sitecore.Commerce.Plugin.Catalog", "Categories", 0, 5).Expand("Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))")).ToList();
                result.Should().NotBeNullOrEmpty();

                result.Count.Should().Be(20);

                foreach (var item in result)
                {
                    item.Key.Should().NotBeNullOrEmpty();
                    item.Key.Split('_')[0].Should().BeOneOf("en", "fr-FR", "de-DE", "ja-JP");
                    item.Value.Should().NotBeNull();
                    item.Value.Should().BeOfType(typeof(Category));
                    item.Key.Should().EndWith(item.Value.FriendlyId);
                }
            }
        }
    }
}
