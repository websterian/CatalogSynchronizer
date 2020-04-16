// © 2017 Sitecore Corporation A/S. All rights reserved. Sitecore® is a registered trademark of Sitecore Corporation A/S.

using System.Collections.Generic;
using CommerceOps.Sitecore.Commerce.Core.Commands;
using CommerceOps.Sitecore.Commerce.Engine;
using Microsoft.OData.Client;
using Sitecore.Commerce.Plugin.Carts;
using Sitecore.Framework.Conditions;

namespace Sitecore.Commerce.ServiceProxy.Extensions
{
    /// <summary>
    /// Defines extension methods
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Does the ops command.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="query">The query.</param>
        /// <returns>A <see cref="CommerceCommandSingle"/></returns>
        public static CommerceCommandSingle DoOpsCommand(this Container container, DataServiceActionQuerySingle<CommerceCommandSingle> query)
        {
            Condition.Requires(query, nameof(query)).IsNotNull();

            return query.GetValue();
        }

        /// <summary>
        /// Adds a line-item to the shopping cart.
        /// </summary>
        /// <param name="container">The service container.</param>
        /// <param name="cartId">The id of the shopping cart.</param>
        /// <param name="itemId">The id of the sellable item.</param>
        /// <param name="quantity">The quantity being added.</param>
        /// <param name="expands">The expands options.</param>
        /// <returns>The <see cref="DataServiceActionQuerySingle{AddCartLineCommand}"/> query.</returns>
        public static DataServiceActionQuerySingle<AddCartLineCommand> AddCartLine(this Engine.Container container, string cartId, string itemId, decimal quantity, string expands)
        {
            return new DataServiceActionQuerySingle<AddCartLineCommand>(container, container.BaseUri.OriginalString.Trim('/') + $"/AddCartLine?$expand={expands}",
                new BodyOperationParameter("cartId", cartId),
                new BodyOperationParameter("itemId", itemId),
                new BodyOperationParameter("quantity", quantity));
        }

        /// <summary>
        /// Adds a line-item to the shopping cart. Sub-lines may be included when adding a product bundle.
        /// </summary>
        /// <param name="container">The service container.</param>
        /// <param name="cartId">The id of the shopping cart.</param>
        /// <param name="itemId">The id of the sellable item.</param>
        /// <param name="quantity">The quantity being added.</param>
        /// <param name="subLines">The product bundle sub-lines, if applicable.</param>
        /// <param name="expands">The expands options.</param>
        /// <returns>The <see cref="DataServiceActionQuerySingle{AddCartLineCommand}"/> query.</returns>
        public static DataServiceActionQuerySingle<AddCartLineCommand> AddCartLineWithSubLines(this Engine.Container container, string cartId, string itemId, decimal quantity, ICollection<CartSubLine> subLines, string expands)
        {
            return new DataServiceActionQuerySingle<AddCartLineCommand>(container, container.BaseUri.OriginalString.Trim('/') + $"/AddCartLineWithSubLines?$expand={expands}",
                new BodyOperationParameter("cartId", cartId),
                new BodyOperationParameter("itemId", itemId),
                new BodyOperationParameter("quantity", quantity),
                new BodyOperationParameter("subLines", subLines));
        }

        /// <summary>
        /// Removes a line-item from the shopping cart.
        /// </summary>
        /// <param name="container">The service container</param>
        /// <param name="cartId">The cart Id</param>
        /// <param name="cartLineId">The cart line-item Id</param>
        /// <param name="expands">The expands options</param>
        /// <returns>The <see cref="DataServiceActionQuerySingle{RemoveCartLineCommand}" /> query</returns>
        public static DataServiceActionQuerySingle<RemoveCartLineCommand> RemoveCartLine(this Engine.Container container, string cartId, string cartLineId, string expands)
        {
            return new DataServiceActionQuerySingle<RemoveCartLineCommand>(container, container.BaseUri.OriginalString.Trim('/') + $"/RemoveCartLine?$expand={expands}",
                new BodyOperationParameter("cartId", cartId),
                new BodyOperationParameter("cartLineId", cartLineId));
        }

        /// <summary>
        /// Updates the quantity of a line-item in the shopping cart.
        /// </summary>
        /// <param name="container">The service container.</param>
        /// <param name="cartId">The cart ID.</param>
        /// <param name="cartLineId">The cart line-item ID.</param>
        /// <param name="quantity">The updated quantity of the line-item.</param>
        /// <param name="expands">The expands options.</param>
        /// <returns>The <see cref="DataServiceActionQuerySingle{UpdateCartLineCommand}" /> query</returns>
        public static DataServiceActionQuerySingle<UpdateCartLineCommand> UpdateCartLine(this Engine.Container container, string cartId, string cartLineId, decimal quantity, string expands)
        {
            return new DataServiceActionQuerySingle<UpdateCartLineCommand>(container, container.BaseUri.OriginalString.Trim('/') + $"/UpdateCartLine?$expand={expands}", new BodyOperationParameter("cartId", cartId),
                new BodyOperationParameter("cartLineId", cartLineId),
                new BodyOperationParameter("quantity", quantity));
        }
    }
}
