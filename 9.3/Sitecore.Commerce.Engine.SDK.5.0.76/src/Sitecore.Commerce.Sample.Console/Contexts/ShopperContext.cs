using System;
using System.Collections.Generic;
using Microsoft.OData.Client;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.Sample.Console;

namespace Sitecore.Commerce.Sample
{
    using CommerceOps = CommerceOps.Sitecore.Commerce.Engine;

    public class ShopperContext
    {
        private Container _shopsContainer;
        private Container _authoringContainer;

        public ShopperContext()
        {
            IsRegistered = false;
            Shop = Program.DefaultStorefront;
            ShopperId = "ConsoleShopper01";
            Environment = EnvironmentConstants.AdventureWorksShops;
            Language = "en-US";
            Currency = "USD";
            EffectiveDate = DateTimeOffset.Now;
            CustomerId = "DefaultCustomerId";
            PolicyKeys = string.Empty;
            GiftCards = new List<string>
            {
                "GC1000000",
                "GC100"
            };
        }

        public bool IsRegistered { get; set; }

        public string ShopperId { get; set; }

        public string Shop { get; set; }

        public string Language { get; set; }

        public string Currency { get; set; }

        public string Environment { get; set; }

        public string CustomerId { get; set; }

        public DateTimeOffset EffectiveDate { get; set; }

        public List<Component> Components { get; set; }

        public List<string> GiftCards { get; set; }

        public string PolicyKeys { get; set; }

        public Container ShopsContainer()
        {
            if (_shopsContainer != null)
            {
                return _shopsContainer;
            }

            _shopsContainer = new Container(new Uri(Program.ShopsServiceUri))
            {
                MergeOption = MergeOption.OverwriteChanges,
                DisableInstanceAnnotationMaterialization = true
            };

            _shopsContainer.BuildingRequest += (s, e) =>
            {
                e.Headers.Add("ShopName", Shop);
                e.Headers.Add("ShopperId", ShopperId);
                e.Headers.Add("CustomerId", CustomerId);
                e.Headers.Add("Language", Language);
                e.Headers.Add("Currency", Currency);
                e.Headers.Add("Environment", Environment);
                e.Headers.Add("PolicyKeys", PolicyKeys);
                e.Headers.Add("EffectiveDate", EffectiveDate.ToString());
                e.Headers.Add("IsRegistered", IsRegistered.ToString());
                e.Headers.Add("Authorization", Program.SitecoreToken);
            };
            return _shopsContainer;
        }

        public Container AuthoringContainer()
        {
            if (_authoringContainer != null)
            {
                return _authoringContainer;
            }

            _authoringContainer = new Container(new Uri(Program.ShopsServiceUri))
            {
                MergeOption = MergeOption.OverwriteChanges,
                DisableInstanceAnnotationMaterialization = true
            };

            _authoringContainer.BuildingRequest += (s, e) =>
            {
                e.Headers.Add("ShopName", Shop);
                e.Headers.Add("Language", Language);
                e.Headers.Add("Currency", Currency);
                e.Headers.Add("Environment", Environment);
                e.Headers.Add("PolicyKeys", PolicyKeys);
                e.Headers.Add("Authorization", Program.SitecoreToken);
            };
            return _authoringContainer;
        }

        public CommerceOps.Container OpsContainer()
        {
            var container = new CommerceOps.Container(new Uri(Program.OpsServiceUri))
            {
                MergeOption = MergeOption.OverwriteChanges,
                DisableInstanceAnnotationMaterialization = true
            };

            container.BuildingRequest += (s, e) =>
            {
                e.Headers.Add("PolicyKeys", PolicyKeys);
                e.Headers.Add("Environment", Environment);
                e.Headers.Add("Authorization", Program.SitecoreToken);
            };

            return container;
        }

        public CommerceOps.Container MinionsContainer()
        {
            var container = new CommerceOps.Container(new Uri(Program.MinionsServiceUri))
            {
                MergeOption = MergeOption.OverwriteChanges,
                DisableInstanceAnnotationMaterialization = true
            };

            container.BuildingRequest += (s, e) =>
            {
                e.Headers.Add("PolicyKeys", PolicyKeys);
                e.Headers.Add("Environment", Environment);
                e.Headers.Add("Authorization", Program.SitecoreToken);
            };

            return container;
        }
    }
}
