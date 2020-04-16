using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Pricing;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class PricingUX
    {
        private static readonly Container ShopsContainer = new CsrSheila().Context.ShopsContainer();

        private static readonly Container AuthoringContainer = new AnonymousCustomerJeff(EnvironmentConstants.AdventureWorksAuthoring)
            .Context.AuthoringContainer();

        private static string _bookId;
        private static Guid _bookUId;
        private static string _snapshotId;
        private static string _cardFriendlyId;
        private static string _cardId;
        private static Guid _cardUId;

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Pricing UX"))
            {
                AddPriceBook();
                EditPriceBook();

                Books();
                BookMaster();
                BookDetails();

                DisassociateCatalog();
                AssociateCatalog();
                BookCatalogs();

                AddPriceCard();
                BookCards();
                CardMaster();
                CardDetails();
                EditPriceCard();
                DuplicatePriceCard();

                AddPriceSnapshot();
                CardSnapshots();
                EditPriceSnapshot();
                EditPriceSnapshot_WithTags();
                SetSnapshotApprovalStatus();

                AddCurrency();
                EditCurrency();
                RemoveCurrency();

                RemovePriceSnapshot();
                DeletePriceCard();
            }
        }

        private static void Books()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    ShopsContainer.GetEntityView(string.Empty, "PriceBooks", string.Empty, string.Empty));
                result.Should().NotBeNull();
                result.ChildViews.Should().NotBeEmpty();

                foreach (var childView in result.ChildViews.Cast<EntityView>())
                {
                    childView.Should().NotBeNull();
                    childView.Properties.Should().NotBeEmpty();
                    childView.Policies.Should().BeEmpty();
                    childView.ChildViews.Should().BeEmpty();
                }
            }
        }

        private static void BookMaster()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    ShopsContainer.GetEntityView(_bookId, "Master", string.Empty, string.Empty));

                result.Should().NotBeNull();
                result.Policies.Should().NotBeEmpty();
                result.Properties.Should().NotBeEmpty();
                result.ChildViews.Should().NotBeEmpty();

                foreach (var childView in result.ChildViews.Cast<EntityView>())
                {
                    childView.Should().NotBeNull();
                    childView.Policies.Should().NotBeEmpty();
                }
            }
        }

        private static void BookDetails()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    ShopsContainer.GetEntityView(_bookId, "Details", string.Empty, string.Empty));
                result.Should().NotBeNull();
                result.Policies.Should().NotBeEmpty();
                result.Properties.Should().NotBeEmpty();
                result.ChildViews.Should().BeEmpty();
            }
        }

        private static void BookCards()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(AuthoringContainer.GetEntityView(_bookId, "PriceBookCards", string.Empty, string.Empty));
                result.Should().NotBeNull();
                result.Policies.Should().NotBeEmpty();
                result.Properties.Should().NotBeEmpty();

                //result.ChildViews.Should().NotBeEmpty();

                //foreach (var childView in result.ChildViews.Cast<EntityView>())
                //{
                //    childView.Should().NotBeNull();
                //    childView.Properties.Should().NotBeEmpty();
                //    childView.Policies.Should().BeEmpty();
                //    childView.ChildViews.Should().BeEmpty();
                //}
            }
        }

        private static void BookCatalogs()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _bookId,
                        "PriceBookCatalogs",
                        string.Empty,
                        string.Empty));
                result.Should().NotBeNull();
                result.Policies.Should().NotBeEmpty();
                result.Properties.Should().NotBeEmpty();
                result.ChildViews.Should().BeEmpty();

                foreach (var childView in result.ChildViews.Cast<EntityView>())
                {
                    childView.Should().NotBeNull();
                    childView.Properties.Should().NotBeEmpty();
                    childView.Policies.Should().BeEmpty();
                    childView.ChildViews.Should().BeEmpty();
                }
            }
        }

        private static void AddPriceBook()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(string.Empty, "Details", "AddPriceBook", string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = "InvalidPriceBook{"
                    },
                };
                var result = Proxy.DoCommand(ShopsContainer.DoAction(view));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                var partialId = $"{Guid.NewGuid():N}".Substring(0, 5);
                var bookName = $"Console{partialId}";
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = bookName
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = "displayname"
                    },
                    new ViewProperty
                    {
                        Name = "Description",
                        Value = "description"
                    },
                    new ViewProperty
                    {
                        Name = "CurrencySetId",
                        Value = "{0F65742E-317F-44B0-A4DE-EBF06209E8EE}"
                    }
                };
                result = Proxy.DoCommand(ShopsContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
                var persistedModel = result.Models.OfType<PersistedEntityModel>().FirstOrDefault();
                persistedModel.Should().NotBeNull();
                _bookUId = persistedModel.EntityUniqueId;
                _bookId = persistedModel.EntityId;
            }
        }

        private static void EditPriceBook()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _bookId,
                        "Details",
                        "EditPriceBook",
                        string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Description",
                        Value = "edited description"
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = "edited display name"
                    },
                    new ViewProperty
                    {
                        Name = "CurrencySetId",
                        Value = "{0F65742E-317F-44B0-A4DE-EBF06209E8EE}"
                    },
                    version
                };
                var result = Proxy.DoCommand(ShopsContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void AssociateCatalog()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    AuthoringContainer.GetEntityView(
                        "Entity-PriceBook-AdventureWorksPriceBook",
                        "PriceBookCatalogs",
                        "AssociateCatalog",
                        string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();
                foreach (var property in view.Properties)
                {
                    property.Name.Should().NotBeNullOrEmpty();
                    if (property.Name.Equals("CatalogName"))
                    {
                        property.Policies.Should().NotBeEmpty();
                    }
                    else
                    {
                        property.Policies.Should().BeEmpty();
                    }
                }

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "CatalogName",
                        Value = "Adventure Works Catalog"
                    },
                    version
                };
                var result = Proxy.DoCommand(AuthoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void DisassociateCatalog()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        "Entity-PriceBook-AdventureWorksPriceBook",
                        string.Empty,
                        "DisassociateCatalog",
                        "Adventure Works Catalog"));
                view.Should().NotBeNull();
                view.Properties.Should().NotBeEmpty();

                var result = Proxy.DoCommand(ShopsContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void CardMaster()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        "Entity-PriceCard-AdventureWorksPriceBook-AdventureWorksPriceCard",
                        "Master",
                        string.Empty,
                        string.Empty));

                result.Should().NotBeNull();
                result.Policies.Should().NotBeEmpty();
                result.Properties.Should().NotBeEmpty();
                result.ChildViews.Should().NotBeEmpty();
            }
        }

        private static void CardSnapshots()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        "Entity-PriceCard-AdventureWorksPriceBook-AdventureWorksPriceCard",
                        "PriceCardSnapshots",
                        string.Empty,
                        string.Empty));

                result.Should().NotBeNull();
                result.EntityId.Should().NotBeNullOrEmpty();
                result.ItemId.Should().BeNullOrEmpty();
                result.Policies.Should().NotBeEmpty();
                result.Properties.Should().NotBeEmpty();
                result.ChildViews.Should().NotBeEmpty();
            }
        }

        private static void CardDetails()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        "Entity-PriceCard-AdventureWorksPriceBook-AdventureWorksPriceCard",
                        "Details",
                        string.Empty,
                        string.Empty));
                result.Should().NotBeNull();
                result.Policies.Should().NotBeEmpty();
                result.Properties.Should().NotBeEmpty();
                result.ChildViews.Should().BeEmpty();
            }
        }

        private static void AddPriceCard()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    AuthoringContainer.GetEntityView(
                        _bookId,
                        "Details",
                        "AddPriceCard",
                        string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = "InvalidPriceCard^"
                    },
                    new ViewProperty
                    {
                        Name = "BookName",
                        Value = "ConsoleUxPriceBook"
                    },
                    version
                };
                var result = Proxy.DoCommand(AuthoringContainer.DoAction(view));
                result.Messages.Should().ContainMessageCode("validationerror");
                ConsoleExtensions.WriteExpectedError();

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Name",
                        Value = "ConsoleUxPriceCard"
                    },
                    new ViewProperty
                    {
                        Name = "BookName",
                        Value = "ConsoleUxPriceBook"
                    },
                    new ViewProperty
                    {
                        Name = "Description",
                        Value = "card's description"
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = "card's display name"
                    },
                    version
                };
                result = Proxy.DoCommand(AuthoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
                var persistedModel = result.Models.OfType<PersistedEntityModel>().FirstOrDefault();
                persistedModel.Should().NotBeNull();
                _cardId = persistedModel.EntityId;
                _cardUId = persistedModel.EntityUniqueId;
                _cardFriendlyId = result.Models.OfType<PriceCardAdded>().FirstOrDefault()?.PriceCardFriendlyId;
            }
        }

        private static void EditPriceCard()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    AuthoringContainer.GetEntityView(
                        _cardId,
                        "Details",
                        "EditPriceCard",
                        string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "Description",
                        Value = "edited description"
                    },
                    new ViewProperty
                    {
                        Name = "DisplayName",
                        Value = "edited display name"
                    },
                    version
                };
                var result = Proxy.DoCommand(AuthoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void DuplicatePriceCard()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _cardId,
                        "Details",
                        "DuplicatePriceCard",
                        string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));

                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "DuplicateCardName",
                        Value = "ConsoleUxPriceCardDuplicate"
                    },
                    version
                };
                var result = Proxy.DoCommand(ShopsContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void DeletePriceCard()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(_bookId, "Details", string.Empty, string.Empty));
                view.Should().NotBeNull();
                view.Properties.Should().NotBeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));

                view.Action = "DeletePriceCard";
                view.ItemId = _cardId;
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    version
                };
                var result = Proxy.DoCommand(ShopsContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void AddPriceSnapshot()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _cardId,
                        "PriceSnapshotDetails",
                        "AddPriceSnapshot",
                        string.Empty));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));

                var snapshotDate = DateTimeOffset.Now;
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "BeginDate",
                        Value = snapshotDate.ToString(CultureInfo.InvariantCulture)
                    },
                    version
                };

                var result = Proxy.DoCommand(ShopsContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
                result.Models.OfType<PriceSnapshotAdded>().FirstOrDefault().Should().NotBeNull();
                _snapshotId = result.Models.OfType<PriceSnapshotAdded>().FirstOrDefault()?.PriceSnapshotId;
                var card = Proxy.GetValue(
                    ShopsContainer.PriceCards.ByKey(_cardFriendlyId)
                        .Expand("Snapshots($expand=SnapshotComponents),Components"));
                card.Should().NotBeNull();
                var snapshot = card?.Snapshots.FirstOrDefault(s => s.Id.EndsWith(_snapshotId));
                snapshot?.SnapshotComponents.OfType<ApprovalComponent>().FirstOrDefault().Should().NotBeNull();
                snapshot?.SnapshotComponents.OfType<ApprovalComponent>().FirstOrDefault()?.Status.Should().Be("Draft");
                snapshot?.BeginDate.Should().BeCloseTo(snapshotDate, 1000);
            }
        }

        private static void EditPriceSnapshot()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _cardId,
                        "PriceSnapshotDetails",
                        "EditPriceSnapshot",
                        _snapshotId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));

                var beginDate = DateTimeOffset.Now.AddDays(30);
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    new ViewProperty
                    {
                        Name = "BeginDate",
                        Value = beginDate.ToString(CultureInfo.InvariantCulture)
                    },
                    version
                };

                var result = Proxy.DoCommand(ShopsContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
                var card = Proxy.GetValue(
                    ShopsContainer.PriceCards.ByKey(_cardFriendlyId)
                        .Expand("Snapshots($expand=SnapshotComponents),Components"));
                card.Should().NotBeNull();
                var snapshot = card?.Snapshots.FirstOrDefault(s => s.Id.EndsWith(_snapshotId));
                snapshot?.BeginDate.Should().BeCloseTo(beginDate, 1000);
            }
        }

        private static void EditPriceSnapshot_WithTags()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _cardId,
                        "PriceSnapshotDetails",
                        "EditPriceSnapshot",
                        _snapshotId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();

                var beginDate = DateTimeOffset.Now.AddDays(30);
                view.Properties.FirstOrDefault(p => p.Name.Equals("BeginDate")).Value =
                    beginDate.ToString(CultureInfo.InvariantCulture);
                view.Properties.FirstOrDefault(p => p.Name.Equals("IncludedTags")).Value =
                    "['IncludedTag1', 'IncludedTag2']";

                var result = Proxy.DoCommand(ShopsContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
                var card = Proxy.GetValue(
                    ShopsContainer.PriceCards.ByKey(_cardFriendlyId)
                        .Expand("Snapshots($expand=SnapshotComponents),Components"));
                card.Should().NotBeNull();
                var snapshot = card?.Snapshots.FirstOrDefault(s => s.Id.EndsWith(_snapshotId));
                snapshot?.BeginDate.Should().BeCloseTo(beginDate, 1000);
                snapshot?.Tags.Should().NotBeNullOrEmpty();
                snapshot?.Tags.Count.Should().Be(2);
            }
        }

        private static void RemovePriceSnapshot()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        _cardId,
                        "PriceSnapshotDetails",
                        string.Empty,
                        _snapshotId));
                view.Should().NotBeNull();
                view.Policies.Should().NotBeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().NotBeEmpty();

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));

                view.Action = "RemovePriceSnapshot";
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    version
                };
                var result = Proxy.DoCommand(ShopsContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
            }
        }

        private static void SetSnapshotApprovalStatus()
        {
            using (new SampleMethodScope())
            {
                var container = new CsrSheila().Context.ShopsContainer();

                var result = Proxy.DoCommand(
                    container.AddPriceCard(_bookId, "consoleapprovalpricecard", "displayname", "description"));
                result.Messages.Should().NotContainErrors();
                var persistedModel = result.Models.OfType<PersistedEntityModel>().FirstOrDefault();
                persistedModel.Should().NotBeNull();
                var cardId = persistedModel.EntityId;
                var cardUId = persistedModel.EntityUniqueId;
                var cardFriendlyId = result.Models.OfType<PriceCardAdded>().FirstOrDefault()?.PriceCardFriendlyId;

                var snapshotDate = DateTimeOffset.Now.AddDays(3);
                result = Proxy.DoCommand(container.AddPriceSnapshot(cardId, snapshotDate));
                result.Messages.Should().NotContainErrors();
                var snapshotId = result.Models.OfType<PriceSnapshotAdded>().FirstOrDefault()?.PriceSnapshotId;

                result = Proxy.DoCommand(ShopsContainer.AddPriceTier(cardId, snapshotId, "USD", 3, 13));
                result.Messages.Should().NotContainErrors();
                result.Models.OfType<PriceTierAdded>().FirstOrDefault().Should().NotBeNull();

                // REQUEST APPROVAL
                var view = Proxy.GetValue(
                    container.GetEntityView(
                        cardId,
                        "SetSnapshotApprovalStatus",
                        "RequestSnapshotApproval",
                        snapshotId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.FirstOrDefault(p => p.Name.Equals("Comment")).Value = "request approval comment";
                result = Proxy.DoCommand(container.DoAction(view));
                result.Messages.Should().NotContainErrors();
                result.Messages.Any(m => m.Code.Equals("information", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();

                var card = Pricing.GetPriceCard(cardFriendlyId);
                var snapshot =
                    card.Snapshots.FirstOrDefault(s => s.Id.Equals(snapshotId, StringComparison.OrdinalIgnoreCase));
                snapshot.Should().NotBeNull();
                snapshot?.SnapshotComponents.OfType<ApprovalComponent>().FirstOrDefault().Should().NotBeNull();
                snapshot?.SnapshotComponents.OfType<ApprovalComponent>()
                    .FirstOrDefault()
                    ?.Status.Should()
                    .Be("ReadyForApproval");

                // REJECT
                view = Proxy.GetValue(
                    container.GetEntityView(
                        cardId,
                        "SetSnapshotApprovalStatus",
                        "RejectSnapshot",
                        snapshotId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.FirstOrDefault(p => p.Name.Equals("Comment")).Value = "reject comment";
                result = Proxy.DoCommand(container.DoAction(view));
                result.Messages.Should().NotContainErrors();
                result.Messages.Any(m => m.Code.Equals("information", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();

                card = Pricing.GetPriceCard(cardFriendlyId);
                snapshot = card.Snapshots.FirstOrDefault(
                    s => s.Id.Equals(snapshotId, StringComparison.OrdinalIgnoreCase));
                snapshot.Should().NotBeNull();
                snapshot?.SnapshotComponents.OfType<ApprovalComponent>().FirstOrDefault().Should().NotBeNull();
                snapshot?.SnapshotComponents.OfType<ApprovalComponent>().FirstOrDefault()?.Status.Should().Be("Draft");

                view = Proxy.GetValue(
                    ShopsContainer.GetEntityView(
                        cardId,
                        "SetSnapshotApprovalStatus",
                        "RequestSnapshotApproval",
                        snapshotId));
                view.Properties.FirstOrDefault(p => p.Name.Equals("Comment")).Value =
                    "request approval second time comment";
                result = Proxy.DoCommand(container.DoAction(view));
                result.Messages.Should().NotContainErrors();
                result.Messages.Any(m => m.Code.Equals("information", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();

                // APPROVE
                view = Proxy.GetValue(
                    container.GetEntityView(
                        cardId,
                        "SetSnapshotApprovalStatus",
                        "ApproveSnapshot",
                        snapshotId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.FirstOrDefault(p => p.Name.Equals("Comment")).Value = "approve comment";
                result = Proxy.DoCommand(container.DoAction(view));
                result.Messages.Should().NotContainErrors();
                result.Messages.Any(m => m.Code.Equals("information", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();

                card = Pricing.GetPriceCard(cardFriendlyId);
                snapshot = card.Snapshots.FirstOrDefault(
                    s => s.Id.Equals(snapshotId, StringComparison.OrdinalIgnoreCase));
                snapshot.Should().NotBeNull();
                snapshot?.SnapshotComponents.OfType<ApprovalComponent>().FirstOrDefault().Should().NotBeNull();
                snapshot?.SnapshotComponents.OfType<ApprovalComponent>()
                    .FirstOrDefault()
                    ?.Status.Should()
                    .Be("Approved");

                // RETRACT
                view = Proxy.GetValue(
                    container.GetEntityView(
                        cardId,
                        "SetSnapshotApprovalStatus",
                        "RetractSnapshot",
                        snapshotId));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();
                view.Properties.FirstOrDefault(p => p.Name.Equals("Comment")).Value = "retract comment";
                result = Proxy.DoCommand(container.DoAction(view));
                result.Messages.Should().NotContainErrors();
                result.Messages.Any(m => m.Code.Equals("information", StringComparison.OrdinalIgnoreCase))
                    .Should()
                    .BeTrue();

                card = Pricing.GetPriceCard(cardFriendlyId);
                snapshot = card.Snapshots.FirstOrDefault(
                    s => s.Id.Equals(snapshotId, StringComparison.OrdinalIgnoreCase));
                snapshot.Should().NotBeNull();
                snapshot?.SnapshotComponents.OfType<ApprovalComponent>().FirstOrDefault().Should().NotBeNull();
                snapshot?.SnapshotComponents.OfType<ApprovalComponent>().FirstOrDefault()?.Status.Should().Be("Draft");
            }
        }

        private static void AddCurrency()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    AuthoringContainer.GetEntityView(
                        _cardId,
                        "PriceRow",
                        "SelectCurrency",
                        $"{_snapshotId}"));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().BeEmpty();

                view.Properties.FirstOrDefault(p => p.Name.Equals("Currency")).Value = "USD";
                var result = Proxy.DoCommand(AuthoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
                view = result.Models.OfType<EntityView>().FirstOrDefault(v => v.Name.Equals(view.Name));
                view.Should().NotBeNull();
                view?.Policies.Should().BeEmpty();
                view?.Properties.Should().NotBeEmpty();
                view?.ChildViews.Should().NotBeEmpty();
                view?.Action.Should().Be("AddCurrency");

                ((EntityView) view.ChildViews.FirstOrDefault()).Properties
                    .FirstOrDefault(p => p.Name.Equals("Quantity"))
                    .Value = "1";
                ((EntityView) view.ChildViews.FirstOrDefault()).Properties.FirstOrDefault(p => p.Name.Equals("Price"))
                    .Value = "20";
                result = Proxy.DoCommand(AuthoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
                result.Models.OfType<PriceTierAdded>().FirstOrDefault().Should().NotBeNull();

                view = Proxy.GetValue(
                    AuthoringContainer.GetEntityView(
                        _cardId,
                        "PriceRow",
                        string.Empty,
                        $"{_snapshotId}|USD"));
                view.Should().NotBeNull();
                view.Properties.Should().NotBeEmpty();
            }
        }

        private static void EditCurrency()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    AuthoringContainer.GetEntityView(
                        _cardId,
                        "PriceRow",
                        "EditCurrency",
                        $"{_snapshotId}|USD"));
                view.Should().NotBeNull();
                view.Policies.Should().BeEmpty();
                view.Properties.Should().NotBeEmpty();
                view.ChildViews.Should().NotBeEmpty();

                ((EntityView) view.ChildViews.FirstOrDefault()).Properties.FirstOrDefault(p => p.Name.Equals("Price"))
                    .Value = "200";
                var result = Proxy.DoCommand(AuthoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();

                view = Proxy.GetValue(
                    AuthoringContainer.GetEntityView(
                        _cardId,
                        "PriceRow",
                        string.Empty,
                        $"{_snapshotId}|USD"));
                view.Should().NotBeNull();
                view.Properties.FirstOrDefault(p => p.Name.Equals("1.0")).Should().NotBeNull();
                view.Properties.FirstOrDefault(p => p.Name.Equals("1.0"))?.Value.Should().Be("200.0");
            }
        }

        private static void RemoveCurrency()
        {
            using (new SampleMethodScope())
            {
                var view = Proxy.GetValue(
                    AuthoringContainer.GetEntityView(
                        _cardId,
                        "PriceRow",
                        string.Empty,
                        $"{_snapshotId}|USD"));

                var version = view.Properties.FirstOrDefault(p => p.Name.Equals("Version"));

                view.Action = "RemoveCurrency";
                view.Properties = new ObservableCollection<ViewProperty>
                {
                    version
                };

                var result = Proxy.DoCommand(AuthoringContainer.DoAction(view));
                result.Messages.Should().NotContainErrors();
            }
        }
    }
}
