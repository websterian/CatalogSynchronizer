using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Sitecore.Commerce.Extensions
{
    public class SampleScenarioScope : IDisposable
    {
        private Stopwatch _watch = new Stopwatch();
        private string _scenarioName;
        bool _disposed = false;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public SampleScenarioScope(string scenarioName)
        {
            _scenarioName = scenarioName;
            _watch.Start();
            ConsoleExtensions.WriteColoredLine(ConsoleColor.White, $"[Begin Scenario] {_scenarioName}");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _watch.Stop();
            ConsoleExtensions.WriteColoredLine(ConsoleColor.White, $"[End Scenario] {_scenarioName} : {_watch.Elapsed}");
            _disposed = true;
        }
    }
}
