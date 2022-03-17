
using SurvDI.Core.Services;

namespace Tests
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