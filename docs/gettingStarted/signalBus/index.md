# SignalBus

SignalBus - вспомогательный сервис, который реализует возможности EventBus

Создадим TestEvent, который будет вызывать из TestService, и подписываться из OtherService.
TestEvent наследуется от ISignal, чтобы автоматически добавиться у SignalBus

```c#
public struct TestEvent : ISignal
{
    public string Data;
    
    public TestEvent(string data)
    {
        Data = data;
    }
}
```
Объявим SignalBus в TestInstaller
```c#
public class TestInstaller : Installer
{
    public override void Installing()
    {
        Container.FindAndAddSignals();//Инициализация сигналов
        Container.BindSingle<SignalBus>();
    
        Container.BindSingle<TestService>();
        Container.BindSingle<OtherService>();
    }
}
```

И так подпишемся на TestEvent в OtherService
```c#
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
```
Вызовем TestEvent из TestService
```c#
public class TestService : IInit
{
    [Inject] private SignalBus _signalBus;
    
    public void Init()
    {
        _signalBus.Fire(new TestEvent("Success"));
    }
}
```
Получаем вполне логичный результат
![s](.\images\unity.png)