# Инъекция

Расширим существующий код добавив OtherService

```c#
public class OtherService
{
    public void Do()
    {
        Debug.Log("Do");
    }
}
```
И добавим его в контейнер
```c#
public class TestInstaller : Installer
{
    public override void Installing()
    {
        Container.BindSingle<TestService>();
    }
}
```
И для того чтобы получить доступ к OtherService, из TestService необходимо инъектировать, 
не стоит волноваться что OtherService будет Null, так как инъекция происходит раньше всех конструкторов
```c#
public class TestService
{
    [Inject] private OtherService _otherService;
    
    public TestService()
    {
        Debug.Log("Success");
        _otherService.Do();
    }
}
```
В консоли Unity мы получаем вполне ожидаемый результат
![s](.\images\doService.png)