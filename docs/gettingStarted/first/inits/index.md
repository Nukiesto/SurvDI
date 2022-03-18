# IInit и другие

Для создания более продвинутой логики есть интерфейсы IInit, IPost, IPreInit

```c#
public class TestService : IInit, IPostInit, IPreInit
{
    public void PreInit()
    {
        //Происходит после всех конструкторов, но перед Init()
    }
    
    public void Init()
    {
        //Происходит после всех вызовов Init();
    }

    public void PostInit()
    {
        //Происходит после всех вызовов PostInit();
    }
}
```