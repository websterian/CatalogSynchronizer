using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Sitecore.Commerce.Engine;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Plugin.Composer;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    public static class Composer
    {
        private static Container _authoringContainer;

        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Composer"))
            {
                var context = new CsrSheila().Context;
                context.Environment = EnvironmentConstants.AdventureWorksAuthoring;
                _authoringContainer = context.ShopsContainer();

                var templates = GetComposerTemplates();
                GetComposerTemplate(templates.FirstOrDefault().Id);
            }
        }

        private static void GetComposerTemplate(string templateId)
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(
                    _authoringContainer.ComposerTemplates.ByKey(templateId).Expand("Components"));
                result.Should().NotBeNull();

                result = Proxy.GetValue(
                    _authoringContainer.ComposerTemplates.ByKey(templateId.ToEntityName<ComposerTemplate>())
                        .Expand("Components"));
                result.Should().NotBeNull();
            }
        }

        private static List<ComposerTemplate> GetComposerTemplates()
        {
            using (new SampleMethodScope())
            {
                var result = _authoringContainer.ComposerTemplates.Expand("Components").Execute();
                var composerTemplates = result as List<ComposerTemplate> ?? result.ToList();
                composerTemplates.Should().NotBeNull();
                composerTemplates.Should().NotBeEmpty();

                return composerTemplates;
            }
        }
    }
}
