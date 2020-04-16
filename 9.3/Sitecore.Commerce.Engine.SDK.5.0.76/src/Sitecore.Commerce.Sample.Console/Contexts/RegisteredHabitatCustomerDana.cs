using System;
using System.Collections.Generic;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Fulfillment;
using Sitecore.Commerce.Plugin.Payments;
using Sitecore.Commerce.Sample.Console;
using Sitecore.Commerce.Sample.Scenarios;

namespace Sitecore.Commerce.Sample.Contexts
{
    public class RegisteredHabitatCustomerDana
    {
        public RegisteredHabitatCustomerDana()
        {
            Context = new ShopperContext
            {
                Shop = Program.DefaultStorefront,
                ShopperId = "HabitatShopperDanaId",
                Environment = EnvironmentConstants.HabitatShops,
                Language = "en-US",
                Currency = "USD",
                PolicyKeys = "ZeroMinionDelay|xActivityPerf",
                EffectiveDate = DateTimeOffset.Now,
                CustomerId = "HabitatCustomerDanaId",
                IsRegistered = true,
                Components = new List<Component>
                {
                    new PhysicalFulfillmentComponent
                    {
                        ShippingParty = new Party
                        {
                            FirstName = "Dana",
                            LastName = "Santos",
                            AddressName = "FulfillmentPartyName",
                            Address1 = "655 W Herndon Ave",
                            City = "Clovis",
                            StateCode = "WA",
                            State = "Washington",
                            Country = "United States",
                            CountryCode = "US",
                            ZipPostalCode = "93612"
                        },
                        FulfillmentMethod = new EntityReference
                        {
                            EntityTarget = "B146622D-DC86-48A3-B72A-05EE8FFD187A",
                            Name = "Ground"
                        }
                    },
                    new FederatedPaymentComponent
                    {
                        PaymentMethodNonce = "fake-valid-nonce",
                        BillingParty = new Party
                        {
                            FirstName = "Dana",
                            LastName = "Santos",
                            AddressName = "PaymentPartyName",
                            Address1 = "655 W Herndon Ave",
                            City = "Clovis",
                            State = "WA",
                            Country = "US",
                            ZipPostalCode = "93612"
                        },
                        PaymentMethod = new EntityReference
                        {
                            EntityTarget = "0CFFAB11-2674-4A18-AB04-228B1F8A1DEC",
                            Name = "Federated"
                        }
                    },
                    new ElectronicFulfillmentComponent
                    {
                        FulfillmentMethod = new EntityReference
                        {
                            EntityTarget = "8A23234F-8163-4609-BD32-32D9DD6E32F5",
                            Name = "Email"
                        },
                        EmailAddress = "danahab@domain.com",
                        EmailContent = "this is the content of the email"
                    }
                }
            };
        }

        public ShopperContext Context { get; set; }

        public void GoShopping()
        {
            BuyPhone.Run(Context);
            BuyFridgeAndWarranty.Run(Context);
            BuyAllDigitals.Run(Context, 1);
            BuyGameSystemAndSubscription.Run(Context);
            BuyCameraAndGiftWrap.Run(Context);
        }
    }
}
