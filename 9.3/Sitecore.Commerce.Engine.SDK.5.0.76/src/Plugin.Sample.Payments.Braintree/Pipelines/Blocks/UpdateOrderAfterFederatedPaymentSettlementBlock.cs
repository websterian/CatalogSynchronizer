// © 2017 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.Threading.Tasks;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Commerce.Plugin.Payments;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.Payments.Braintree
{
    /// <summary>
    /// Defines a block which updates an order after the federated payment has been settled.
    /// </summary>   
    /// <seealso>
    ///   <cref>
    ///         Sitecore.Framework.Pipelines.PipelineBlock{Sitecore.Commerce.Plugin.Orders.SalesActivity,
    ///         Sitecore.Commerce.Plugin.Orders.SalesActivity, Sitecore.Commerce.Core.CommercePipelineExecutionContext}
    ///   </cref>
    /// </seealso>
    [PipelineDisplayName(PaymentsBraintreeConstants.UpdateOrderAfterFederatedPaymentSettlementBlock)]
    public class UpdateOrderAfterFederatedPaymentSettlementBlock : PipelineBlock<SalesActivity, SalesActivity, CommercePipelineExecutionContext>
    {
        private readonly GetOrderCommand _getOrderCommand;
        private readonly IPersistEntityPipeline _persistEntityPipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateOrderAfterFederatedPaymentSettlementBlock"/> class.
        /// </summary>
        /// <param name="getOrderCommand">The get order command.</param>
        /// <param name="persistEntityPipeline">The persist entity pipeline.</param>
        public UpdateOrderAfterFederatedPaymentSettlementBlock(GetOrderCommand getOrderCommand, IPersistEntityPipeline persistEntityPipeline)
        {
            _getOrderCommand = getOrderCommand;
            _persistEntityPipeline = persistEntityPipeline;
        }

        /// <summary>
        /// Runs the specified argument.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// A cart with federate payment component
        /// </returns>
        public override async Task<SalesActivity> Run(SalesActivity arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: sales activity can not be null.");

            if (!arg.HasComponent<FederatedPaymentComponent>()
                || !arg.PaymentStatus.Equals(context.GetPolicy<KnownSalesActivityStatusesPolicy>().Settled, StringComparison.OrdinalIgnoreCase))
            {
                return arg;
            }

            var order = await _getOrderCommand.Process(context.CommerceContext, arg.Order.EntityTarget).ConfigureAwait(false);
            if (order == null || !order.HasComponent<FederatedPaymentComponent>())
            {
                return arg;
            }

            var orderPayment = order.GetComponent<FederatedPaymentComponent>();
            var payment = arg.GetComponent<FederatedPaymentComponent>();
            orderPayment.TransactionStatus = payment.TransactionStatus;

            await _persistEntityPipeline.Run(new PersistEntityArgument(order), context).ConfigureAwait(false);

            return arg;
        }
    }
}
