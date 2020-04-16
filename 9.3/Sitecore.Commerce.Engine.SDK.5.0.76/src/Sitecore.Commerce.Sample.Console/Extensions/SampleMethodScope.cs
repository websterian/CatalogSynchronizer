using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Sitecore.Commerce.Extensions
{
    public class SampleMethodScope : IDisposable
    {
        private static int TabCount = 1;
        private Stopwatch _watch = new Stopwatch();
        private string _methodName;
        private bool _disposed = false;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public SampleMethodScope()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(1);
            if (sf.GetMethod().Name.Equals("MoveNext"))
            {
                // The method is an async task
                sf = st.GetFrame(3);
            }

            _methodName = sf.GetMethod().Name;
            _watch.Start();

            ConsoleExtensions.WriteColoredLine(ConsoleColor.DarkGray, $"{new string('>', (TabCount++) * 2)} [Begin Method] {_methodName}");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _watch.Stop();
            ConsoleExtensions.WriteColoredLine(ConsoleColor.DarkGray, $"{new string('<', (--TabCount) * 2)} [End Method] {_methodName} : {_watch.Elapsed}");
            _disposed = true;
        }
    }
}
