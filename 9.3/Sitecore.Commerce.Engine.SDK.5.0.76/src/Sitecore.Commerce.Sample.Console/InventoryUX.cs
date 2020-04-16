using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Inventory;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class InventoryUX
    {
        private const int InitialQuantity = 100;
        private const int UpdatedQuantity = 200;
        private const int BackorderLimit = 50;
        private static readonly DateTimeOffset BackorderAvailabilityDate = DateTimeOffset.UtcNow.AddDays(1);
        private static string _catalog1Name;
        private static string _catalog2Name;
        private static string _inventorySet1Name;
        private static string _inventorySet2Name;
        private static string _inventorySet3Name;
        private static string _productName;
        private static string _catalog1Id;
        private static string _catalog2Id;
        private static string _inventorySet1Id;
        private static string _inventorySet2Id;
        private static string _inventorySet3Id;
        private static string _productId;
        private static string _productInventoryInfo1Id;
        private static string _productInventoryInfo2Id;
        private static string _inventoryExportFilePath;

        public static void RunScenarios()
        {
            using (new SampleScenarioScope(nameof(InventoryUX)))
            {
                var partial = $"{Guid.NewGuid():N}".Substring(0, 3);
                _catalog1Name = $"InventoryCatalog1{partial}";
                _catalog2Name = $"InventoryCatalog2{partial}";
                _inventorySet1Name = $"InventorySet1{partial}";
                _inventorySet2Name = $"InventorySet2{partial}";
                _inventorySet3Name = $"InventorySet3{partial}";
                _productName = $"InventoryUXProduct{partial}";

                _catalog1Id = _catalog1Name.ToEntityId<Catalog>();
                _catalog2Id = _catalog2Name.ToEntityId<Catalog>();
                _productId = _productName.ToEntityId<SellableItem>();
                _inventorySet1Id = _inventorySet1Name.ToEntityId<InventorySet>();
                _inventorySet2Id = _inventorySet2Name.ToEntityId<InventorySet>();
                _inventorySet3Id = _inventorySet3Name.ToEntityId<InventorySet>();
                _productInventoryInfo1Id = _productName.ToEntityId<InventoryInformation>(_inventorySet1Name);
                _productInventoryInfo2Id = _productName.ToEntityId<InventoryInformation>(_inventorySet2Name);
                _inventoryExportFilePath = Path.Combine(Path.GetTempPath(), "consoleinventory.zip");

                EngineExtensions.AddCatalog(_catalog1Name, $"{_catalog1Name} Display Name");
                EngineExtensions.AddCatalog(_catalog2Name, $"{_catalog2Name} Display Name");
                EngineExtensions.AddSellableItem(_productId, _catalog1Id, _catalog1Name, _catalog1Name);
                EngineExtensions.AssociateSellableItem(_productId, _catalog2Id, _catalog2Name, _catalog2Name);
                AddInventorySet();
                EditInventorySet();
                AssociateCatalogToInventorySet();
                AssociateSellableItemToInventorySet();
                EditInventoryInformation1();
                EditInventoryInformation2();
                ExportInventorySetsFull().Wait();
                TransferInventoryInformation();
                DisassociateSellableItemFromInventorySet();
                DisassociateCatalogFromInventorySet();
                ImportInventorySetsReplace().Wait();

                DeleteInventorySet();
            }
        }

        private static void DisassociateCatalogFromInventorySet()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.DisassociateCatalogFromInventorySet(_inventorySet1Name, _catalog1Name);
                EngineExtensions.AssertChildViewItemNotExists(_inventorySet1Id, ChildViewNames.InventorySetSellableItems, _productId);

                EngineExtensions.DisassociateCatalogFromInventorySet(_inventorySet2Name, _catalog2Name);
                EngineExtensions.AssertChildViewItemNotExists(_inventorySet2Id, ChildViewNames.InventorySetSellableItems, _productId);
            }
        }

        private static async Task ImportInventorySetsReplace()
        {
            using (new SampleMethodScope())
            {
                await EngineExtensions.ImportInventorySets(_inventoryExportFilePath, "replace", "CatalogAlreadyAssociated");

                var inventoryInfo1 = Proxy.GetValue(EngineExtensions.AuthoringContainer.Value.InventoryInformation.ByKey(_productInventoryInfo1Id).Expand("Components($expand=ChildComponents)"));
                inventoryInfo1.Should().NotBeNull();
                inventoryInfo1.Quantity.Should().Be(UpdatedQuantity);
                var backorderComponent1 = inventoryInfo1.Components.OfType<BackorderableComponent>().FirstOrDefault();
                backorderComponent1.Should().NotBeNull();
                backorderComponent1.Backorderable.Should().BeFalse();
                backorderComponent1.BackorderAvailabilityDate.HasValue.Should().BeFalse();
                backorderComponent1.BackorderLimit.Should().Be(0);

                var inventoryInfo2 = Proxy.GetValue(EngineExtensions.AuthoringContainer.Value.InventoryInformation.ByKey(_productInventoryInfo2Id).Expand("Components($expand=ChildComponents)"));
                inventoryInfo2.Should().NotBeNull();
                inventoryInfo2.Quantity.Should().Be(UpdatedQuantity);
                var backorderComponent2 = inventoryInfo2.Components.OfType<BackorderableComponent>().FirstOrDefault();
                backorderComponent2.Should().NotBeNull();
                backorderComponent2.Backorderable.Should().BeTrue();
                backorderComponent2.BackorderAvailabilityDate.Should().BeCloseTo(BackorderAvailabilityDate, 1000);
                backorderComponent2.BackorderLimit.Should().Be(BackorderLimit);
            }
        }

        private static void DisassociateSellableItemFromInventorySet()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.DisassociateSellableItemFromInventorySet(_inventorySet1Name, _productId);
                EngineExtensions.DisassociateSellableItemFromInventorySet(_inventorySet2Name, _productId);
            }
        }

        private static void TransferInventoryInformation()
        {
            using (new SampleMethodScope())
            {
                var quantityToTransfer = 12;
                EngineExtensions.TransferInventoryInformation(_inventorySet1Id, _productId, _inventorySet2Id, _productId, quantityToTransfer);

                var inventoryInfo1 = Proxy.GetValue(EngineExtensions.AuthoringContainer.Value.InventoryInformation.ByKey(_productInventoryInfo1Id));
                inventoryInfo1.Should().NotBeNull();
                inventoryInfo1.Quantity.Should().Be(UpdatedQuantity - quantityToTransfer);

                var inventoryInfo2 = Proxy.GetValue(EngineExtensions.AuthoringContainer.Value.InventoryInformation.ByKey(_productInventoryInfo2Id));
                inventoryInfo2.Should().NotBeNull();
                inventoryInfo2.Quantity.Should().Be(UpdatedQuantity + quantityToTransfer);
            }
        }

        private static async Task ExportInventorySetsFull()
        {
            using (new SampleMethodScope())
            {
                await EngineExtensions.ExportInventorySets(_inventoryExportFilePath, "full");
            }
        }

        private static void EditInventoryInformation1()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.EditInventoryInformation(
                    _inventorySet1Name,
                    _productId,
                    new List<ViewProperty>
                    {
                        new ViewProperty
                        {
                            Name = "Quantity",
                            Value = UpdatedQuantity.ToString(),
                            OriginalType = typeof(int).FullName
                        }
                    });

                var inventoryInfo1 = Proxy.GetValue(EngineExtensions.AuthoringContainer.Value.InventoryInformation.ByKey(_productInventoryInfo1Id).Expand("Components($expand=ChildComponents)"));
                inventoryInfo1.Should().NotBeNull();
                inventoryInfo1.Quantity.Should().Be(UpdatedQuantity);
                var backorderComponent1 = inventoryInfo1.Components.OfType<BackorderableComponent>().FirstOrDefault();
                backorderComponent1.Should().NotBeNull();
                backorderComponent1.Backorderable.Should().BeFalse();
                backorderComponent1.BackorderAvailabilityDate.HasValue.Should().BeFalse();
                backorderComponent1.BackorderLimit.Should().Be(0);
            }
        }

        private static void EditInventoryInformation2()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.EditInventoryInformation(
                    _inventorySet2Name,
                    _productId,
                    new List<ViewProperty>
                    {
                        new ViewProperty
                        {
                            Name = "Quantity",
                            Value = UpdatedQuantity.ToString(),
                            OriginalType = typeof(int).FullName
                        },
                        new ViewProperty
                        {
                            Name = "Backorderable",
                            Value = "true",
                            OriginalType = typeof(bool).FullName
                        },
                        new ViewProperty
                        {
                            Name = "BackorderAvailabilityDate",
                            Value = BackorderAvailabilityDate.ToString(),
                            OriginalType = typeof(DateTimeOffset).FullName
                        },
                        new ViewProperty
                        {
                            Name = "BackorderLimit",
                            Value = BackorderLimit.ToString(),
                            OriginalType = typeof(int).FullName
                        }
                    });

                var inventoryInfo2 = Proxy.GetValue(EngineExtensions.AuthoringContainer.Value.InventoryInformation.ByKey(_productInventoryInfo2Id).Expand("Components($expand=ChildComponents)"));
                inventoryInfo2.Should().NotBeNull();
                inventoryInfo2.Quantity.Should().Be(UpdatedQuantity);
                var backorderComponent2 = inventoryInfo2.Components.OfType<BackorderableComponent>().FirstOrDefault();
                backorderComponent2.Should().NotBeNull();
                backorderComponent2.Backorderable.Should().BeTrue();
                backorderComponent2.BackorderAvailabilityDate.HasValue.Should().BeTrue();
                backorderComponent2.BackorderAvailabilityDate.Value.Should().BeCloseTo(BackorderAvailabilityDate, 1000);
                backorderComponent2.BackorderLimit.Should().Be(BackorderLimit);
            }
        }

        private static void AssociateSellableItemToInventorySet()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.AssociateSellableItemToInventorySet(_inventorySet1Name, _productId, InitialQuantity);
                EngineExtensions.AssociateSellableItemToInventorySet(_inventorySet2Name, _productId, InitialQuantity);
            }
        }

        private static void AssociateCatalogToInventorySet()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.AssociateCatalogToInventorySet(_inventorySet1Name, _catalog1Name);
                EngineExtensions.AssociateCatalogToInventorySet(_inventorySet2Name, _catalog2Name);
            }
        }

        private static void AddInventorySet()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.AddInventorySet(_inventorySet1Name);
                EngineExtensions.AddInventorySet(_inventorySet2Name);
            }
        }

        private static void DeleteInventorySet()
        {
            using (new SampleMethodScope())
            {
                EngineExtensions.AddInventorySet(_inventorySet3Name);
                EngineExtensions.DeleteInventorySet(_inventorySet3Id);
            }
        }

        private static void EditInventorySet()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(EngineExtensions.AuthoringContainer.Value.GetEntityView(_inventorySet1Id, "Details", "EditInventorySet", string.Empty));
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
                        Value = "Console UX Inventory Set (updated)"
                    },
                    new ViewProperty
                    {
                        Name = "Description",
                        Value = "Console UX Inventory Set Description"
                    },
                    version
                };

                var result = Proxy.DoCommand(EngineExtensions.AuthoringContainer.Value.DoAction(view));
                result.Messages.Should().NotContainMessageCode("error");
            }
        }
    }
}
