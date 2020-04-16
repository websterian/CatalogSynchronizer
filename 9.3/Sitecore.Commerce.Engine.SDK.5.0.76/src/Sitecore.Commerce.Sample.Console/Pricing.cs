using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Pricing
    {
        private static readonly Container Container =
            new CsrSheila().Context.AuthoringContainer();

        private static string _bookFriendlyId;
        private static Guid _bookUId;
        private static string _priceSnapshotId;
        private static string _priceTierId;
        private static string _cardFriendlyId;
        private static Guid _cardUId;
        private static string _duplicatedPriceCardFriendlyId;

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Pricing"))
            {
                AddPriceBook();
                GetPriceBook();
                EditPriceBook();

                DisassociatedCatalogFromBook();
                AssociatedCatalogToBook();
                GetBookAssociatedCatalogs();

                AddPriceCard();
                EditPriceCard();
                DuplicatePriceCard();

                AddPriceSnapshot();
                EditPriceSnapshot();
                SetSnapshotApprovalStatus();

                AddPriceTier();
                EditPriceTier();

                AddPriceSnapshotTag();

                GetPriceCard();

                RemovePriceTier();
                RemovePriceSnapshotTag();
                RemovePriceSnapshot();

                DeletePriceCard();
            }
        }

        public static PriceCard GetPriceCard(string cardFriendlyId = "")
        {
            using (new SampleMethodScope())
            {
                var friendlyId = string.IsNullOrEmpty(cardFriendlyId)
                    ? _cardFriendlyId
                    : cardFriendlyId;

                var result = Proxy.GetValue(Container.PriceCards.ByKey(friendlyId).Expand("Snapshots($expand=SnapshotComponents),Components"));

                result.Should().NotBeNull();

                return result;
            }
        }

        public static void AssociatedCatalogToBook()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(
                    Container.AssociateCatalogToPriceBook("AdventureWorksPriceBook", "Adventure Works Catalog"));
                result.Should().NotBeNull();
                result.Messages.Should().NotContainErrors();
                result.Messages.Any(m => m.Code.Equals("information", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
            }
        }

        private static void AddPriceBook()
        {
            using (new SampleMethodScope())
            {
                var partialId = $"{Guid.NewGuid():N}".Substring(0, 5);
                var bookName = $"Console{partialId}";
                var result = Proxy.DoCommand(
                    Container.AddPriceBook(
                        bookName,
                        "displayname",
                        "description",
                        "{0F65742E-317F-44B0-A4DE-EBF06209E8EE}"));
                result.Messages.Should().NotContainErrors();
                result.Messages.Should().NotContainErrors();
                var priceBookCreated = result.Models.OfType<PersistedEntityModel>().FirstOrDefault();
                _bookFriendlyId = priceBookCreated?.EntityFriendlyId;
                _bookUId = priceBookCreated.EntityUniqueId;

                result = Proxy.DoCommand(
                    Container.AddPriceBook(
                        bookName,
                        "displayname",
                        "description",
                        "{0F65742E-317F-44B0-A4DE-EBF06209E8EE}"));
                result.Messages.Any(m => m.Code.Equals("validationerror", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(
                    Container.AddPriceBook($"Console{partialId}PriceBook1", string.Empty, string.Empty, string.Empty));
                result.Messages.Should().NotContainErrors();

                result = Proxy.DoCommand(
                    Container.AddPriceBook(
                        $"Console{partialId}PriceBook2",
                        "displayname",
                        "description",
                        "{0F65742E-317F-44B0-A4DE-EBF06209E8EE}"));
                result.Messages.Should().NotContainErrors();

                result = Proxy.DoCommand(
                    Container.AddPriceBook(
                        $"Console{partialId}PriceBook3",
                        string.Empty,
                        "description",
                        "{0F65742E-317F-44B0-A4DE-EBF06209E8EE}"));
                result.Messages.Should().NotContainErrors();

                result = Proxy.DoCommand(
                    Container.AddPriceBook(
                        $"Console{partialId}PriceBook4",
                        "displayname",
                        string.Empty,
                        "{0F65742E-317F-44B0-A4DE-EBF06209E8EE}"));
                result.Messages.Should().NotContainErrors();

                result = Proxy.DoCommand(
                    Container.AddPriceBook(
                        $"Console{partialId}PriceBook5",
                        "displayname",
                        "description",
                        string.Empty));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void GetPriceBook()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(Container.PriceBooks.ByKey(_bookFriendlyId).Expand("Components"));
                result.Should().NotBeNull();
            }
        }

        private static void EditPriceBook()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(
                    Container.EditPriceBook(
                        _bookFriendlyId,
                        "edited description",
                        "edited display name",
                        "{0F65742E-317F-44B0-A4DE-EBF06209E8EE}"));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void GetBookAssociatedCatalogs()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.Execute(Container.GetPriceBookAssociatedCatalogs("AdventureWorksPriceBook"))
                    .ToList();
                result.Should().NotBeNull();
                result.Should().NotBeEmpty();
            }
        }

        private static void DisassociatedCatalogFromBook()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(
                    Container.DisassociateCatalogFromPriceBook("AdventureWorksPriceBook", "Adventure Works Catalog"));
                result.Should().NotBeNull();
                result.Messages.Should().NotContainErrors();
                result.Messages.Any(m => m.Code.Equals("information", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
            }
        }

        private static void AddPriceCard()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(
                    Container.AddPriceCard(_bookFriendlyId, "ConsolePriceCard", "displayname", "description"));
                result.Messages.Should().NotContainErrors();
                result.Models.OfType<PersistedEntityModel>().FirstOrDefault().Should().NotBeNull();
                _cardFriendlyId = result.Models.OfType<PersistedEntityModel>().FirstOrDefault()?.EntityFriendlyId;
                _cardUId = result.Models.OfType<PersistedEntityModel>().FirstOrDefault().EntityUniqueId;

                result = Proxy.DoCommand(
                    Container.AddPriceCard(_bookFriendlyId, "ConsolePriceCard", "displayname", "description"));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(
                    Container.AddPriceCard(_bookFriendlyId, "ConsolePriceCard1", string.Empty, "description"));
                result.Messages.Should().NotContainErrors();

                result = Proxy.DoCommand(
                    Container.AddPriceCard(_bookFriendlyId, "ConsolePriceCard2", "displayname", string.Empty));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void EditPriceCard()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(
                    Container.EditPriceCard(_cardFriendlyId, "edited display name", "edited description"));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void DeletePriceCard()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(Container.DeletePriceCard("InvalidCard"));
                result.Messages.Any(m => m.Code.Equals("validationerror", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(Container.DeletePriceCard(_cardFriendlyId));
                result.Messages.Should().NotContainErrors();

                result = Proxy.DoCommand(
                    Container.DeletePriceCard(_duplicatedPriceCardFriendlyId)); // DELETING CLONED CARD 
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void DuplicatePriceCard()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(Container.DuplicatePriceCard("InvalidCard", "ConsolePriceCardDuplicate"));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(Container.DuplicatePriceCard(_cardFriendlyId, "ConsolePriceCardDuplicate"));
                result.Messages.Should().NotContainErrors();
                _duplicatedPriceCardFriendlyId =
                    result.Models.OfType<PriceCardAdded>().FirstOrDefault()?.PriceCardFriendlyId;
            }
        }

        private static void AddPriceSnapshot()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(Container.AddPriceSnapshot("InvalidCard", DateTimeOffset.Now));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                var snapshotDate = DateTimeOffset.Now;
                result = Proxy.DoCommand(Container.AddPriceSnapshot(_cardFriendlyId, snapshotDate));
                result.Messages.Should().NotContainErrors();
                result.Models.OfType<PriceSnapshotAdded>().FirstOrDefault().Should().NotBeNull();
                _priceSnapshotId = result.Models.OfType<PriceSnapshotAdded>().FirstOrDefault()?.PriceSnapshotId;

                var card = GetPriceCard(_cardFriendlyId);
                var snapshot = card?.Snapshots.FirstOrDefault(s => s.Id.EndsWith(_priceSnapshotId));
                snapshot?.SnapshotComponents.OfType<ApprovalComponent>().FirstOrDefault().Should().NotBeNull();
                snapshot?.SnapshotComponents.OfType<ApprovalComponent>().FirstOrDefault()?.Status.Should().Be("Draft");
                snapshot?.BeginDate.Should().BeCloseTo(snapshotDate, 1000);

                result = Proxy.DoCommand(Container.AddPriceSnapshot(_cardFriendlyId, snapshotDate));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();
            }
        }

        private static void EditPriceSnapshot()
        {
            using (new SampleMethodScope())
            {
                var beginDate = DateTimeOffset.Now;

                var result = Proxy.DoCommand(Container.EditPriceSnapshot("InvalidCard", _priceSnapshotId, beginDate));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(Container.EditPriceSnapshot(_cardFriendlyId, "InvalidSnapshot", beginDate));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(Container.EditPriceSnapshot(_cardFriendlyId, _priceSnapshotId, beginDate));
                result.Messages.Should().NotContainErrors();
                var card = GetPriceCard(_cardFriendlyId);
                var snapshot = card?.Snapshots.FirstOrDefault(s => s.Id.EndsWith(_priceSnapshotId));
                snapshot?.BeginDate.Should().BeCloseTo(beginDate, 1000);
            }
        }

        private static void RemovePriceSnapshot()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(Container.RemovePriceSnapshot("InvalidCard", _priceSnapshotId));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(Container.RemovePriceSnapshot(_cardFriendlyId, "InvalidSnapshot"));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(Container.RemovePriceSnapshot(_cardFriendlyId, _priceSnapshotId));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void SetSnapshotApprovalStatus()
        {
            using (new SampleMethodScope())
            {
                var container = new CsrSheila().Context.ShopsContainer();

                var snapshotDate = DateTimeOffset.Now.AddDays(30);
                var result = Proxy.DoCommand(container.AddPriceSnapshot(_cardFriendlyId, snapshotDate));
                result.Messages.Should().NotContainErrors();
                result.Models.OfType<PriceSnapshotAdded>().FirstOrDefault().Should().NotBeNull();
                var snapshotId = result.Models.OfType<PriceSnapshotAdded>().FirstOrDefault()?.PriceSnapshotId;

                result = Proxy.DoCommand(Container.AddPriceTier(_cardFriendlyId, snapshotId, "USD", 3, 13));
                result.Messages.Should().NotContainErrors();
                result.Models.OfType<PriceTierAdded>().FirstOrDefault().Should().NotBeNull();

                result = Proxy.DoCommand(
                    container.SetPriceSnapshotsApprovalStatus(
                        _cardFriendlyId,
                        new List<string>
                        {
                            snapshotId
                        },
                        "ReadyForApproval",
                        "my comment"));
                result.Messages.Should().NotContainErrors();
                result.Messages.Any(m => m.Code.Equals("information", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();
            }
        }

        private static void AddPriceTier()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(Container.AddPriceTier("InvalidCard", _priceSnapshotId, "USD", 3, 13));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(Container.AddPriceTier(_cardFriendlyId, "InvalidSnapshot", "USD", 3, 13));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(Container.AddPriceTier(_cardFriendlyId, _priceSnapshotId, "USD", 3, 13));
                result.Messages.Should().NotContainErrors();
                result.Models.OfType<PriceTierAdded>().FirstOrDefault().Should().NotBeNull();
                _priceTierId = result.Models.OfType<PriceTierAdded>().FirstOrDefault()?.PriceTierId;

                result = Proxy.DoCommand(Container.AddPriceTier(_cardFriendlyId, _priceSnapshotId, "USD", 3, 13));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();
            }
        }

        private static void EditPriceTier()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(
                    Container.EditPriceTier("InvalidCard", _priceSnapshotId, _priceTierId, 13));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(Container.EditPriceTier(_cardFriendlyId, "InvalidSnapshot", _priceTierId, 13));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(
                    Container.EditPriceTier(_cardFriendlyId, _priceSnapshotId, "InvalidTiers", 13));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(Container.EditPriceTier(_cardFriendlyId, _priceSnapshotId, _priceTierId, 13));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void RemovePriceTier()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(
                    Container.RemovePriceTier(_cardFriendlyId, _priceSnapshotId, "InvalidTier"));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(Container.RemovePriceTier(_cardFriendlyId, "InvalidSnapshot", _priceTierId));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(Container.RemovePriceTier(_cardFriendlyId, _priceSnapshotId, _priceTierId));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void AddPriceSnapshotTag()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(
                    Container.AddPriceSnapshotTag(_cardFriendlyId, _priceSnapshotId, "ThisIsATag"));
                result.Messages.Should().NotContainErrors();

                result = Proxy.DoCommand(
                    Container.AddPriceSnapshotTag(_cardFriendlyId, _priceSnapshotId, "ThisIsATag"));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(Container.AddPriceSnapshotTag("InvalidCard", _priceSnapshotId, "AnotherTag"));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(
                    Container.AddPriceSnapshotTag(_cardFriendlyId, "InvalidSnapshot", "AnotherTag"));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();
            }
        }

        private static void RemovePriceSnapshotTag()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(
                    Container.RemovePriceSnapshotTag("InvalidCard", _priceSnapshotId, "ThisIsATag"));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(
                    Container.RemovePriceSnapshotTag(_cardFriendlyId, "InvalidSnapshot", "ThisIsATag"));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(
                    Container.RemovePriceSnapshotTag(_cardFriendlyId, _priceSnapshotId, "InvalidTag"));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                result = Proxy.DoCommand(
                    Container.RemovePriceSnapshotTag(_cardFriendlyId, _priceSnapshotId, "ThisIsATag"));
                result.Messages.Should().NotContainErrors();
            }
        }
    }
}
