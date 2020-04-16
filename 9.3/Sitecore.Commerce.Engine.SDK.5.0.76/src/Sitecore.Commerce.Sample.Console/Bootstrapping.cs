using System;
using System.IO;
using FluentAssertions;
using Sitecore.Commerce.Extensions;
using Sitecore.Commerce.Sample.Contexts;
using Sitecore.Commerce.ServiceProxy;

namespace Sitecore.Commerce.Sample.Console
{
    using CommerceOps = CommerceOps.Sitecore.Commerce.Engine;

    public static class Bootstrapping
    {
        public static void RunScenarios()
        {
            using (new SampleScenarioScope("Bootstrapping"))
            {
                var container = new DevOpAndre
                {
                    Context =
                    {
                        Environment = "GlobalEnvironment"
                    }
                }.Context.OpsContainer();
                Bootstrap(container);
                CleanEnvironment(container, "AdventureWorksAuthoring");
                CleanEnvironment(container, "HabitatAuthoring");
                InitializeEnvironment(container, "AdventureWorksAuthoring");
                InitializeEnvironment(container, "HabitatAuthoring");

                GetPipelines(container);
            }
        }

        public static void InitializeEnvironment(CommerceOps.Container container, string environmentName)
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(container.InitializeEnvironment(environmentName));
                result.WaitUntilCompletion();
            }
        }

        private static void Bootstrap(CommerceOps.Container container)
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(container.Bootstrap());
                result.ResponseCode.Should().Be("Ok");
            }
        }

        private static void CleanEnvironment(CommerceOps.Container container, string environment)
        {
            using (new SampleMethodScope())
            {
                var result = Proxy.GetValue(container.CleanEnvironment(environment));
                result.ResponseCode.Should().Be("Ok");
            }
        }

        private static void GetPipelines(CommerceOps.Container container)
        {
            using (new SampleMethodScope())
            {
                var pipelineConfiguration = Proxy.GetValue(container.GetPipelines());

                var localPath = AppDomain.CurrentDomain.BaseDirectory;

                var pipelineFile = $"{localPath}/logs/ConfiguredPipelines.log";

                if (!Directory.Exists($"{localPath}/logs"))
                {
                    Directory.CreateDirectory($"{localPath}/logs");
                }

                if (File.Exists(pipelineFile))
                {
                    File.Delete(pipelineFile);
                }

                using (StreamWriter file =
                    new StreamWriter(pipelineFile))
                {
                    file.WriteLine("Current Pipeline Configuration");
                    file.WriteLine("-----------------------------------------------------------------");
                    foreach (var pipeline in pipelineConfiguration.List)
                    {
                        file.WriteLine($"{pipeline.Namespace}");
                        file.WriteLine($"{pipeline.Name} ({pipeline.Receives} => {pipeline.Returns})");
                        foreach (var block in pipeline.Blocks)
                        {
                            var computedNamespace = block.Namespace.Replace("Sitecore.Commerce.", "");
                            file.WriteLine(
                                $"     {computedNamespace}.{block.Name} ({block.Receives} => {block.Returns})");
                        }

                        if (!string.IsNullOrEmpty(pipeline.Comment))
                        {
                            file.WriteLine("     ------------------------------------------------------------");
                            file.WriteLine($"     Comment: {pipeline.Comment}");
                        }

                        file.WriteLine("-----------------------------------------------------------------");
                    }
                }
            }
        }
    }
}
