using System.Collections.Generic;
using Sirenix.OdinInspector;
using SurvDI.Application.Interfaces;
using SurvDI.Core.Common;
using SurvDI.Core.Services;

namespace Tests
{
    public class Service2
    {
        [ShowInInspector, InjectMulti] private List<Service> _service = new List<Service>();
        [ShowInInspector, InjectMulti] private List<ITickable> _servicesTickables = new List<ITickable>();
        
        public Service2(SignalBus signalBus)
        {
            signalBus.Fire(new TestSignal("123"));
        }
    }
}