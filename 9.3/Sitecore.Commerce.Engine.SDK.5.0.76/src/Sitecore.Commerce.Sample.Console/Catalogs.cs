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
    public static class Catalogs
    {
        private static readonly Container ShopsContainer = new AnonymousCustomerJeff().Context.ShopsContainer();

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Catalogs"))
            {
                GetCatalog();
                GetCatalogs();
                GetCatalogAllLanguages();
                GetCatalogsAllLanguages();
            }
        }

        private static void GetCatalog()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(ShopsContainer.Catalogs.ByKey("Adventure Works Catalog"));
                result.Should().NotBeNull();
            }
        }

        private static void GetCatalogs()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.Execute(ShopsContainer.Catalogs);
                result.Should().NotBeNull();
            }
        }

        private static void GetCatalogAllLanguages()
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

                var result = Proxy.Execute(container.GetCatalogItemAllLanguages($"{typeof(Catalog).FullName}, Sitecore.Commerce.Plugin.Catalog", "Adventure Works Catalog").Expand("Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))")).ToList();
                result.Should().NotBeNullOrEmpty();
                result.Count.Should().Be(4);

                foreach (var item in result)
                {
                    item.Key.Should().NotBeNullOrEmpty();
                    item.Key.Split('_')[0].Should().BeOneOf("en", "fr-FR", "de-DE", "ja-JP");
                    item.Value.Should().NotBeNull();
                    item.Value.Should().BeOfType(typeof(Catalog));
                    item.Key.Should().EndWith(item.Value.FriendlyId);
                }
            }
        }

        private static void GetCatalogsAllLanguages()
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

                var result = Proxy.Execute(container.GetCatalogItemsAllLanguages($"{typeof(Catalog).FullName}, Sitecore.Commerce.Plugin.Catalog", "Catalogs", 0, 5).Expand("Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))")).ToList();
                result.Should().NotBeNullOrEmpty();
                result.Count.Should().Be(4);

                foreach (var item in result)
                {
                    item.Key.Should().NotBeNullOrEmpty();
                    item.Key.Split('_')[0].Should().BeOneOf("en", "fr-FR", "de-DE", "ja-JP");
                    item.Value.Should().NotBeNull();
                    item.Value.Should().BeOfType(typeof(Catalog));
                    item.Key.Should().EndWith(item.Value.FriendlyId);
                }
            }
        }
    }
}
