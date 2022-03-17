using SurvDI.Core.Services;
using UnityEngine;

namespace Tests
{
    public class Service
    {
        public Service(SignalBus signalBus)
        {
            signalBus.Subscribe<TestSignal>(s =>
            {
                Debug.Log(s.Test);
            });
        }
    }
}