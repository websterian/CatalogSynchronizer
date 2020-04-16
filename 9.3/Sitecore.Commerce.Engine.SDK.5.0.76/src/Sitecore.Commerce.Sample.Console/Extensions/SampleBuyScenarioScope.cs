using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Sitecore.Commerce.Extensions
{
    public class SampleBuyScenarioScope : IDisposable
    {
        private static int TabCount = 1;
        private Stopwatch _watch = new Stopwatch();
        private string _scenarioName;
        private bool _disposed;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public SampleBuyScenarioScope()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(1);

            _scenarioName = sf.GetMethod().ReflectedType.Name;
            _watch.Start();

            ConsoleExtensions.WriteColoredLine(ConsoleColor.DarkCyan, $"{new string('>', (TabCount++) * 2)} [Begin Buy Scenario] {_scenarioName}");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _watch.Stop();
            ConsoleExtensions.WriteColoredLine(ConsoleColor.DarkCyan, $"{new string('<', (--TabCount) * 2)} [End Buy Scenario] {_scenarioName} : {_watch.Elapsed}");
            _disposed = true;
        }
    }
}
