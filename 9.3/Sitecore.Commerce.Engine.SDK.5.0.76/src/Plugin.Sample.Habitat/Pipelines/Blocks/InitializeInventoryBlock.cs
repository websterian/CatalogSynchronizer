// © 2017 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Internal;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Inventory;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.Habitat
{
    /// <summary>
    /// Ensure Habitat inventory has been loaded.
    /// </summary>
    /// <seealso>
    ///     <cref>
    ///         Sitecore.Framework.Pipelines.PipelineBlock{System.String, System.String,
    ///         Sitecore.Commerce.Core.CommercePipelineExecutionContext}
    ///     </cref>
    /// </seealso>
    [PipelineDisplayName(HabitatConstants.InitializeCatalogBlock)]
    public class InitializeInventoryBlock : PipelineBlock<string, string, CommercePipelineExecutionContext>
    {
        private readonly CommerceCommander _commander;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeCatalogBlock"/> class.
        /// </summary>
        /// <param name="hostingEnvironment">The hosting environment.</param>
        /// <param name="commander">The CommerceCommander.</param>
        public InitializeInventoryBlock(
            IHostingEnvironment hostingEnvironment,
            CommerceCommander commander)
        {
            HostingEnvironment = hostingEnvironment;
            _commander = commander;
        }

        /// <summary>
        /// Gets the <see cref="IHostingEnvironment"/> implementation.
        /// </summary>
        protected IHostingEnvironment HostingEnvironment { get; }

        /// <summary>
        /// Executes the block.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public override async Task<string> Run(string arg, CommercePipelineExecutionContext context)
        {
            var artifactSet = "Environment.Habitat.Catalog-1.0";

            // Check if this environment has subscribed to this Artifact Set
            if (!context.GetPolicy<EnvironmentInitializationPolicy>().InitialArtifactSets.Contains(artifactSet))
            {
                return arg;
            }

            using (var stream = new FileStream(GetPath("Habitat_Inventory.zip"), FileMode.Open, FileAccess.Read))
            {
                var file = new FormFile(stream, 0, stream.Length, stream.Name, stream.Name);

                var argument = new ImportInventorySetsArgument(file, CatalogConstants.Replace)
                {
                    BatchSize = -1,
                    ErrorThreshold = 10
                };
                await _commander.Pipeline<IImportInventorySetsPipeline>()
                    .Run(argument, context.CommerceContext.PipelineContextOptions)
                    .ConfigureAwait(false);
            }

            return arg;
        }

        private string GetPath(string fileName)
        {
            return Path.Combine(HostingEnvironment.WebRootPath, "data", "Catalogs", fileName);
        }
    }
}
