using System;
using System.IO;
using System.Linq;
using System.Text;
using CommerceOps.Sitecore.Commerce.Core;
using CommerceOps.Sitecore.Commerce.Engine;
using CommerceOps.Sitecore.Commerce.Plugin.Availability;
using FluentAssertions;
using Newtonsoft.Json;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;
using CommerceEnvironment = Sitecore.Commerce.Core.CommerceEnvironment;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Environments
    {
        private static readonly Container OpsContainer
            = new DevOpAndre
            {
                Context =
                {
                    Environment = "GlobalEnvironment"
                }
            }.Context.OpsContainer();

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Environments"))
            {
                NewEnvironment();

                var habitat = Proxy.GetValue(
                    OpsContainer.Environments.ByKey("Entity-CommerceEnvironment-HabitatAuthoring")
                        .Expand("Components"));
                habitat.Should().NotBeNull();
            }
        }

        private static void NewEnvironment()
        {
            using (new SampleMethodScope())
            {
                var environmentName = $"Console{Guid.NewGuid():N}";
                var environment = new CommerceEnvironment
                {
                    Name = environmentName
                };
                var environmentJson = JsonConvert.SerializeObject(environment);

                ImportEnvironment(environmentJson);

                var getEnvironment = Proxy.GetValue(
                    OpsContainer.Environments.ByKey(environmentName)
                        .Expand("Components"));
                getEnvironment.Should()
                    .NotBeNull("Verify environment can be found (belongs in list) after it imported");

                var exportedEnvironmentJson = ExportEnvironment(environmentName);
                exportedEnvironmentJson.Should().NotBeNullOrEmpty("Verify environment exported");

                var exportedEnvironment =
                    JsonConvert.DeserializeObject<CommerceEnvironment>(exportedEnvironmentJson);
                exportedEnvironment.Name.Should()
                    .Be(
                        environment.Name,
                        "Verify exported environment can be deserialized and the environment name was preserved");
                exportedEnvironment.DisplayName = "New Display Name";

                ImportEnvironment(JsonConvert.SerializeObject(exportedEnvironment));

                var updatedEnvironmentJson = ExportEnvironment(environmentName);
                var updatedEnvironment =
                    JsonConvert.DeserializeObject<CommerceEnvironment>(updatedEnvironmentJson);
                updatedEnvironment.DisplayName.Should().Be("New Display Name", "Verify updated environment");
            }
        }

        private static void CloneEnvironment()
        {
            using (new SampleMethodScope())
            {
                var originalEnvironment =
                    ExportEnvironment(
                        EnvironmentConstants.AdventureWorksShops); // Export an Environment to use as a template
                var serializer = new JsonSerializer
                {
                    TypeNameHandling = TypeNameHandling.All,
                    NullValueHandling = NullValueHandling.Ignore
                };
                var reader = new StringReader(originalEnvironment);
                CommerceOps.Sitecore.Commerce.Core.CommerceEnvironment newEnvironment = null;
                using (var jsonReader = new JsonTextReader(reader)
                {
                    DateParseHandling = DateParseHandling.DateTimeOffset
                })
                {
                    newEnvironment = serializer.Deserialize<CommerceOps.Sitecore.Commerce.Core.CommerceEnvironment>(jsonReader);
                }

                // Change the Id of the environment in order to import as a new Environment
                var newEnvironmentId = Guid.NewGuid();
                newEnvironment.ArtifactStoreId = newEnvironmentId;
                newEnvironment.Name = "ConsoleSample." + newEnvironmentId.ToString("N");
                var sw = new StringWriter(new StringBuilder());
                string newSerializedEnvironment = null;
                using (var writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, newSerializedEnvironment);
                }

                newSerializedEnvironment = sw.ToString();

                // imports the environment into Sitecore Commerce
                var importedEnvironment = ImportEnvironment(newSerializedEnvironment);
                importedEnvironment.EntityId.Should()
                    .Be($"Entity-CommerceEnvironment-ConsoleSample.{newEnvironmentId}");
                importedEnvironment.Name.Should().Be($"ConsoleSample.{newEnvironmentId}");

                // Adds a policy
                var policyResult = Proxy.DoOpsCommand(
                    OpsContainer.AddPolicy(
                        importedEnvironment.EntityId,
                        "Sitecore.Commerce.Plugin.Availability.GlobalAvailabilityPolicy, Sitecore.Commerce.Plugin.Availability",
                        new GlobalAvailabilityPolicy
                        {
                            AvailabilityExpires = 0
                        },
                        "GlobalEnvironment"));
                policyResult.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeFalse();
                policyResult.Models.OfType<PolicyAddedModel>().Any().Should().BeTrue();

                // Initialize the Environment with default artifacts
                Bootstrapping.InitializeEnvironment(OpsContainer, $"ConsoleSample.{newEnvironmentId}");

                // Get a SellableItem from the environment to assure that we have set it up correctly
                var shopperInNewEnvironmentContainer = new RegisteredCustomerDana
                {
                    Context =
                    {
                        Environment = $"ConsoleSample.{newEnvironmentId}"
                    }
                }.Context.ShopsContainer();
                var result = Proxy.GetValue(
                    shopperInNewEnvironmentContainer.SellableItems.ByKey("Adventure Works Catalog,AW055 01,")
                        .Expand(
                            "Components($expand=ChildComponents($expand=ChildComponents($expand=ChildComponents)))"));
                result.Should().NotBeNull();
                result.Name.Should().Be("Unisex hiking pants");

                // Get the environment to validate change was made
                var updatedEnvironment =
                    Proxy.GetValue(OpsContainer.Environments.ByKey(importedEnvironment.EntityUniqueId.ToString()));
                var globalAvailabilityPolicy =
                    updatedEnvironment.Policies.OfType<GlobalAvailabilityPolicy>().FirstOrDefault();
                globalAvailabilityPolicy.Should().NotBeNull();
                globalAvailabilityPolicy.AvailabilityExpires.Should().Be(0);
            }
        }

        private static PersistedEntityModel ImportEnvironment(string environmentAsString)
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoOpsCommand(OpsContainer.ImportEnvironment(environmentAsString));

                result.Should().NotBeNull();
                result.Messages.Should().NotContainErrors();
                var persistedModel =
                    result.Models.OfType<PersistedEntityModel>()
                        .FirstOrDefault();
                persistedModel.Should().NotBeNull();
                persistedModel.EntityId.Should().NotBeNullOrEmpty();
                persistedModel.EntityUniqueId.Should().NotBe(Guid.Empty);

                return result.Models.OfType<PersistedEntityModel>().First();
            }
        }

        private static string ExportEnvironment(string environmentName)
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(OpsContainer.ExportEnvironment(environmentName));
                result.Should().NotBeNull();
                return result;
            }
        }
    }
}
