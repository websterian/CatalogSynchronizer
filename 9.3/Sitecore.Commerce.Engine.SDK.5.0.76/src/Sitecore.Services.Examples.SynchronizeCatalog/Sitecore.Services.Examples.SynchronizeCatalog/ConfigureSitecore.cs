// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigureSitecore.cs" company="Sitecore Corporation">
//   Copyright (c) Sitecore Corporation 1999-2017
// </copyright>
// <summary>
//   The SamplePlugin startup class.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Configuration;
using Sitecore.Framework.Pipelines.Definitions.Extensions;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Blocks;

namespace Sitecore.Services.Examples.SynchronizeCatalog
{
    /// <summary>
    /// The carts configure sitecore class.
    /// </summary>
    public class ConfigureSitecore : IConfigureSitecore
    {
        /// <summary>
        /// The configure services.
        /// </summary>
        /// <param name="services">
        /// The services.
        /// </param>
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);
            services.RegisterAllCommands(assembly);

            services.Sitecore().Pipelines(config => config
                .AddPipeline<ISynchronizeCatalogMinionPipeline, SynchronizeCatalogMinionPipeline>(
                    configure => { configure.Add<GetModelsFromFileBlock>().Add<SynchronizeCatalogBlock>(); })
                .AddPipeline<ISynchronizeCatalogPipeline, SynchronizeCatalogPipeline>(
                    configure => { configure.Add<SynchronizeCatalogBlock>(); })
                .ConfigurePipeline<IConfigureServiceApiPipeline>(configure => configure.Add<ConfigureServiceApiBlock>())
            );

            services.AddAutoMapper(assembly);
        }
    }
}