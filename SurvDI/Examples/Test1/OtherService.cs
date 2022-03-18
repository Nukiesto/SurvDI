using SurvDI.Core.Services;
using UnityEngine;

namespace SurvDI.Examples.Test1
{
    public class OtherService
    {
        public OtherService(SignalBus signalBus)
        {
            signalBus.Subscribe<TestEvent>(s =>
            {
                Debug.Log(s.Data);
            });
        }
    }
}