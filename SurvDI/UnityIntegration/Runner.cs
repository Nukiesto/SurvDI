using System.Collections.Generic;
using SurvDI.Application.Interfaces;
using SurvDI.Core.Container;
using UnityEngine;

namespace SurvDI.UnityIntegration.UnityIntegration
{
    public class Runner : MonoBehaviour
    {
#if UNITY_2019_4
        private readonly List<ITickable> _tickables = new List<ITickable>();
        private readonly List<IFixTickable> _fixTickables = new List<IFixTickable>();
        private readonly List<ILateTickable> _lateTickables = new List<ILateTickable>();
#else
        private readonly List<ITickable> _tickables = new();
        private readonly List<IFixTickable> _fixTickables = new();
        private readonly List<ILateTickable> _lateTickables = new();
#endif
        
        public void Init(DiContainer container)
        {
            container.OnBindNewInstanceEvent += (c, s) =>
            {
                switch (s.Object)
                {
                    case ITickable tick:
                        _tickables.Add(tick);
                        break;
                    case IFixTickable fixTick:
                        _fixTickables.Add(fixTick);
                        break;
                    case ILateTickable lateTick:
                        _lateTickables.Add(lateTick);
                        break;
                }
            };
            container.OnRemoveInstanceEvent += (c, s) =>
            {
                switch (s.Object)
                {
                    case ITickable tick:
                        if (_tickables.Contains(tick))
                            _tickables.Remove(tick);
                        break;
                    case IFixTickable fixTick:
                        if (_fixTickables.Contains(fixTick))
                            _fixTickables.Remove(fixTick);
                        break;
                    case ILateTickable lateTick:
                        if (_lateTickables.Contains(lateTick))
                            _lateTickables.Remove(lateTick);
                        break;
                }
            };
            //Add tickables
            _tickables.AddRange(container.GetInterfaceUnits<ITickable>());
            _fixTickables.AddRange(container.GetInterfaceUnits<IFixTickable>());
            _lateTickables.AddRange(container.GetInterfaceUnits<ILateTickable>());
        }
        private void Update()
        {
            for (var i = 0; i < _tickables.Count; i++)
                _tickables[i].Tick();
        }
        private void LateUpdate()
        {
            for (var i = 0; i < _lateTickables.Count; i++)
                _lateTickables[i].LateTick();
        }
        private void FixedUpdate()
        {
            for (var i = 0; i < _fixTickables.Count; i++)
                _fixTickables[i].FixTick();
        }
    }
}