using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Rules
    {
        private static readonly Container ShopsContainer = new CsrSheila().Context.ShopsContainer();

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Rules"))
            {
                GetConditions();
                GetRuntimeSessionConditions();
                GetDateConditions();
                GetActions();
                GetOperators();
            }
        }

        private static void GetConditions()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.Execute(ShopsContainer.GetConditions(string.Empty));
                result.Should().NotBeNull();
                result.Any().Should().BeTrue();
            }
        }

        private static void GetRuntimeSessionConditions()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.Execute(
                    ShopsContainer.GetConditions(
                        "Sitecore.Commerce.Plugin.Rules.IRuntimeSessionCondition, Sitecore.Commerce.Plugin.Rules"));
                result.Should().NotBeNull();
                result.Any().Should().BeTrue();
            }
        }

        private static void GetDateConditions()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.Execute(
                    ShopsContainer.GetConditions(
                        "Sitecore.Commerce.Plugin.Rules.IDateCondition, Sitecore.Commerce.Plugin.Rules"));
                result.Should().NotBeNull();
                result.Any().Should().BeTrue();
            }
        }

        private static void GetActions()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.Execute(ShopsContainer.GetActions(string.Empty));
                result.Should().NotBeNull();
                result.Any().Should().BeTrue();
            }
        }

        private static void GetOperators()
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.Execute(ShopsContainer.GetOperators(string.Empty));
                result.Should().NotBeNull();
                result.Any().Should().BeTrue();
            }
        }
    }
}
