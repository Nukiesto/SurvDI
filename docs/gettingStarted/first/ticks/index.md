# Реализация Update

Для того чтобы реализовать Update, необходимо наследоваться от ITickable.
ITickable - аналог Update() из MonoBehaviour, IFixTickable - FixUpdate(), ILateTickable - LateUpdate()

```c#
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
        _otherService.Do();
    }

    public void FixTick()
    {
        
    }

    public void LateTick()
    {
        
    }
}
```
Запустив данный код, мы получим
![s](.\images\tick.png)