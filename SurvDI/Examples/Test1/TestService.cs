using SurvDI.Application.Application.Interfaces;
using SurvDI.Core.Common;
using UnityEngine;

namespace SurvDI.Examples.Test1
{
    public class TestService : ITickable, IFixTickable, ILateTickable
    {
        [Inject] private OtherService _otherService;
        
        public TestService()
        {
            Debug.Log("Success");
            _otherService.Do();
        }
        
        public void Tick()
        {
            
        }

        public void FixTick()
        {
            
        }

        public void LateTick()
        {
            
        }
    }
}