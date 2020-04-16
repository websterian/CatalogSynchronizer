// © 2017 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System;

namespace Sitecore.Commerce.ServiceProxy.Exceptions
{
    /// <summary>
    /// Defines the commerce service query Single exception.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class CommerceServiceQuerySingleException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommerceServiceQuerySingleException"/> class.
        /// </summary>
        /// <param name="query">The query.</param>
        public CommerceServiceQuerySingleException(string query)
        {
            Query = query;
        }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        /// <value>
        /// The query.
        /// </value>
        public string Query { get; set; }
    }
}
