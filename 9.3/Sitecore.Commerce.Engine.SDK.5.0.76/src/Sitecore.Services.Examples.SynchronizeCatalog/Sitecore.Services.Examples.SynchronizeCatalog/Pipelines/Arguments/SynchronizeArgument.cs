// © 2019 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System.Collections.Generic;
using Sitecore.Commerce.Core;
using Sitecore.Services.Examples.SynchronizeCatalog.Models;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Arguments
{

    public class SynchronizeCatalogArgument : PipelineArgument
    {

        public List<Product> Products;
        public List<Variant> Variants;
        public List<Category> Categories;
        public List<Catalog> Catalogs;

        public Options Options;

        public SynchronizeCatalogArgument()
        {
            //Condition.Requires(parameter).IsNotNull("The parameter can not be null");

            Products = new List<Product>();
            Variants = new List<Variant>();
            Categories = new List<Category>();
            Catalogs = new List<Catalog>();
            Options = new Options();
        }
    }
}
