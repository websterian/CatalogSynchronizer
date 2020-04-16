using System;
using System.Collections.ObjectModel;
using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Composer;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class ComposerUX
    {
        internal const string CatalogName = "Adventure Works Catalog";
        private static string _templateId;
        private static string _composerViewName;
        private static string _composerViewDisplayName;
        private static string _templateName;
        private static string _entityId;
        private static Container _authoringContainer;

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Composer UX"))
            {
                var partial = $"{Guid.NewGuid():N}".Substring(0, 5);
                _templateId = $"Entity-ComposerTemplate-MyConsoleTemplate{partial}";
                _composerViewName = $"MyConsoleView{partial}";
                _composerViewDisplayName = $"My Console View {partial}";
                _templateName = $"MyConsoleTemplate{partial}";

                _entityId = $"ConsoleComposerProduct{partial}".ToEntityId<SellableItem>();
                EngineExtensions.AddSellableItem(_entityId, CatalogName.ToEntityId<Catalog>(), CatalogName, CatalogName);

                var context = new CsrSheila().Context;
                context.Environment = EnvironmentConstants.AdventureWorksAuthoring;
                _authoringContainer = context.ShopsContainer();

                var composerEntityViewItemId = AddChildView();
                AddProperties(composerEntityViewItemId, _entityId);
                EditView(composerEntityViewItemId, _entityId);
                AddMinMaxPropertyConstraint(composerEntityViewItemId, _entityId);
                AddSelectionOptionPropertyConstraint(composerEntityViewItemId, _entityId);
                RemoveProperty(composerEntityViewItemId, _entityId);
                RemoveView(composerEntityViewItemId, _entityId);

                composerEntityViewItemId = AddChildView();
                MakeTemplate(composerEntityViewItemId);
                RemoveView(composerEntityViewItemId, _entityId);

                composerEntityViewItemId = AddChildViewFromTemplate();
                RemoveView(composerEntityViewItemId, _entityId);

                var composerTemplateViewItemId = GetTemplateViews();
                ManageTemplateTags();
                LinkTemplateToEntities();
                AddProperties(composerTemplateViewItemId, _templateId);
                EditView(composerTemplateViewItemId, _templateId);
                AddMinMaxPropertyConstraint(composerTemplateViewItemId, _templateId);
                AddSelectionOptionPropertyConstraint(composerTemplateViewItemId, _templateId);
                RemoveProperty(composerTemplateViewItemId, _templateId);
                RemoveTemplate();

                CreateTemplate();
            }
        }

        private static string AddChildView()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "AddChildView", "AddChildView", string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = string.Empty
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = string.Empty
                    },
                    version
                };
                var result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = _composerViewName
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = _composerViewDisplayName
                    },
                    version
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                var masterView = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "Master", string.Empty, string.Empty));
                masterView.Should().NotBeNull();
                var composerView = masterView.ChildViews.OfType<EntityView>()
                    .FirstOrDefault(v => v.Name.Equals(_composerViewName));
                composerView.Should().NotBeNull();
                return composerView.ItemId;
            }
        }

        private static string AddChildViewFromTemplate()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(
                        _entityId,
                        "AddChildViewFromTemplate",
                        "AddChildViewFromTemplate",
                        string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Template",
                        Value = string.Empty
                    },
                    version
                };
                var result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Template",
                        Value = _templateName
                    },
                    version
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                var masterView = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "Master", string.Empty, string.Empty));
                masterView.Should().NotBeNull();
                var composerView = masterView.ChildViews.OfType<EntityView>()
                    .FirstOrDefault(v => v.Name.Equals(_composerViewName));
                composerView.Should().NotBeNull();
                return composerView.ItemId;
            }
        }

        private static void AddProperties(string composerViewItemId, string _entityId)
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "AddProperty", "AddProperty", composerViewItemId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = string.Empty
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = string.Empty
                    },
                    new ViewProperty
                    {
                        Name = "PropertyType",
                        Value = string.Empty
                    },
                    version
                };
                var result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = "MyStringProperty"
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = "My String Property"
                    },
                    new ViewProperty
                    {
                        Name = "PropertyType",
                        Value = "System.String"
                    },
                    version
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "AddProperty", "AddProperty", composerViewItemId));
                version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = "MyDecimalProperty"
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = "My Decimal Property"
                    },
                    new ViewProperty
                    {
                        Name = "PropertyType",
                        Value = "System.Decimal"
                    },
                    version
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "AddProperty", "AddProperty", composerViewItemId));
                version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = "MyIntProperty"
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = "My Int Property"
                    },
                    new ViewProperty
                    {
                        Name = "PropertyType",
                        Value = "System.Int64"
                    },
                    version
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "AddProperty", "AddProperty", composerViewItemId));
                version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = "MyDateProperty"
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = "My Date Property"
                    },
                    new ViewProperty
                    {
                        Name = "PropertyType",
                        Value = "System.DateTimeOffset"
                    },
                    version
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "AddProperty", "AddProperty", composerViewItemId));
                version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = "MyBoolProperty"
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = "My Bool Property"
                    },
                    new ViewProperty
                    {
                        Name = "PropertyType",
                        Value = "System.Boolean"
                    },
                    version
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                var masterView = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "Master", string.Empty, string.Empty));
                masterView.Should().NotBeNull();
                var composerView = masterView.ChildViews.OfType<EntityView>()
                    .FirstOrDefault(v => v.Name.Equals(_composerViewName));
                composerView.Should().NotBeNull();
                composerView.Properties.Should().NotBeEmpty();
            }
        }

        private static void EditView(string composerViewItemId, string _entityId)
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "EditView", "EditView", composerViewItemId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "MyStringProperty",
                        Value = string.Empty
                    },
                    new ViewProperty
                    {
                        Name = "MyDecimalProperty",
                        Value = string.Empty
                    },
                    new ViewProperty
                    {
                        Name = "MyIntProperty",
                        Value = "asd"
                    },
                    new ViewProperty
                    {
                        Name = "MyBoolProperty",
                        Value = null
                    },
                    new ViewProperty
                    {
                        Name = "MyDateProperty",
                        Value = string.Empty
                    },
                    version
                };
                var result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "MyStringProperty",
                        Value = "value"
                    },
                    new ViewProperty
                    {
                        Name = "MyDecimalProperty",
                        Value = "3.5"
                    },
                    new ViewProperty
                    {
                        Name = "MyIntProperty",
                        Value = "3"
                    },
                    new ViewProperty
                    {
                        Name = "MyBoolProperty",
                        Value = "true"
                    },
                    new ViewProperty
                    {
                        Name = "MyDateProperty",
                        Value = "2018-02-23T14:14:09.404Z"
                    },
                    version
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                var masterView = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "Master", string.Empty, string.Empty));
                masterView.Should().NotBeNull();
                var composerView = masterView.ChildViews.OfType<EntityView>()
                    .FirstOrDefault(v => v.Name.Equals(_composerViewName));
                composerView.Should().NotBeNull();
                composerView.Properties.Should().NotBeEmpty();
                composerView.Properties.All(p => p.Value != string.Empty).Should().BeTrue();
            }
        }

        private static void AddMinMaxPropertyConstraint(string composerViewItemId, string _entityId)
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(
                        _entityId,
                        "AddMinMaxPropertyConstraint",
                        "AddMinMaxPropertyConstraint",
                        composerViewItemId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Property",
                        Value = string.Empty
                    },
                    new ViewProperty
                    {
                        Name = "Minimum",
                        Value = string.Empty
                    },
                    new ViewProperty
                    {
                        Name = "Maximum",
                        Value = "asd"
                    },
                    version
                };
                var result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Property",
                        Value = "MyDateProperty"
                    },
                    new ViewProperty
                    {
                        Name = "Minimum",
                        Value = "0"
                    },
                    new ViewProperty
                    {
                        Name = "Maximum",
                        Value = "20"
                    },
                    version
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Property",
                        Value = "MyIntProperty"
                    },
                    new ViewProperty
                    {
                        Name = "Minimum",
                        Value = "0"
                    },
                    new ViewProperty
                    {
                        Name = "Maximum",
                        Value = "20"
                    },
                    version
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void AddSelectionOptionPropertyConstraint(string composerViewItemId, string _entityId)
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(
                        _entityId,
                        "AddSelectionOptionPropertyConstraint",
                        "AddSelectionOptionPropertyConstraint",
                        composerViewItemId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.ChildViews.Should().NotBeEmpty();
                view.Properties.Should().NotBeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Property",
                        Value = string.Empty
                    },
                    version
                };
                var result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Property",
                        Value = "MyStringProperty"
                    },
                    version
                };
                view.ChildViews = new ObservableCollection<Model>
                {
                    new EntityView
                    {
                        Name = "Cell",
                        Properties = new ObservableCollection<ViewProperty>
                        {
                            new ViewProperty
                            {
                                Name = "OptionValue",
                                Value = "Value1"
                            },
                            new ViewProperty
                            {
                                Name = "OptionName",
                                Value = "Value 1"
                            }
                        }
                    },
                    new EntityView
                    {
                        Name = "Cell",
                        Properties = new ObservableCollection<ViewProperty>
                        {
                            new ViewProperty
                            {
                                Name = "OptionValue",
                                Value = "Value2"
                            },
                            new ViewProperty
                            {
                                Name = "OptionName",
                                Value = "Value 2"
                            }
                        }
                    }
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void RemoveProperty(string composerViewItemId, string _entityId)
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(
                        _entityId,
                        "RemoveProperty",
                        "RemoveProperty",
                        composerViewItemId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Property",
                        Value = string.Empty
                    },
                    version
                };
                var result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Property",
                        Value = "MyDecimalProperty"
                    },
                    version
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                var masterView = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "Master", string.Empty, string.Empty));
                masterView.Should().NotBeNull();
                var composerView = masterView.ChildViews.OfType<EntityView>()
                    .FirstOrDefault(v => v.Name.Equals(_composerViewName));
                composerView.Should().NotBeNull();
                composerView.Properties.Should().NotBeEmpty();
                composerView.Properties.Any(p => p.Name.Equals("MyDecimalProperty")).Should().BeFalse();
            }
        }

        private static void MakeTemplate(string composerViewItemId)
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "MakeTemplate", "MakeTemplate", composerViewItemId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = string.Empty
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = string.Empty
                    },
                    version
                };
                var result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = _templateName
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = "My Console Template"
                    },
                    version
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                var templatesView = Proxy.GetValue(
                    _authoringContainer.GetEntityView(string.Empty, "ComposerTemplates", string.Empty, string.Empty));
                templatesView.Should().NotBeNull();
                templatesView.Policies.Should().NotBeEmpty();
                templatesView.Properties.Should().BeEmpty();
                templatesView.ChildViews.Should().NotBeEmpty();
                templatesView.ChildViews.OfType<EntityView>().Any(v => v.EntityId.Equals(_templateId)).Should().BeTrue();
            }
        }

        private static void RemoveView(string composerViewItemId, string _entityId)
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, string.Empty, "RemoveView", composerViewItemId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();

                var result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                var masterView = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_entityId, "Master", string.Empty, string.Empty));
                masterView.Should().NotBeNull();
                masterView.ChildViews.OfType<EntityView>().Any(v => v.Name.Equals(_composerViewName)).Should().BeFalse();
            }
        }

        private static string GetTemplateViews()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_templateId, "Master", string.Empty, string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().NotBeEmpty();
                foreach (var childView in view.ChildViews.OfType<EntityView>())
                {
                    childView.Name.Should().BeOneOf("Details", _composerViewName);
                    childView.EntityId.Should().Be(_templateId);

                    if (childView.Name.Equals("Details"))
                    {
                        childView.ItemId.Should().BeNullOrEmpty();
                    }

                    if (childView.Name.Equals(_composerViewName))
                    {
                        childView.ItemId.Should().NotBeNullOrEmpty();
                    }

                    childView.Policies.Should().NotBeEmpty();
                    childView.ChildViews.Should().BeEmpty();
                }

                view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_templateId, "Details", string.Empty, string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().NotBeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().NotBeEmpty();
                return view.ChildViews.OfType<EntityView>().FirstOrDefault()?.ItemId;
            }
        }

        private static void ManageTemplateTags()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(
                        _templateId,
                        "ManageTemplateTags",
                        "ManageTemplateTags",
                        string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Tags",
                        Value = "['Tag1','Tag2','Tag3']"
                    },
                    version
                };
                var result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(
                        _templateId,
                        "ManageTemplateTags",
                        "ManageTemplateTags",
                        string.Empty));
                version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Tags",
                        Value = "['Tag1','Tag2']"
                    },
                    version
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_templateId, "Details", string.Empty, string.Empty));
                view.Should().NotBeNull();
                view.Properties.FirstOrDefault(p => p.Name.Equals("Tags"))
                    ?.Value?.Should()
                    .Be("[\r\n  \"Tag1\",\r\n  \"Tag2\"\r\n]");
            }
        }

        private static void LinkTemplateToEntities()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(
                        _templateId,
                        "LinkTemplateToEntities",
                        "LinkTemplateToEntities",
                        string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();

                var catalogProperty =
                    view.Properties.FirstOrDefault(p => p.Name.Equals("Sitecore.Commerce.Plugin.Catalog.Catalog"));
                catalogProperty.Should().NotBeNull();
                catalogProperty.Value = "true";
                var result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(
                        _templateId,
                        "LinkTemplateToEntities",
                        "LinkTemplateToEntities",
                        string.Empty));
                catalogProperty =
                    view.Properties.FirstOrDefault(p => p.Name.Equals("Sitecore.Commerce.Plugin.Catalog.Catalog"));
                catalogProperty.Should().NotBeNull();
                catalogProperty.Value = "false";
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(_templateId, "Details", string.Empty, string.Empty));
                view.Should().NotBeNull();
                view.Properties.FirstOrDefault(p => p.Name.Equals("LinkedEntities"))?.Value?.Should().Be("[]");
            }
        }

        private static void RemoveTemplate()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(string.Empty, string.Empty, "RemoveTemplate", _templateId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();

                var result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                var templatesView = Proxy.GetValue(
                    _authoringContainer.GetEntityView(string.Empty, "ComposerTemplates", string.Empty, string.Empty));
                templatesView.Should().NotBeNull();
                templatesView.ChildViews.OfType<EntityView>().Any(v => v.ItemId.Equals(_templateId)).Should().BeFalse();
            }
        }

        private static void CreateTemplate()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    _authoringContainer.GetEntityView(string.Empty, "CreateTemplate", "CreateTemplate", string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = string.Empty
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = string.Empty
                    }
                };
                var result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                var partial = $"{Guid.NewGuid():N}".Substring(0, 5);
                var templateName = $"MyConsoleTemplate{partial}";
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = templateName
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = $"My Console Template {partial}"
                    },
                    new ViewProperty
                    {
                        Name = "ViewName",
                        Value = $"MyConsoleView{partial}"
                    },
                    new ViewProperty
                    {
                        Name = "ViewDisplayName",
                        Value = $"My Console View {partial}"
                    }
                };
                result = Proxy.DoCommand(_authoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                var templatesView = Proxy.GetValue(
                    _authoringContainer.GetEntityView(string.Empty, "ComposerTemplates", string.Empty, string.Empty));
                templatesView.Should().NotBeNull();
                templatesView.ChildViews.OfType<EntityView>().Any(v => v.ItemId.Equals(templateName.ToEntityId<ComposerTemplate>())).Should().BeTrue();
            }
        }
    }
}
