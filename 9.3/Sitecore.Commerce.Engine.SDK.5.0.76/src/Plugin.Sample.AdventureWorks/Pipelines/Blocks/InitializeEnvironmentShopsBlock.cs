// © 2015 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.ManagedLists;
using Sitecore.Commerce.Plugin.Shops;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.AdventureWorks
{
    /// <summary>
    /// Defines a block which bootstraps shops for AdventureWorks Sample environment.
    /// </summary>
    [PipelineDisplayName(AwConstants.InitializeEnvironmentShopsBlock)]
    public class InitializeEnvironmentShopsBlock : PipelineBlock<string, string, CommercePipelineExecutionContext>
    {
        private readonly IAddEntitiesPipeline _addEntitiesPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeEnvironmentShopsBlock"/> class.
        /// </summary>
        /// <param name="addEntitiesPipeline">
        /// The add entities pipeline.
        /// </param>
        public InitializeEnvironmentShopsBlock(IAddEntitiesPipeline addEntitiesPipeline)
        {
            _addEntitiesPipeline = addEntitiesPipeline;
        }

        /// <summary>
        /// The run.
        /// </summary>
        /// <param name="arg">
        /// The argument.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override async Task<string> Run(string arg, CommercePipelineExecutionContext context)
        {
            var artifactSet = "Environment.Shops-1.0";

            //// Check if this environment has subscribed to this Artifact Set
            if (!context.GetPolicy<EnvironmentInitializationPolicy>()
                .InitialArtifactSets.Contains(artifactSet))
            {
                return arg;
            }

            context.Logger.LogInformation($"{Name}.InitializingArtifactSet: ArtifactSet={artifactSet}");

            // Default Shop Entity
            var persistEntitiesArgument = new List<PersistEntityArgument>
            {
                new PersistEntityArgument(CreateShop($"{CommerceEntity.IdPrefix<Shop>()}Storefront",
                    "Storefront", "Storefront", "Storefront")),

                new PersistEntityArgument(CreateShop($"{CommerceEntity.IdPrefix<Shop>()}AwShopCanada",
                    "AwShopCanada", "Adventure Works Canada", "AwShopCanada")),

                new PersistEntityArgument(CreateShop($"{CommerceEntity.IdPrefix<Shop>()}AwShopUsa",
                    "AwShopUsa", "Adventure Works USA", "AwShopUsa")),

                new PersistEntityArgument(CreateShop($"{CommerceEntity.IdPrefix<Shop>()}AwShopGermany",
                    "AwShopGermany", "Adventure Works Germany"))
            };

            await _addEntitiesPipeline.Run(new PersistEntitiesArgument(persistEntitiesArgument), context).ConfigureAwait(false);
            return arg;
        }

        private Shop CreateShop(string id, string name, string displayName, string friendlyId = null)
        {
            var shop = new Shop
            {
                Id = id,
                Name = name,
                DisplayName = displayName,
                FriendlyId = friendlyId
            };

            shop.AddComponents(new ListMembershipsComponent
            {
                Memberships = new List<string>
                {
                    CommerceEntity.ListName<Shop>()
                }
            });

            return shop;
        }
    }
}
