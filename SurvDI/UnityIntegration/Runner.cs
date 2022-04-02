using System.Collections.Generic;
using SurvDI.Application.Interfaces;
using SurvDI.Core.Container;
using UnityEngine;

namespace SurvDI.UnityIntegration
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
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (s.Object is ITickable tick)
                    _tickables.Add(tick);
                if (s.Object is IFixTickable fixTick)
                    _fixTickables.Add(fixTick);
                if (s.Object is ILateTickable lateTick)
                    _lateTickables.Add(lateTick);
            };
            container.OnRemoveInstanceEvent += (c, s) =>
            {
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (s.Object is ITickable tick)
                    if (_tickables.Contains(tick))
                        _tickables.Remove(tick);
                if (s.Object is IFixTickable fixTick)
                    if (_fixTickables.Contains(fixTick))
                        _fixTickables.Remove(fixTick);
                if (s.Object is ILateTickable lateTick)
                    if (_lateTickables.Contains(lateTick))
                        _lateTickables.Remove(lateTick);
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