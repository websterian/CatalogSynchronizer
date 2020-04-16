// © 2019 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Services.Examples.SynchronizeCatalog.Models;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines;
using Sitecore.Services.Examples.SynchronizeCatalog.Pipelines.Arguments;

namespace Sitecore.Services.Examples.SynchronizeCatalog.Commands
{
    public class SynchronizeCatalogCommand : CommerceCommand
    {
        private readonly CommerceCommander _commander;

        public SynchronizeCatalogCommand(CommerceCommander commander, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _commander = commander;
        }

        public async Task<SynchronizeCatalogResult> Process(CommerceContext commerceContext, SynchronizeCatalogArgument arg)
        {
            using (var activity = CommandActivity.Start(commerceContext, this))
            {
                var result = await _commander.Pipeline<ISynchronizeCatalogPipeline>().Run(arg, new CommercePipelineExecutionContextOptions(commerceContext)).ConfigureAwait(false);

                return result;
            }
        }
    }
}
