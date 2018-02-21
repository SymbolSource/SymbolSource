using System.Diagnostics;

namespace SymbolSource.Contract.Storage
{
    internal class DebugExtensions
    {
        [Conditional("DEBUG")] 
        public static void Assert(bool condition)
        {
            if (!condition)
                Debugger.Break();

            Debug.Assert(condition);
        }
    }
}