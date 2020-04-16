// © 2016 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System.Threading.Tasks;
using Sitecore.Commerce.Core;

namespace Plugin.Sample.Payments.Braintree
{
    /// <summary>
    /// Defines the Braintree Client Policy for Payments.
    /// </summary>
    public class BraintreeClientPolicy : Policy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BraintreeClientPolicy" /> class.
        /// </summary>
        public BraintreeClientPolicy()
        {
            Environment = string.Empty;
            MerchantId = string.Empty;
            PublicKey = string.Empty;
            PrivateKey = string.Empty;
        }

        /// <summary>
        /// Gets or sets the environment.
        /// </summary>
        /// <value>
        /// The environment.
        /// </value>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the merchant identifier.
        /// </summary>
        /// <value>
        /// The merchant identifier.
        /// </value>
        public string MerchantId { get; set; }

        /// <summary>
        /// Gets or sets the public key.
        /// </summary>
        /// <value>
        /// The public key.
        /// </value>
        public string PublicKey { get; set; }

        /// <summary>
        /// Gets or sets the private key.
        /// </summary>
        /// <value>
        /// The private key.
        /// </value>
        public string PrivateKey { get; set; }

        /// <summary>
        /// Returns true if ... is valid.
        /// </summary>
        /// <param name="commerceContext">The commerce context.</param>
        /// <returns>Returns true if ... is valid.</returns>
        public async Task<bool> IsValid(CommerceContext commerceContext)
        {
            if (!string.IsNullOrEmpty(Environment)
                && !string.IsNullOrEmpty(MerchantId)
                && !string.IsNullOrEmpty(PublicKey)
                && !string.IsNullOrEmpty(PrivateKey))
            {
                return true;
            }

            await commerceContext.AddMessage(
                    commerceContext.GetPolicy<KnownResultCodes>().Error,
                    "InvalidClientPolicy",
                    null,
                    "Invalid Braintree Client Policy")
                .ConfigureAwait(false);
            return false;
        }
    }
}
