// © 2015 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.FaultInjection;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Commerce.Plugin.Payments;
using Sitecore.Framework.Configuration;
using Sitecore.Framework.Pipelines.Definitions.Extensions;

namespace Plugin.Sample.Payments.Braintree
{
    /// <summary>
    /// The payments braintree configure sitecore class
    /// </summary>
    /// <seealso cref="Sitecore.Framework.Configuration.IConfigureSitecore" />
    public class ConfigureSitecore : IConfigureSitecore
    {
        /// <summary>
        /// The configure services.
        /// </summary>
        /// <param name="services">
        /// The services.
        /// </param>
        /// 
        public void ConfigureServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.RegisterAllPipelineBlocks(assembly);

            services.Sitecore().Pipelines(config => config
                .ConfigurePipeline<IGetClientTokenPipeline>(d => { d.Add<GetClientTokenBlock>(); })
                .ConfigurePipeline<ICreateOrderPipeline>(d => { d.Add<CreateFederatedPaymentBlock>().Before<CreateOrderBlock>(); })
                .ConfigurePipeline<IReleaseOnHoldOrderPipeline>(d => { d.Add<UpdateFederatedPaymentBlock>().After<ValidateOnHoldOrderBlock>(); })
                .ConfigurePipeline<IRefundPaymentsPipeline>(d => { d.Add<RefundFederatedPaymentBlock>().Before<PrepArgumentToPersistEntityBlock<Order>>(); })
                .ConfigurePipeline<ICancelOrderPipeline>(d => { d.Add<VoidCancelOrderFederatedPaymentBlock>().After<GetPendingOrderBlock>(); })
                .ConfigurePipeline<IRunningPluginsPipeline>(c => { c.Add<RegisteredPluginBlock>().After<RunningPluginsBlock>(); })
                .ConfigurePipeline<IReleasedOrdersMinionPipeline>(c => { c.Add<SettleOrderSalesActivitiesBlock>().After<SettlePaymentFaultBlock>(); }));

            services.RegisterAllCommands(assembly);
        }
    }
}
