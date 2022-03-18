using SurvDI.Application.Application.Interfaces;
using SurvDI.Core.Common;
using SurvDI.Core.Services;

namespace SurvDI.Examples.Test1
{
    public struct TestEvent : ISignal
    {
        public string Data;

        public TestEvent(string data)
        {
            Data = data;
        }
    }
    public class TestService : IInit
    {
        [Inject] private SignalBus _signalBus;
        
        public void Init()
        {
            _signalBus.Fire(new TestEvent("Success"));
        }
    }
}