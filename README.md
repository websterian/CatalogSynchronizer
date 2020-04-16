# Catalog Synchronizer
Establishes an integration pattern for synchronizing Sitecore commerce catalogs, categories and products easily
## Purpose

Almost all Sitecore commerce projects require a custom process to be developed to import catalogs, categories and products.

Each Sitecore commerce project is left to design and implement their own catalog integration and migration process, this leads to inconsistencies across projects, sub optimal performance and high cost of ownership.

This project has been created to establish a design pattern and provide sample code in the form of a plugin. Once the plugin is added to your commerce engine project, all API, models and other infrastructure is established to quickly and easily migrate all entities related to the catalog.

This project attempts to resolve some of the challenges seen on Sitecore commerce projects by: -

- Decreasing the amount of plumbing code that needs to be created for every project.
- Allowing the same code to be reused for both catalog migration and integration, therefore creating better data consistency.
- Providing a pattern for the &quot;Pulling&quot; of a catalog into Sitecore Commerce using a minion
- Providing a pattern for the &quot;Pushing&quot; of a catalog into Sitecore Commerce using a third-party solution or custom code, this is achieved by exposing a simplified API.
- Provide some example code that meets some of the most common integration scenarios

Although this plugin has been created based on requirements from many Sitecore commerce projects over the last few years it will by no means meet everyone needs, that is not the intention.

We encourage you to add features to this project so it can be leveraged by others in the community.

## Features

- A set of models that simplify the more complex SellableItem (and child variant component), category and catalog components within Sitecore commerce. This allows the inputs for the API and batch processes to be more easily understood and generated more efficiently programmatically.
- A main class that handles all catalog synchronization logic regardless if it&#39;s called from a minion, API or any other process.
- An API action that accepts input in the form of a collection of simplified catalog models, this API can be called easily from anywhere to import catalogs, categories, products or variants. For example, postman, Azure Functions, MuleSoft etc. This API can be use instead of &quot;DoActions&quot; and other clumsy mechanism exposed through the OOB API.
- An example minion that establishes a pattern on how to import catalogs asynchronously from a simple flat CSV file. To change the process so the catalog is pulled from another source, only a few blocks need to be replaced and some settings established in the policy file.
- Updates, creates and deletes are supported for SellableItems (Products) and Variants. For catalogs and categories, updates and creates are supported but for deletes they are marked as &quot;purged&quot; and the relevant minion must be run to clean them up.
- A pattern is established to skip certain operations in the process, this option is available for the creation of the relationships between entities currently as it is the least performant and may not be necessary after the catalog structure is established e.g. in an ongoing catalog update scenario.
- Partial updates are supported, in other words you don&#39;t have to pass through the entire entity each time. This is achieved by checking if the field is null in order to determine if the destination field should be replaced.
- Set of postman scripts to get started and test the process.
- The new pipelines in 9.3 have been used to persist more than one entity at a time.
- Setting of Tags is supported
- Setting of List prices is supported for Products and Variants

## Future features

- Support for price cards
- Support for inventory

## Installing\Running

The main plugin for 9.3 is here

### To run from visual studio

This repo is a full copy of the Commerce SDK

1. Download or clones the repo
2. Go to &quot;9.3\Sitecore.Commerce.Engine.SDK.5.0.76&quot;
3. Open Customer.Sample.Solution930.sln
4. Set the &quot;Sitecore.Commerce.Engine&quot; project as the startup project.
5. Change all the configuration files to use your databases
6. Hit start.
7. For more information see the standard Sitecore commerce documents for setting up a developer machine.

### To setup postman collection

1. &quot;9.3\Sitecore.Commerce.Engine.SDK.5.0.76\src\Sitecore.Services.Examples.SynchronizeCatalog\Sitecore.Services.Examples.SynchronizeCatalog&quot;
2. Import Catalog Synchronization.postman\_collection.json into postman.

#### To run a test catalog import

1. Import the collection as described above
2. Go to the collection named &quot;Catalog Synchronization&quot;
3. Run one of the requests e.g. UpdateCreateCatalogCategoryProductsAndVariants

#### To run the minion

1. Import the collection as described above
2. Go to the collection named &quot;Catalog Synchronization&quot;
3. Run the &quot;Run SynchronizeCatalogMinion&quot; request
