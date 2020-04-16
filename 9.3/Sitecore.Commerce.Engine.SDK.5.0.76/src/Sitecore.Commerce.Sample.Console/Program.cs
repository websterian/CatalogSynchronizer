using System;
using System.Diagnostics;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Sample.Console.Authentication;
using Sitecore.Commerce.Sample.Console.Properties;

namespace Sitecore.Commerce.Sample.Console
{
    public class Program
    {
        public static string DefaultStorefront = "CommerceEngineDefaultStorefront";

        public static string OpsServiceUri = "https://localhost:5000/CommerceOps/";
        public static string ShopsServiceUri = "https://localhost:5000/api/";
        public static string MinionsServiceUri = "https://localhost:5000/CommerceOps/";
        public static string AuthoringServiceUri = "https://localhost:5000/api/";
        public static string SitecoreIdServerUri = "https://sxastorefront-identityserver/";

        public static string UserName = @"sitecore\admin";
        public static string Password = "b";

        public static string SitecoreTokenRaw;

        public static string SitecoreToken;

        // Should the environment be bootstrapped when this program runs?
        private static readonly bool ShouldBootstrapOnLoad = true;
        private static readonly bool ShouldDevOpsScenarios = true;
        private static readonly bool ShouldRunCatalogScenarios = true;
        private static readonly bool ShouldRunPricingScenarios = true;
        private static readonly bool ShouldRunPromotionsScenarios = true;
        private static readonly bool ShouldRunInventoryScenarios = true;
        private static readonly bool ShouldRunOrdersScenarios = true;
        private static readonly bool ShouldRunCustomersScenarios = true;
        private static readonly bool ShouldRunEntitlementsScenarios = true;
        private static readonly bool ShouldRunSearchScenarios = true;
        private static readonly bool ShouldRunBusinessUsersScenarios = true;
        private static readonly bool ShouldRunVersionScenarios = true;

        private static readonly bool DemoStops = true;

        static void Main(string[] args)
        {
            try
            {
                OpsServiceUri = Settings.Default.OpsServiceUri;
                ShopsServiceUri = Settings.Default.ShopsServiceUri;
                MinionsServiceUri = Settings.Default.MinionsServiceUri;
                AuthoringServiceUri = Settings.Default.AuthoringServiceUri;
                SitecoreIdServerUri = Settings.Default.SitecoreIdServerUri;

                UserName = Settings.Default.UserName;
                Password = Settings.Default.Password;

                SitecoreTokenRaw = SitecoreIdServerAuth.GetToken();
                SitecoreToken = $"Bearer {SitecoreTokenRaw}";

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                System.Console.ForegroundColor = ConsoleColor.Cyan;

                if (ShouldBootstrapOnLoad)
                {
                    Bootstrapping.RunScenarios();
                    Content.RunScenarios();
                }

                if (ShouldDevOpsScenarios)
                {
                    Environments.RunScenarios();

                    Plugins.RunScenarios();

                    Entities.RunScenarios();

                    Policies.RunScenarios();

                    Caching.RunScenarios();
                }

                if (ShouldRunCatalogScenarios)
                {
                    Catalogs.RunScenarios();
                    CatalogsUX.RunScenarios();

                    Categories.RunScenarios();
                    CategoriesUX.RunScenarios();

                    SellableItems.RunScenarios();
                    SellableItemsUX.RunScenarios();
                }

                if (ShouldRunPricingScenarios)
                {
                    Pricing.RunScenarios();
                    PricingUX.RunScenarios();
                }

                if (ShouldRunPromotionsScenarios)
                {
                    Promotions.RunScenarios();
                    PromotionsUX.RunScenarios();
                    PromotionsRuntime.RunScenarios();

                    Rules.RunScenarios();

                    Coupons.RunScenarios();
                    CouponsUX.RunScenarios();
                }

                if (ShouldRunInventoryScenarios)
                {
                    Inventory.RunScenarios();
                    InventoryUX.RunScenarios();
                }

                if (ShouldRunOrdersScenarios)
                {
                    Fulfillment.RunScenarios();

                    Payments.RunScenarios();

                    Carts.RunScenarios();

                    Returns.RunScenarios();

                    OrdersUX.RunScenarios();
                    Orders.RunScenarios();

                    Shipments.RunScenarios(); // ORDERS HAVE TO BE RELEASED FOR SHIPMENTS TO GET GENERATED
                }

                if (ShouldRunCustomersScenarios)
                {
                    CustomersUX.RunScenarios();
                }

                if (ShouldRunEntitlementsScenarios)
                {
                    Entitlements.RunScenarios();
                }

                if (ShouldRunSearchScenarios)
                {
                    Search.RunScenarios();
                }

                if (ShouldRunBusinessUsersScenarios)
                {
                    ComposerUX.RunScenarios();
                    Composer.RunScenarios();
                }

                if (ShouldRunVersionScenarios)
                {
                    Versions.RunScenarios();
                }

                stopwatch.Stop();

                System.Console.WriteLine($"Test Runs Complete - {stopwatch.ElapsedMilliseconds} ms -  (Hit any key to continue)");

                if (DemoStops)
                {
                    System.Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                ConsoleExtensions.WriteErrorLine("An unexpected exception occurred.");
                ConsoleExtensions.WriteErrorLine(ex.ToString());
                System.Console.ReadKey();
            }

            System.Console.WriteLine("done.");
        }
    }
}
