// © 2019 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Braintree;
using Braintree.Exceptions;
using Microsoft.Extensions.Logging;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Orders;
using Sitecore.Commerce.Plugin.Payments;
using Sitecore.Framework.Conditions;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.Payments.Braintree
{
    /// <summary>
    ///  Defines the settle order sales activities block.
    /// </summary>
    /// <seealso>
    ///     <cref>
    ///         Sitecore.Framework.Pipelines.PipelineBlock{Sitecore.Commerce.Plugin.Orders.Order,
    ///         Sitecore.Commerce.Plugin.Orders.Order, Sitecore.Commerce.Core.CommercePipelineExecutionContext}
    ///     </cref>
    /// </seealso>
    [PipelineDisplayName(PaymentsBraintreeConstants.SettleOrderSalesActivitiesBlock)]
    public class SettleOrderSalesActivitiesBlock : PipelineBlock<Order, Order, CommercePipelineExecutionContext>
    {
        private readonly CommerceCommander _commander;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettleOrderSalesActivitiesBlock"/> class.
        /// </summary>
        /// <param name="commander">The commander.</param>
        public SettleOrderSalesActivitiesBlock(CommerceCommander commander)
        {
            _commander = commander;
        }

        /// <summary>
        /// Runs the specified argument.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>The <see cref="Order"/></returns>
        public override async Task<Order> Run(Order arg, CommercePipelineExecutionContext context)
        {
            Condition.Requires(arg).IsNotNull($"{Name}: The order cannot be null.");

            if (!arg.HasComponent<FederatedPaymentComponent>()
                || !arg.Status.Equals(context.GetPolicy<KnownOrderStatusPolicy>().Released, StringComparison.OrdinalIgnoreCase))
            {
                return arg;
            }

            var knownOrderStatuses = context.GetPolicy<KnownOrderStatusPolicy>();

            var payment = arg.GetComponent<FederatedPaymentComponent>();
            var salesActivityReference = arg.SalesActivity.FirstOrDefault(sa => sa.Name.Equals(payment.Id, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(payment.TransactionId) || salesActivityReference == null)
            {
                payment.TransactionStatus = knownOrderStatuses.Problem;
                arg.Status = knownOrderStatuses.Problem;

                await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        "InvalidOrMissingPropertyValue",
                        new object[]
                        {
                            "TransactionId"
                        },
                        "Invalid or missing value for property 'TransactionId'.")
                    .ConfigureAwait(false);
                return arg;
            }

            var salesActivity = await _commander
                .GetEntity<SalesActivity>(context.CommerceContext, salesActivityReference.EntityTarget, salesActivityReference.EntityTargetUniqueId)
                .ConfigureAwait(false);
            if (salesActivity == null)
            {
                payment.TransactionStatus = knownOrderStatuses.Problem;
                arg.Status = knownOrderStatuses.Problem;

                await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        "EntityNotFound",
                        new object[]
                        {
                            salesActivityReference.EntityTarget
                        },
                        $"Entity '{salesActivityReference.EntityTarget}' was not found.")
                    .ConfigureAwait(false);
                return arg;
            }

            var knowSalesActivitiesStatus = context.GetPolicy<KnownSalesActivityStatusesPolicy>();

            if (payment.TransactionStatus.Equals(knownOrderStatuses.Problem, StringComparison.OrdinalIgnoreCase))
            {
                salesActivity.PaymentStatus = knowSalesActivitiesStatus.Problem;
            }

            await SettleSalesActivity(salesActivity, context).ConfigureAwait(false);

            payment.TransactionStatus = salesActivity.GetComponent<FederatedPaymentComponent>().TransactionStatus;
            if (salesActivity.PaymentStatus.Equals(knowSalesActivitiesStatus.Problem, StringComparison.OrdinalIgnoreCase))
            {
                arg.Status = knownOrderStatuses.Problem;
            }

            var knownSalesActivityLists = context.GetPolicy<KnownOrderListsPolicy>();
            var listToAssignTo = !salesActivity.PaymentStatus.Equals(knowSalesActivitiesStatus.Settled, StringComparison.OrdinalIgnoreCase)
                ? knownSalesActivityLists.ProblemSalesActivities
                : knownSalesActivityLists.SettledSalesActivities;
            var argument = new MoveEntitiesInListsArgument(knownSalesActivityLists.SettleSalesActivities, listToAssignTo, new List<string>
            {
                salesActivity.Id
            });
            await _commander.Pipeline<IMoveEntitiesInListsPipeline>().Run(argument, context.CommerceContext.PipelineContextOptions).ConfigureAwait(false);
            await _commander.PersistEntity(context.CommerceContext, salesActivity).ConfigureAwait(false);

            return arg;
        }

        /// <summary>
        /// Settle a sales activity
        /// </summary>
        /// <param name="salesActivity">The sales activity</param>
        /// <param name="context">The pipeline context</param>
        /// <returns>A <see cref="Task"/></returns>
        protected virtual async Task SettleSalesActivity(SalesActivity salesActivity, CommercePipelineExecutionContext context)
        {
            var knownSalesActivityStatuses = context.GetPolicy<KnownSalesActivityStatusesPolicy>();

            if (!salesActivity.PaymentStatus.Equals(knownSalesActivityStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            {
                salesActivity.PaymentStatus = knownSalesActivityStatuses.Problem;
                await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Warning,
                        "InvalidSalesActivityPaymentStatus",
                        new object[]
                        {
                            salesActivity.Id
                        },
                        $"Sales activity '{salesActivity.Id}' has an invalid payment state '{salesActivity.PaymentStatus}', payment status should be '{knownSalesActivityStatuses.Pending}'.")
                    .ConfigureAwait(false);
                return;
            }

            var payment = salesActivity.GetComponent<FederatedPaymentComponent>();
            if (string.IsNullOrEmpty(payment.TransactionId))
            {
                salesActivity.PaymentStatus = knownSalesActivityStatuses.Problem;
                await context.CommerceContext.AddMessage(
                    context.GetPolicy<KnownResultCodes>().Error,
                    "InvalidOrMissingPropertyValue",
                    new object[]
                    {
                        "TransactionId"
                    },
                    "Invalid or missing value for property 'TransactionId'.").ConfigureAwait(false);
                return;
            }

            var braintreeClientPolicy = context.GetPolicy<BraintreeClientPolicy>();
            if (!await braintreeClientPolicy.IsValid(context.CommerceContext).ConfigureAwait(false))
            {
                salesActivity.PaymentStatus = knownSalesActivityStatuses.Problem;
                return;
            }

            try
            {
                var gateway =
                    new BraintreeGateway(
                        braintreeClientPolicy.Environment,
                        braintreeClientPolicy.MerchantId,
                        braintreeClientPolicy.PublicKey,
                        braintreeClientPolicy.PrivateKey);

                var transaction = await gateway.Transaction.FindAsync(payment.TransactionId).ConfigureAwait(false);
                if (transaction.Status != TransactionStatus.AUTHORIZED)
                {
                    salesActivity.PaymentStatus = knownSalesActivityStatuses.Problem;
                    await context.CommerceContext.AddMessage(
                            context.GetPolicy<KnownResultCodes>().Error,
                            "SettlePaymentFailed",
                            null,
                            $"{Name}. Can not submit transaction '{payment.TransactionId}' for settlement because its status is not Authorized. Sales Activity: {salesActivity.Id}. Order: {salesActivity.Order.EntityTarget}")
                        .ConfigureAwait(false);

                    return;
                }

                var result = await gateway.Transaction.SubmitForSettlementAsync(payment.TransactionId).ConfigureAwait(false);
                if (result.IsSuccess())
                {
                    transaction = result.Target;

                    if (transaction.Status == TransactionStatus.SUBMITTED_FOR_SETTLEMENT && braintreeClientPolicy.Environment.Equals("sandbox", StringComparison.OrdinalIgnoreCase))
                    {
                        transaction = await gateway.TestTransaction.SettleAsync(payment.TransactionId).ConfigureAwait(false);
                    }

                    switch (transaction.Status.ToString())
                    {
                        case "submitted_for_settlement":
                        case "settling":
                        case "settled":
                            salesActivity.PaymentStatus = knownSalesActivityStatuses.Settled;
                            context.Logger.LogInformation($"{Name}: Sales Activity '{salesActivity.Id}' was settled. Environment: {context.CommerceContext.Environment.Name}");
                            break;
                        default:
                            salesActivity.PaymentStatus = knownSalesActivityStatuses.Problem;
                            await context.CommerceContext.AddMessage(
                                    context.GetPolicy<KnownResultCodes>().Error,
                                    "SettlePaymentFailed",
                                    null,
                                    $"{Name}. Settle payment failed for '{payment.TransactionId}': {transaction.ProcessorResponseText}. Transaction's status is not settle or settling. Sales Activity: {salesActivity.Id}. Order: {salesActivity.Order.EntityTarget}")
                                .ConfigureAwait(false);
                            break;
                    }

                    payment.TransactionStatus = transaction.Status.ToString();
                }
                else
                {
                    salesActivity.PaymentStatus = knownSalesActivityStatuses.Problem;
                    var errorMessages = result.Errors.DeepAll().Aggregate(string.Empty, (current, error) => current + ("Error: " + (int) error.Code + " - " + error.Message + "\n"));
                    await context.CommerceContext.AddMessage(
                            context.GetPolicy<KnownResultCodes>().Error,
                            "SettlePaymentFailed",
                            null,
                            $"{Name}. Transaction '{payment.TransactionId}' failed to submit for settlement: {errorMessages}. Sales Activity: {salesActivity.Id}. Order: {salesActivity.Order.EntityTarget}")
                        .ConfigureAwait(false);
                }
            }
            catch (NotFoundException ex)
            {
                salesActivity.PaymentStatus = knownSalesActivityStatuses.Problem;
                await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        "SettlePaymentFailed",
                        new object[]
                        {
                            ex
                        },
                        $"{Name}. Transaction '{payment.TransactionId}' not found. Sales Activity: {salesActivity.Id}. Order: {salesActivity.Order.EntityTarget}")
                    .ConfigureAwait(false);
            }

            catch (NullReferenceException ex)
            {
                salesActivity.PaymentStatus = knownSalesActivityStatuses.Problem;
                await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        "SettlePaymentFailed",
                        new object[]
                        {
                            ex
                        },
                        $"{Name}. Transaction '{payment.TransactionId}' set. Sales Activity: {salesActivity.Id}. Order: {salesActivity.Order.EntityTarget}")
                    .ConfigureAwait(false);
            }
            catch (BraintreeException ex)
            {
                salesActivity.PaymentStatus = knownSalesActivityStatuses.Problem;
                await context.CommerceContext.AddMessage(
                        context.GetPolicy<KnownResultCodes>().Error,
                        "SettlePaymentFailed",
                        new object[]
                        {
                            ex
                        },
                        $"{Name}. A Braintree exception occurred when trying to settle transaction '{payment.TransactionId}'. Sales Activity: {salesActivity.Id}. Order: {salesActivity.Order.EntityTarget}")
                    .ConfigureAwait(false);
            }
        }
    }
}
