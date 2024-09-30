using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvDI.Core.Services.EventControllerIntegration
{
    public class EventModuleManager
    {
        private class EventUnit
        {
            
        }
        private const int MaxCallDepth = 15;
        private int _eventsInCall;
        private readonly Dictionary<Type, Delegate> _events = new(32);
        private readonly Dictionary<EventModule, Dictionary<Type, Delegate>> _eventsModules = new(32);
        
        public void NewEventModule(EventModule eventModule)
        {
            if (!_eventsModules.ContainsKey(eventModule))
            {
                _eventsModules[eventModule] = new Dictionary<Type, Delegate>();
            }
        }

        public void DestroyedEventModule(EventModule eventModule)
        {
            if (_eventsModules.TryGetValue(eventModule, out var dic))
            {
                //Debug.Log("Contains");
                //Debug.Log(dic.Count);
                
                foreach (var keypair in dic)
                {
                    var t = typeof(EventModuleManager);
                    t.GetMethod(nameof(Unsubscribe))?.MakeGenericMethod(keypair.Key).Invoke(this, new object[]{keypair.Value,false});
                    //Debug.Log(_events.ContainsKey(keypair.Key));
                }

                _eventsModules.Remove(eventModule);
            }
        }
        

        /// <summary>
        /// Subscribe callback to be raised on specific event.
        /// </summary>
        public void Subscribe<T> (EventModule eventModule, Action<T> eventAction) where T : struct
        {
            if (eventAction == null) return;
            var eventType = typeof (T);
            _events.TryGetValue (eventType, out var rawList);
            _events[eventType] = (rawList as Action<T>) + eventAction;
            if (_eventsModules.TryGetValue(eventModule, out var dic))
            {
                dic.TryGetValue (eventType, out var rawList2);
                dic[eventType] = (rawList2 as Action<T>) + eventAction;
            }
        }
        public void Subscribe<T> (EventModule eventModule, Action eventAction) where T : struct
        {
            if (eventAction == null) return;
            
            Subscribe<T>(eventModule, s=>{eventAction.Invoke();});
        }
        /// <summary>
        /// Unsubscribe callback.
        /// </summary>
        public void Unsubscribe<T> (Action<T> eventAction, bool keepEvent = false) where T : struct
        {
            if (eventAction == null) return;
            var eventType = typeof (T);
            if (!_events.TryGetValue(eventType, out var rawList)) return;
            
            var list = (rawList as Action<T>) - eventAction;
            if (list == null && !keepEvent) {
                _events.Remove (eventType);
            } else {
                _events[eventType] = list;
            }
        }

        /// <summary>
        /// Unsubscribe all callbacks from event.
        /// </summary>
        public void UnsubscribeAll<T> (bool keepEvent = false) where T : struct
        {
            var eventType = typeof (T);
            Delegate rawList;
            if (_events.TryGetValue (eventType, out rawList)) {
                if (keepEvent) {
                    _events[eventType] = null;
                } else {
                    _events.Remove (eventType);
                }
            }
        }

        /// <summary>
        /// Unsubscribe all listeneres and clear all events.
        /// </summary>
        public void UnsubscribeAndClearAllEvents () {
            _events.Clear ();
        }

        /// <summary>
        /// Publish event.
        /// </summary>
        public void Publish<T> (T eventMessage) where T : struct
        {
            if (_eventsInCall >= MaxCallDepth) {
#if UNITY_EDITOR
                Debug.LogError ("Max call depth reached");
#endif
                return;
            }
            var eventType = typeof (T);
            _events.TryGetValue (eventType, out var rawList);
            if (rawList is Action<T> list) {
                _eventsInCall++;
                try {
                    list (eventMessage);
                } catch (Exception ex) {
                    Debug.LogError (ex);
                }
                _eventsInCall--;
            }
        }

        public void Publish<T>() where T : struct
        {
            Publish<T>(default);
        }
    }
}