// © 2015 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.ManagedLists;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.AdventureWorks
{
    /// <summary>
    /// Defines a block which bootstraps an environments Regions.
    /// </summary>
    [PipelineDisplayName(AwConstants.InitializeEnvironmentRegionsBlock)]
    public class InitializeEnvironmentRegionsBlock : PipelineBlock<string, string, CommercePipelineExecutionContext>
    {
        private readonly IAddEntitiesPipeline _addEntitiesPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeEnvironmentRegionsBlock"/> class.
        /// </summary>
        /// <param name="addEntitiesPipeline">
        /// The add entities pipeline.
        /// </param>
        public InitializeEnvironmentRegionsBlock(IAddEntitiesPipeline addEntitiesPipeline)
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
            var artifactSet = "Environment.Regions-1.0";

            //// Check if this environment has subscribed to this Artifact Set
            if (!context.GetPolicy<EnvironmentInitializationPolicy>()
                .InitialArtifactSets.Contains(artifactSet))
            {
                return arg;
            }

            context.Logger.LogInformation($"{Name}.InitializingArtifactSet: ArtifactSet={artifactSet}");
            var persistEntitiesArgument = new List<PersistEntityArgument>
            {
                new PersistEntityArgument(
                    new Country(new List<Component>
                    {
                        new ListMembershipsComponent
                        {
                            Memberships = new List<string>
                            {
                                "Countries"
                            }
                        }
                    })
                    {
                        Id = $"{CommerceEntity.IdPrefix<Country>()}USA",
                        Name = "United States",
                        IsoCode2 = "US",
                        IsoCode3 = "USA",
                        AddressFormat = "1"
                    }),

                new PersistEntityArgument(
                    new Country(new List<Component>
                    {
                        new ListMembershipsComponent
                        {
                            Memberships = new List<string>
                            {
                                "Countries"
                            }
                        }
                    })
                    {
                        Id = $"{CommerceEntity.IdPrefix<Country>()}CAN",
                        Name = "Canada",
                        IsoCode2 = "CA",
                        IsoCode3 = "CAN",
                        AddressFormat = "1"
                    }),

                new PersistEntityArgument(
                    new Country(new List<Component>
                    {
                        new ListMembershipsComponent
                        {
                            Memberships = new List<string>
                            {
                                "Countries"
                            }
                        }
                    })
                    {
                        Id = $"{CommerceEntity.IdPrefix<Country>()}DNK",
                        Name = "Denmark",
                        IsoCode2 = "DK",
                        IsoCode3 = "DNK",
                        AddressFormat = "1"
                    })
            };

            await _addEntitiesPipeline.Run(new PersistEntitiesArgument(persistEntitiesArgument), context).ConfigureAwait(false);
            return arg;
        }
    }
}
