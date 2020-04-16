// © 2016 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System.Threading.Tasks;
using Braintree;
using Braintree.Exceptions;
using Sitecore.Commerce.Core;
using Sitecore.Framework.Pipelines;

namespace Plugin.Sample.Payments.Braintree
{
    /// <summary>
    ///  Defines a block which gets a payment service client tokent.
    /// </summary>
    /// <seealso>
    /// <cref>
    ///  Sitecore.Framework.Pipelines.PipelineBlock{System.String, System.String, Sitecore.Commerce.Core.CommercePipelineExecutionContext}
    ///  </cref>
    ///  </seealso>
    [PipelineDisplayName(PaymentsBraintreeConstants.GetClientTokenBlock)]
    public class GetClientTokenBlock : PipelineBlock<string, string, CommercePipelineExecutionContext>
    {
        /// <summary>
        /// Runs the specified argument.
        /// </summary>
        /// <param name="arg">The argument.</param>
        /// <param name="context">The context.</param>
        /// <returns>A client token string</returns>
        public override async Task<string> Run(string arg, CommercePipelineExecutionContext context)
        {
            var braintreeClientPolicy = context.GetPolicy<BraintreeClientPolicy>();
            if (braintreeClientPolicy == null)
            {
                await context.CommerceContext.AddMessage(
                    context.GetPolicy<KnownResultCodes>().Error,
                    "InvalidOrMissingPropertyValue",
                    new object[]
                    {
                        "BraintreeClientPolicy"
                    },
                    $"{Name}. Missing BraintreeClientPolicy").ConfigureAwait(false);
                return arg;
            }

            if (!(await braintreeClientPolicy.IsValid(context.CommerceContext).ConfigureAwait(false)))
            {
                return string.Empty;
            }

            try
            {
                var gateway = new BraintreeGateway(braintreeClientPolicy?.Environment, braintreeClientPolicy?.MerchantId, braintreeClientPolicy?.PublicKey, braintreeClientPolicy?.PrivateKey);
                var clientToken = gateway.ClientToken.Generate();
                return clientToken;
            }
            catch (BraintreeException ex)
            {
                await context.CommerceContext.AddMessage(
                    context.GetPolicy<KnownResultCodes>().Error,
                    "InvalidClientPolicy",
                    new object[]
                    {
                        "BraintreeClientPolicy",
                        ex
                    },
                    $"{Name}. Invalid BraintreeClientPolicy").ConfigureAwait(false);
                return arg;
            }
        }
    }
}
