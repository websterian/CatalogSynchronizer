using System;
using System.Collections.ObjectModel;
using System.Linq;
using FluentAssertions;
using Microsoft.OData.Client;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Versions
    {
        private static readonly Container AuthoringContainer = new AnonymousCustomerJeff(EnvironmentConstants.AdventureWorksAuthoring)
            .Context.AuthoringContainer();

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Versions"))
            {
                AddCatalogVersion();
            }
        }

        private static void AddCatalogVersion()
        {
            using (new SampleMethodScope())
            {
                var catalogName = Guid.NewGuid().ToString("N");
                var entityId = $"Entity-Catalog-{catalogName}";

                // Create catalog
                var addCatalogView = Proxy.GetValue(
                    AuthoringContainer.GetEntityView(string.Empty, "Details", "AddCatalog", string.Empty));
                addCatalogView.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = catalogName
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = catalogName
                    }
                };
                var addCatalogResult = Proxy.DoCommand(AuthoringContainer.DoAction(addCatalogView));
                addCatalogResult.Messages.Should().NotContainErrors();

                // Create new version
                var addVersionView = Proxy.GetValue(AuthoringContainer.GetEntityView(entityId, string.Empty, "AddEntityVersion", string.Empty));
                var addVersionResult = Proxy.DoCommand(AuthoringContainer.DoAction(addVersionView));
                addVersionResult.Messages.Should().NotContainErrors();

                // Change merge option to retrieve all entities.
                AuthoringContainer.MergeOption = MergeOption.NoTracking;

                var versions = AuthoringContainer.FindEntityVersions(
                        "Sitecore.Commerce.Plugin.Catalog.Catalog, Sitecore.Commerce.Plugin.Catalog",
                        entityId)
                    .Execute()
                    .ToList();
                versions.Count.Should().Be(2);
                versions.ForEach(v => v.EntityVersion.Should().BeOneOf(1, 2));
            }
        }
    }
}
