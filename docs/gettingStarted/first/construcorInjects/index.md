# Инъекции прямо в конструктор

```c#
public class TestService
{
    public TestService(OtherService otherService)
    {
        otherService.Do();
    }
}
```