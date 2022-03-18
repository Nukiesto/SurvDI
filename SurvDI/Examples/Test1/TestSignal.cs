
using SurvDI.Core.Services;

namespace SurvDI.Examples.Test1
{
    public readonly struct TestSignal : ISignal
    {
        public readonly string Test;

        public TestSignal(string test)
        {
            Test = test;
        }
    }
}