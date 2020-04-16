using System.Linq;
using CommerceOps.Sitecore.Commerce.Engine;
using FluentAssertions;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Caching
    {
        private static readonly Container OpsContainer = new DevOpAndre().Context.OpsContainer();

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Caching"))
            {
                GetStoreCaches();
                GetStoreCache(); // assuming enabled provider is redis
                GetDataStoreCache(); // assuming enabled provider is redis
                GetCache();
                ClearCache();
                ClearDataStoreCaches();
                ClearCacheStore();
            }
        }

        private static void ClearCacheStore()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(OpsContainer.ClearCacheStore());
                result.Should().NotBeNull();
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void ClearDataStoreCaches()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(OpsContainer.ClearDataStoreCaches());
                result.Should().NotBeNull();
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void ClearCache()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(OpsContainer.ClearCache("Items"));
                result.Should().NotBeNull();
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void GetStoreCaches()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.Execute(OpsContainer.GetCacheStores()).ToList();
                result.Should().NotBeNull();
                result.Should().NotBeEmpty();
            }
        }

        private static void GetStoreCache()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(OpsContainer.GetCacheStore("Commerce-Redis-Store"));
                result.Should().NotBeNull();
                result.Caches.Should().NotBeEmpty();
            }
        }

        private static void GetDataStoreCache()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(OpsContainer.GetDataCacheStore("Commerce-Redis-Store"));
                result.Should().NotBeNull();
                result.Caches.Should().NotBeEmpty();
            }
        }

        private static void GetCache()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(OpsContainer.GetCache("Items"));
                result.Should().NotBeNull();
            }
        }
    }
}
