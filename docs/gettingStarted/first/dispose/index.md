# IDisposable

Когда происходит уничтожение объекта, вызывается Dispose

```c#
public class TestService : IDisposable
{
    public void Dispose()
    {
    }
}
```