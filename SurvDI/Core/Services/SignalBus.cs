using System;
using System.Collections.Generic;
using System.Linq;
using Plugins.SurvDI.Core.Common;
using Plugins.SurvDI.Core.Container;

namespace Plugins.SurvDI.Core.Services
{
    public interface ISignal { }

    internal class Signal
    {
        public readonly Type SignalType;
        
        private readonly List<Action<ISignal>> _subsribes = new List<Action<ISignal>>();
        private readonly List<Action> _subsribes2 = new List<Action>();
        
        public Signal(Type signalType)
        {
            SignalType = signalType;
        }

        public void Subscribe<T>(Action<T> action)
        {
            void ActionIn(ISignal signal)
            {
                action.Method.Invoke(action.Target, new object[] {signal});
            }

            _subsribes.Add(ActionIn);
        }

        public void Subscribe(Action action)
        {
            _subsribes2.Add(action);
        }

        public void Fire(ISignal action)
        {
            foreach (var subsribe in _subsribes)
                subsribe.Invoke(action);
            foreach (var subsribe in _subsribes2)
                subsribe.Invoke();
        }
        public void Fire()
        {
            foreach (var subsribe in _subsribes)
                subsribe.Invoke(default);
            foreach (var subsribe in _subsribes2)
                subsribe.Invoke();
        }
    }
    public class SignalBus
    {
        //key-signalType;v-action
        
#if UNITY_2019_4
        private readonly Dictionary<Type, Signal> _signals = new Dictionary<Type, Signal>();
#else
        private readonly Dictionary<Type, Signal> _signals = new();
#endif
        
        [InjectMulti] private List<Signal> _signalsInject;

        public SignalBus()
        {
            var signals = _signalsInject.Where(signal => !_signals.ContainsKey(signal.SignalType));
            foreach (var signal in signals)
                _signals.Add(signal.SignalType, signal);
        }
        public void Fire<T>() where T : struct
        {
            if (_signals.TryGetValue(typeof(T), out var signal))
                signal.Fire();
        }
        public void Fire<T>(T signal) where T : struct
        {
            if (_signals.TryGetValue(typeof(T), out var signalT))
                signalT.Fire((ISignal)signal);
        }
        public void Subscribe<T>(Action<T> action) where T : struct
        {
            if (_signals.TryGetValue(typeof(T), out var signalT))
                signalT.Subscribe(action);
        }
        public void Subscribe<T>(Action action) where T : struct
        {
            if (_signals.TryGetValue(typeof(T), out var signalT))
                signalT.Subscribe(action);
        }
    }

    public static class SignalBusExt
    {
        public static ContainerUnit DeclareSignal(this DiContainer container, Type typeSignal)
        {
            var signal = new Signal(typeSignal);
            return container.BindInstanceMulti(signal);
        }
    }
}