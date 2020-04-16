using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Availability;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Policies
    {
        private static readonly Container ShopsContainer = new CsrSheila().Context.ShopsContainer();

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Policies"))
            {
                var policySet = GetPolicySet("Entity-PolicySet-GlobalCartPolicies");
                AddUpdatePolicySet(policySet);
                RemovePolicy(policySet);
            }
        }

        private static void RemovePolicy(PolicySet policySet)
        {
            using (new SampleMethodScope())
            {
                policySet.Should().NotBeNull();
                var result =
                    Proxy.DoCommand(
                        ShopsContainer.RemovePolicy(
                            "Entity-PolicySet-GlobalCartPolicies",
                            "Sitecore.Commerce.Plugin.Availability.AvailabilityAlwaysPolicy, Sitecore.Commerce.Plugin.Availability",
                            string.Empty));
                result.Messages.Should().NotContainMessageCode("error");
            }
        }

        private static void AddUpdatePolicySet(PolicySet policySet)
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.DoCommand(
                    ShopsContainer.AddPolicy(
                        policySet.Id,
                        "Sitecore.Commerce.Plugin.Availability.AvailabilityAlwaysPolicy, Sitecore.Commerce.Plugin.Availability",
                        new AvailabilityAlwaysPolicy
                        {
                            PolicyId = "AvailabilityAlways"
                        }));
                result.Messages.Should().NotContainErrors();
                result.Models.OfType<PolicyAddedModel>().Any().Should().BeTrue();
            }
        }

        private static PolicySet GetPolicySet(string id)
        {
            using (new SampleMethodScope())
            {
                var result = ShopsContainer.PolicySets.ByKey(id).GetValue();

                result.Should().NotBeNull();

                return result;
            }
        }
    }
}
