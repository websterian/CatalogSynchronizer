using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http.OData;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Sitecore.Commerce.Core;
using Sitecore.Services.Examples.SynchronizeCatalog.Commands;
using Sitecore.Services.Examples.SynchronizeCatalog.Models;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Arguments;
using Catalog = Sitecore.Services.Examples.SynchronizeCatalog.Models.Catalog;
using Category = Sitecore.Services.Examples.SynchronizeCatalog.Models.Category;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Controllers
{
    public class ApiController : CommerceController
    {
        private readonly CommerceCommander _commander;

        public ApiController(IServiceProvider serviceProvider, CommerceEnvironment globalEnvironment, CommerceCommander commerceCommander)
            : base(serviceProvider, globalEnvironment)
        {
            _commander = commerceCommander;
        }

        [HttpPut]
        [Route("SynchronizeCatalog()")]
        public async Task<IActionResult> SynchronizeCatalog([FromBody] ODataActionParameters value)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(ModelState);
            }

            var arg = new SynchronizeCatalogArgument();

            if (value.ContainsKey("options"))
            {
                var optionsArray = (JArray)value["options"];
                var options = optionsArray.ToObject<List<Options>>();
                arg.Options = options.FirstOrDefault();
            }

            if (value.ContainsKey("products"))
            {
                var productsArray = (JArray)value["products"];
                var products = productsArray.ToObject<List<Product>>();
                arg.Products = products;
            }

            if (value.ContainsKey("variants"))
            {
                var variantsArray = (JArray)value["variants"];
                var variants = variantsArray.ToObject<List<Variant>>();
                arg.Variants = variants;
            }

            if (value.ContainsKey("catalogs"))
            {
                var catalogsArray = (JArray)value["catalogs"];
                var catalogs = catalogsArray.ToObject<List<Catalog>>();
                arg.Catalogs = catalogs;
            }


            if (value.ContainsKey("categories"))
            {
                var categoriesArray = (JArray)value["categories"];
                var categories = categoriesArray.ToObject<List<Category>>();
                arg.Categories = categories;
            }

            var result = await _commander.Command<SynchronizeCatalogCommand>().Process(CurrentContext, arg).ConfigureAwait(false);

            return new ObjectResult(result);
        }
    }
}

