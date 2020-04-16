using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class CategoriesUX
    {
        internal const string CatalogName = "Adventure Works Catalog";
        internal static string _parentCategoryName;
        internal static string _childCategoryName;
        internal static string _catalogId;
        internal static string _parentCategoryId;
        internal static string _childCategoryId;

        public static void RunScenarios()
        {
            using (new SampleScenarioScope(MethodBase.GetCurrentMethod().DeclaringType.Name))
            {
                var partial = $"{Guid.NewGuid():N}".Substring(0, 5);
                _parentCategoryName = $"ConsoleCategory{partial}";
                _childCategoryName = $"ConsoleChildCategory{partial}";
                _catalogId = $"Entity-Catalog-{CatalogName}";
                _parentCategoryId = $"Entity-Category-{CatalogName}-{_parentCategoryName}";
                _childCategoryId = $"Entity-Category-{CatalogName}-{_childCategoryName}";

                EngineExtensions.AssertCatalogExists(_catalogId);

                AddCategoryToCatalog();
                AddCategoryToCategory();
                EditCategory();
                TryEditCategoryUsingShopsEnvironment();
                EngineExtensions.DeleteCategory(_childCategoryId);
                EngineExtensions.DeleteCategory(_parentCategoryId);
            }
        }

        private static void AddCategoryToCatalog()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.AssertCatalogExists(_catalogId);
                EngineExtensions.AddCategory(_parentCategoryId, _catalogId, CatalogName);
            }
        }

        private static void AddCategoryToCategory()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.AssertCategoryExists(_parentCategoryId);
                EngineExtensions.AddCategory(_childCategoryId, _parentCategoryId, _parentCategoryName);
            }
        }

        private static void TryEditCategoryUsingShopsEnvironment()
        {
            using (new SampleMethodScope())
            {
                var shopsContainer = new AnonymousCustomerJeff().Context.ShopsContainer();
                var view = Proxy.GetValue(
                    shopsContainer.GetEntityView(_parentCategoryId, "Details", "EditCategory", string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().BeEmpty();
                view.ChildViews.Should().BeEmpty();
            }
        }

        private static void EditCategory()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(EngineExtensions.AuthoringContainer.Value.GetEntityView(_parentCategoryId, "Details", "EditCategory", string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = "Console UX Category (updated)"
                    },
                    new ViewProperty
                    {
                        Name = "Description",
                        Value = "Console UX Category Description"
                    },
                    version
                };

                var result = Proxy.DoCommand(EngineExtensions.AuthoringContainer.Value.DoAction(view));
                result.Messages.Any(m => m.Code.Equals("error", StringComparison.OrdinalIgnoreCase)).Should().BeFalse();
            }
        }
    }
}
