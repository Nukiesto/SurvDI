using System;

namespace SurvDI.Core.Services.EventControllerIntegration
{
    public class EventModule
    {
        private EventModuleManager _eventModuleManager;
        
        public void Init(EventModuleManager eventModuleManager)
        {
            _eventModuleManager = eventModuleManager;
            eventModuleManager.NewEventModule(this);
        }

        public void Publish<T>() where T : struct
        {
            _eventModuleManager.Publish<T>();
        }
        public void Publish<T>(T signal) where T : struct
        {
            _eventModuleManager.Publish(signal);
        }
        public void Subscribe<T>(Action<T> signal) where T : struct
        {
            _eventModuleManager.Subscribe(this, signal);
        }
        public void Subscribe<T>(Action signal) where T : struct
        {
            _eventModuleManager.Subscribe<T>(this, signal);
        }
        public void Dispose()
        {
            _eventModuleManager.DestroyedEventModule(this);    
        }
    }
}