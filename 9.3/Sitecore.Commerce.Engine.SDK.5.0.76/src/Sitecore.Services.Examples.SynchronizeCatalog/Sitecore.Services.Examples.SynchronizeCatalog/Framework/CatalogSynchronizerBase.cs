using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.ManagedLists;
using Sitecore.Services.Examples.SynchronizeCatalog.Models;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Arguments;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Framework
{
    public abstract class CatalogSynchronizerBase
    {
        private SynchronizeCatalogResult _synchronizeCatalogResult;
        private readonly CommerceCommander _commerceCommander;
        private SynchronizeCatalogArgument _synchronizeCatalogArgument;
        private readonly CommercePipelineExecutionContext _context;

        protected CatalogSynchronizerBase(CommerceCommander commerceCommander, CommercePipelineExecutionContext context)
        {
            _commerceCommander = commerceCommander;
            _context = context;
        }

        public abstract void SetProductCustomFields(Product sourceProduct, SellableItem destinationProduct);
        public abstract void SetVariantCustomFields(Variant sourceVariant, ItemVariationComponent destinationVariant);

        public async Task<SynchronizeCatalogResult> Run(SynchronizeCatalogArgument arg)
        {
            _synchronizeCatalogArgument = arg;
            _synchronizeCatalogResult = new SynchronizeCatalogResult();
            Log("Starting Simple Catalog Import Service Run");

            await ImportProductsAndVariants().ConfigureAwait(false);
            await ImportCatalogs().ConfigureAwait(false);
            await ImportCategories().ConfigureAwait(false);

            if(arg.Options.SkipRelationships == false)
                await ImportRelationships().ConfigureAwait(false);

            Log("Ending Simple Catalog Import Service Run");
            return _synchronizeCatalogResult;
        }

        private void Log(string message)
        {
            if (_synchronizeCatalogArgument.Options.ExcludeLogInResults)
                return;

            message = DateTime.UtcNow + " : " + message;
            _synchronizeCatalogResult.LogMessages.Add(message);
        }

        private async Task ImportRelationships()
        {
            foreach (var sourceCategory in _synchronizeCatalogArgument.Categories.FindAll(x =>
                x.Operation.ToLower() != "delete" && 
                string.IsNullOrEmpty(x.ParentId) == false &&
                string.IsNullOrEmpty(x.ParentType) == false))
            {
                var result = await _commerceCommander.Pipeline<IAssociateCategoryToParentPipeline>().Run(
                            new CatalogReferenceArgument(
                                sourceCategory.FullCatalogId,
                                sourceCategory.FullParentId, 
                                sourceCategory.FullIdWithCatalog), _context.CommerceContext.PipelineContextOptions)
                    .ConfigureAwait(false);
            }

            foreach (var sourceProduct in _synchronizeCatalogArgument.Products.FindAll(x => 
                x.Operation.ToLower() != "delete" &&
                string.IsNullOrEmpty(x.ParentId) == false &&
                string.IsNullOrEmpty(x.ParentType) == false))
            {
                var result = await _commerceCommander.Pipeline<IAssociateSellableItemToParentPipeline>().Run(
                        new CatalogReferenceArgument(
                            sourceProduct.FullCatalogId,
                            sourceProduct.FullParentId,
                            sourceProduct.FullId), _context.CommerceContext.PipelineContextOptions)
                    .ConfigureAwait(false);
            }
        }

        private async Task ImportCatalogs()
        {
            var updates = new List<CommerceEntity>();
            var adds = new List<CommerceEntity>();

            await ProcessCatalogs(updates, adds).ConfigureAwait(false);

            await PersistUpdates(updates).ConfigureAwait(false);
            await PersistAdds(adds).ConfigureAwait(false);

            _synchronizeCatalogResult.NumberOfCatalogsCreated = adds.Count();
            _synchronizeCatalogResult.NumberOfCatalogsUpdated = updates.Count();
            //Catalog aren't deleted they are just marked for purging and the minion processes it
            _synchronizeCatalogResult.NumberOfCatalogsMarkedForPurging = _synchronizeCatalogArgument.Catalogs.Count(x => x.Operation.ToLower() == "delete");
        }

        private async Task ImportCategories()
        {
            var updates = new List<CommerceEntity>();
            var adds = new List<CommerceEntity>();

            await ProcessCategories(updates, adds).ConfigureAwait(false);

            await PersistUpdates(updates).ConfigureAwait(false);
            await PersistAdds(adds).ConfigureAwait(false);

            _synchronizeCatalogResult.NumberOfCategoriesCreated = adds.Count();
            _synchronizeCatalogResult.NumberOfCategoriesUpdated = updates.Count();

            //Catalog are deleted they are just marked for purging and the minion processes it
            _synchronizeCatalogResult.NumberOfCategoriesMarkedforPurging = _synchronizeCatalogArgument.Categories.Count(x => x.Operation.ToLower() == "delete");
        }

        private async Task ImportProductsAndVariants()
        {
            var updates = new List<CommerceEntity>();
            var adds = new List<CommerceEntity>();
            var deletes = new List<CommerceEntity>();

            await ProcessSellableItemsAndVariants(deletes, updates, adds).ConfigureAwait(false);

            await PersistUpdates(updates).ConfigureAwait(false);
            await PersistAdds(adds).ConfigureAwait(false);
            await PersistDeletes(deletes).ConfigureAwait(false);

            _synchronizeCatalogResult.NumberOfProductsCreated = adds.Count();
            _synchronizeCatalogResult.NumberOfProductsUpdated = updates.Count();
            _synchronizeCatalogResult.NumberOfProductsDeleted = deletes.Count();
        }

        private async Task PersistAdds(List<CommerceEntity> adds)
        {
            if (adds.Any())
            {
                Log($"Starting {adds.First().GetType().Name} adds");
                var addArguments = new List<PersistEntityArgument>();
                adds.ForEach(i =>
                {
                    addArguments.Add(new PersistEntityArgument(i));
                });

                var items = new PersistEntitiesArgument(addArguments);
                await _commerceCommander.Pipeline<IAddEntitiesPipeline>().Run(items, _context).ConfigureAwait(false);
                Log($"Ending {adds.First().GetType().Name} adds");
            }
        }

        private async Task PersistUpdates(List<CommerceEntity> updates)
        {
            if (updates.Any())
            {
                Log($"Starting {updates.First().GetType().Name} updates");
                var updateArguments = new List<PersistEntityArgument>();
                updates.ForEach(i =>
                {
                    updateArguments.Add(new PersistEntityArgument(i));
                });

                var items = new PersistEntitiesArgument(updateArguments);
                await _commerceCommander.Pipeline<IUpdateEntitiesPipeline>().Run(items, _context).ConfigureAwait(false);
                Log($"Ending {updates.First().GetType().Name} updates");
            }
        }

        private async Task PersistDeletes(List<CommerceEntity> deletes)
        {
            if (deletes.Any())
            {
                Log($"Starting {deletes.First().GetType().Name} deletes");
                var deleteSellableItemsArguments = new List<DeleteEntityArgument>();
                deletes.ForEach(i =>
                {
                    deleteSellableItemsArguments.Add(new DeleteEntityArgument(i));
                });

                var items = new DeleteEntitiesArgument(deleteSellableItemsArguments);
                await _commerceCommander.Pipeline<IDeleteEntitiesPipeline>().Run(items, _context).ConfigureAwait(false);
                Log($"Ending {deletes.First().GetType().Name} deletes");
            }
        }

        private async Task ProcessSellableItemsAndVariants(ICollection<CommerceEntity> deletes, ICollection<CommerceEntity> updates, IList<CommerceEntity> adds)
        {
            var findEntityArguments = new List<FindEntityArgument>();

            //Find all the sellable items that will be effected by the products being imported
            _synchronizeCatalogArgument.Products.ForEach(i =>
            {
                var id = i.FullId;

                if (findEntityArguments.FirstOrDefault(x => x.EntityId == id) == null)
                    findEntityArguments.Add(new FindEntityArgument(
                        typeof(SellableItem), id));
            });

            //Find all the sellable items that will be effected by the variants being imported
            _synchronizeCatalogArgument.Variants.ForEach(i =>
            {
                var id = i.FullVariantParentId;

                if (findEntityArguments.FirstOrDefault(x => x.EntityId == id) == null)
                    findEntityArguments.Add(new FindEntityArgument(
                        typeof(SellableItem), id));
            });

            Log("Starting find of sellable items to update.");
            var findEntitiesArgument = new FindEntitiesArgument(findEntityArguments, typeof(SellableItem));
            var foundSellableItems = await _commerceCommander.Pipeline<IFindEntitiesPipeline>().Run(findEntitiesArgument, _context).ConfigureAwait(false);
            Log("Ending find of sellable items to update.");

            foreach (var sourceProduct in _synchronizeCatalogArgument.Products)
            {
                Log($"Starting processing of sellable item {sourceProduct.Id}");
                var destinationProduct = foundSellableItems.OfType<SellableItem>().FirstOrDefault(x => x.ProductId == sourceProduct.Id);

                if (destinationProduct == null)
                {
                    Log($"{sourceProduct.Id} does not exist it will be created");
                    destinationProduct = new SellableItem();
                }

                destinationProduct.ProductId = sourceProduct.Id;
                destinationProduct.Id = sourceProduct.FullId;
                destinationProduct.FriendlyId = sourceProduct.Id;

                if (sourceProduct.Operation.ToLower() == "delete")
                {
                    Log($"{sourceProduct.Id} has been added to list for deletion");
                    deletes.Add(destinationProduct);
                    continue;
                }

                if (sourceProduct.Name != null)
                {
                    Log($"{sourceProduct.Id} description has been set");
                    destinationProduct.Name = sourceProduct.Name;
                }

                if (sourceProduct.Description != null)
                {
                    Log($"{sourceProduct.Id} description has been set");
                    destinationProduct.Description = sourceProduct.Description;
                }

                if (sourceProduct.DisplayName != null)
                {
                    Log($"{sourceProduct.Id} display name has been set");
                    destinationProduct.DisplayName = sourceProduct.DisplayName;
                }

                if (sourceProduct.Brand != null)
                {
                    Log($"{sourceProduct.Id} brand has been set");
                    destinationProduct.Brand = sourceProduct.Brand;
                }

                if (sourceProduct.Manufacturer != null)
                {
                    Log($"{sourceProduct.Id} manufacturer has been set");
                    destinationProduct.Manufacturer = sourceProduct.Manufacturer;
                }

                if (sourceProduct.TypeOfGood != null)
                {
                    Log($"{sourceProduct.Id} type of good has been set");
                    destinationProduct.TypeOfGood = sourceProduct.TypeOfGood;
                }

                AddListPriceToSellableItem(sourceProduct, destinationProduct);
                AddTagsToSellableItem(sourceProduct, destinationProduct);

                SetProductCustomFields(sourceProduct, destinationProduct);

                if (destinationProduct.IsPersisted)
                {
                    updates.Add(destinationProduct);
                }
                else
                {
                    adds.Add(destinationProduct);
                }

                Log($"Ending processing of sellable item {sourceProduct.Id}");
            }

            ProcessVariants(foundSellableItems, updates, adds);
        }

        private async Task ProcessCatalogs(ICollection<CommerceEntity> updateCatalogs, ICollection<CommerceEntity> addCatalogs)
        {
            var findEntityArguments = new List<FindEntityArgument>();

            //Find all the catalogs that will be effected by the process
            _synchronizeCatalogArgument.Catalogs.ForEach(i =>
            {
                var id = i.FullId;

                if (findEntityArguments.FirstOrDefault(x => x.EntityId == id) == null)
                    findEntityArguments.Add(new FindEntityArgument(
                        typeof(Commerce.Plugin.Catalog.Catalog), id));
            });

            Log("Starting find of catalogs to update.");
            var findEntitiesArgument = new FindEntitiesArgument(findEntityArguments, typeof(Commerce.Plugin.Catalog.Catalog));
            var foundCatalogs = await _commerceCommander.Pipeline<IFindEntitiesPipeline>().Run(findEntitiesArgument, _context).ConfigureAwait(false);
            Log("Ending find of catalogs to update.");

            foreach (var sourceCatalog in _synchronizeCatalogArgument.Catalogs)
            {
                Log($"Starting processing of catalog {sourceCatalog.Id}");
                var destinationCatalog = foundCatalogs.OfType<Commerce.Plugin.Catalog.Catalog>().FirstOrDefault(x => x.Id == sourceCatalog.FullId);

                if (destinationCatalog == null)
                {
                    Log($"{sourceCatalog.Id} does not exist it will be created");
                    destinationCatalog = new Commerce.Plugin.Catalog.Catalog();
                }

                destinationCatalog.Id = sourceCatalog.FullId;
                destinationCatalog.FriendlyId = sourceCatalog.Id;
                destinationCatalog.Name = sourceCatalog.Id;

                if (sourceCatalog.DisplayName != null)
                {
                    Log($"{sourceCatalog.Id} display name has been set");
                    destinationCatalog.DisplayName = sourceCatalog.DisplayName;
                }

                if (destinationCatalog.IsPersisted)
                {
                    if (sourceCatalog.Operation.ToLower() == "delete")
                    {
                        Log($"{sourceCatalog.Id} has been marked for purging, run minion to complete the process");
                        destinationCatalog.AddComponents(new PurgeCatalogsComponent());
                        destinationCatalog.GetComponent<TransientListMembershipsComponent>().Memberships.Add(_context.GetPolicy<KnownCatalogListsPolicy>().PurgeCatalogs);
                    }

                    updateCatalogs.Add(destinationCatalog);
                }
                else
                {
                    if (sourceCatalog.Operation.ToLower() != "delete")
                    {
                        var membershipsComponent = destinationCatalog.GetComponent<ListMembershipsComponent>();
                        if (membershipsComponent.Memberships.FirstOrDefault(x => x == $"{CommerceEntity.ListName<Commerce.Plugin.Catalog.Catalog>()}") == null)
                            membershipsComponent.Memberships.Add($"{CommerceEntity.ListName<Commerce.Plugin.Catalog.Catalog>()}");

                        addCatalogs.Add(destinationCatalog);
                    }
                }

                Log($"Ending processing of catalog {sourceCatalog.Id}");
            }
        }

        private async Task ProcessCategories(ICollection<CommerceEntity> updates, ICollection<CommerceEntity> adds)
        {
            var findEntityArguments = new List<FindEntityArgument>();

            //Find all the catalogs that will be effected by the process
            _synchronizeCatalogArgument.Categories.ForEach(i =>
            {
                var id = i.FullIdWithCatalog;

                if (findEntityArguments.FirstOrDefault(x => x.EntityId == id) == null)
                    findEntityArguments.Add(new FindEntityArgument(
                        typeof(Commerce.Plugin.Catalog.Category), id));
            });

            Log("Starting find of category to update.");
            var findEntitiesArgument = new FindEntitiesArgument(findEntityArguments, typeof(Commerce.Plugin.Catalog.Category));

            var foundCategories = await _commerceCommander.Pipeline<IFindEntitiesPipeline>().Run(findEntitiesArgument, _context)
                .ConfigureAwait(false);

            Log("Ending find of category to update.");

            foreach (var sourceCategory in _synchronizeCatalogArgument.Categories)
            {
                Log($"Starting processing of category {sourceCategory.IdWithCatalog()}");
                var destinationCategory = foundCategories.OfType<Commerce.Plugin.Catalog.Category>().FirstOrDefault(x =>
                    x.Id == sourceCategory.FullIdWithCatalog);

                if (destinationCategory == null)
                {
                    Log($"{sourceCategory.IdWithCatalog()} does not exist it will be created");
                    destinationCategory = new Commerce.Plugin.Catalog.Category();
                }

                destinationCategory.Id = sourceCategory.FullIdWithCatalog;
                destinationCategory.FriendlyId = sourceCategory.IdWithCatalog();

                if (sourceCategory.DisplayName != null)
                {
                    Log($"{sourceCategory.IdWithCatalog()} display name has been set");
                    destinationCategory.DisplayName = sourceCategory.DisplayName;
                }

                if (sourceCategory.Name != null)
                {
                    Log($"{sourceCategory.IdWithCatalog()} Name has been set");
                    destinationCategory.Name = sourceCategory.Name;
                }

                if (sourceCategory.Description != null)
                {
                    Log($"{sourceCategory.IdWithCatalog()} description has been set");
                    destinationCategory.Description = sourceCategory.Description;
                }

                if (destinationCategory.IsPersisted)
                {
                    if (sourceCategory.Operation.ToLower() == "delete")
                    {
                        Log($"{sourceCategory.IdWithCatalog()} has been marked for purging, run minion to complete the process");
                        destinationCategory.AddComponents(new PurgeCategoriesComponent());
                        destinationCategory.GetComponent<TransientListMembershipsComponent>().Memberships
                            .Add(_context.GetPolicy<KnownCatalogListsPolicy>().PurgeCategories);
                    }

                    updates.Add(destinationCategory);
                }
                else
                {
                    if (sourceCategory.Operation.ToLower() != "delete")
                    {
                        var membershipsComponent = destinationCategory.GetComponent<ListMembershipsComponent>();
                        if (membershipsComponent.Memberships.FirstOrDefault(x => x == $"{CommerceEntity.ListName<Commerce.Plugin.Catalog.Category>()}") == null)
                            membershipsComponent.Memberships.Add($"{CommerceEntity.ListName<Commerce.Plugin.Catalog.Category>()}");

                        adds.Add(destinationCategory);
                    }
                }

                Log($"Ending processing of category {sourceCategory.IdWithCatalog()}");
            }
        }

        private void ProcessVariants(ICollection<CommerceEntity> foundSellableItem, ICollection<CommerceEntity> updates, ICollection<CommerceEntity> adds)
        {
            foreach (var sourceVariant in _synchronizeCatalogArgument.Variants)
            {
                Log($"Starting processing of variant {sourceVariant.Id}");

                if (string.IsNullOrEmpty(sourceVariant.Id) || string.IsNullOrEmpty(sourceVariant.ParentId))
                {
                    Log($"{sourceVariant.Id} {sourceVariant.ParentId} has no parentid or id and will be skipped");
                    continue;
                }

                var destinationProduct = updates.OfType<SellableItem>().FirstOrDefault(x => x.ProductId == sourceVariant.SplitParentId);
                if (destinationProduct == null)
                {
                    destinationProduct = adds.OfType<SellableItem>().FirstOrDefault(x => x.ProductId == sourceVariant.SplitParentId);
                    if (destinationProduct == null)
                    {
                        destinationProduct = foundSellableItem.OfType<SellableItem>().FirstOrDefault(x => x.ProductId == sourceVariant.SplitParentId);
                        //Make sure we add it to the list of products that will get updated
                        if (destinationProduct != null && updates.FirstOrDefault(x => x.Id == destinationProduct.Id) == null)
                            updates.Add(destinationProduct);
                    }
                }

                //The variant in the list must be attached to a product that exists, and or is being updated or created in the same run
                if (destinationProduct == null)
                {
                    _synchronizeCatalogResult.LogMessages.Add(
                        $"The variant {sourceVariant.Id} must be attached to a product that exists, and or is being updated or created in the same run");
                    continue;
                }

                var destinationVariation = destinationProduct.GetVariation(sourceVariant.Id);

                var newComponent = false;
                if (destinationVariation == null)
                {
                    destinationVariation = new ItemVariationComponent();
                    newComponent = true;
                    Log($"The variant {sourceVariant.Id} is new and will be added");
                }

                if (sourceVariant.Operation.ToLower() == "delete")
                {
                    var variations = destinationProduct.GetComponent<ItemVariationsComponent>();
                    variations.ChildComponents.Remove(destinationVariation);
                    Log($"The variant {sourceVariant.Id} will be added to the list for deletion");
                    continue;
                }

                if (sourceVariant.Id != null)
                {
                    Log($"{sourceVariant.Id} id has been set");
                    destinationVariation.Id = sourceVariant.Id;
                }

                if (sourceVariant.Name != null)
                {
                    Log($"{sourceVariant.Id} Name has been set");
                    destinationVariation.Name = sourceVariant.Name;
                }

                if (sourceVariant.DisplayName != null)
                {
                    Log($"{sourceVariant.Id} DisplayName has been set");
                    destinationVariation.DisplayName = sourceVariant.DisplayName;
                }

                if (sourceVariant.Description != null)
                {
                    Log($"{sourceVariant.Id} Description has been set");
                    destinationVariation.Description = sourceVariant.Description;
                }

                AddTagsToVariant(sourceVariant, destinationVariation);
                AddListPriceToVariant(sourceVariant, destinationVariation);

                if (sourceVariant.Color != null)
                {
                    Log($"{sourceVariant.Id} color has been set");
                    destinationVariation.GetComponent<Sitecore.Commerce.Plugin.Catalog.DisplayPropertiesComponent>()
                            .Color =
                        sourceVariant.Color;
                }

                if (sourceVariant.Size != null)
                {
                    Log($"{sourceVariant.Id} size has been set");
                    destinationVariation.GetComponent<Sitecore.Commerce.Plugin.Catalog.DisplayPropertiesComponent>()
                            .Size =
                        sourceVariant.Size;
                }

                if (sourceVariant.Style != null)
                {
                    Log($"{sourceVariant.Id} Style has been set");
                    destinationVariation.GetComponent<Sitecore.Commerce.Plugin.Catalog.DisplayPropertiesComponent>()
                            .Style =
                        sourceVariant.Style;
                }

                if (newComponent)
                    destinationProduct.GetComponent<ItemVariationsComponent>()
                        .ChildComponents
                        .Add(destinationVariation);

                SetVariantCustomFields(sourceVariant, destinationVariation);

                Log($"Ending processing of variant {sourceVariant.Id}");
            }
        }

        private void AddListPriceToVariant(Product sourceVariant, Component destinationVariation)
        {
            if (sourceVariant.ListPriceCurrency == null)
                return;

            var pricePolicy = destinationVariation.GetPolicy<Commerce.Plugin.Pricing.ListPricingPolicy>();

            pricePolicy.RemovePrice(new Money(sourceVariant.ListPriceCurrency, sourceVariant.ListPrice));
            pricePolicy.AddPrice(new Money(sourceVariant.ListPriceCurrency, sourceVariant.ListPrice));

            Log($"{sourceVariant.Id} price has been set");
        }

        private void AddListPriceToSellableItem(Product sourceProduct, CommerceEntity destinationProduct)
        {
            if (sourceProduct.ListPriceCurrency == null)
                return;

            var pricePolicy = destinationProduct.GetPolicy<Commerce.Plugin.Pricing.ListPricingPolicy>();

            pricePolicy.RemovePrice(new Money(sourceProduct.ListPriceCurrency, sourceProduct.ListPrice));
            pricePolicy.AddPrice(new Money(sourceProduct.ListPriceCurrency, sourceProduct.ListPrice));

            Log($"{sourceProduct.Id} price has been set");
        }

        private void AddTagsToSellableItem(Product sourceProduct, SellableItem destinationProduct)
        {
            if (sourceProduct.Tags == null)
                return;

            foreach (var sourceTag in sourceProduct.Tags.Split('|'))
            {
                var destinationTag = destinationProduct.Tags.FirstOrDefault(x => x.Name == sourceTag);

                if (destinationTag == null)
                {
                    destinationProduct.Tags.Add(new Tag(sourceTag));
                }
            }

            Log($"{sourceProduct.Id} tag has been set");
        }

        private void AddTagsToVariant(Product sourceProduct, ItemVariationComponent destinationComponent)
        {
            if (sourceProduct.Tags == null)
                return;

            foreach (var sourceTag in sourceProduct.Tags?.Split('|'))
            {
                var destinationTag = destinationComponent.Tags.FirstOrDefault(x => x.Name == sourceTag);

                if (destinationTag == null)
                {
                    destinationComponent.Tags.Add(new Tag(sourceTag));
                }
            }

            Log($"{sourceProduct.Id} tag has been set");
        }

    }
}
