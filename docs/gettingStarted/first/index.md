# Начало

Начнем с того что в SurvDI есть MonoContext и ProjectContext.

BindInstancesOnSceneInRuntime - включать ли в DiContainer объекты находящиеся на сцене

Installers - список в котором находятся необходимые installers

![s](.\images\monoContextInspector.png)

    -Настоятельно рекомендую использовать только один MonoContext и ProjectContext на сцене, 
    во избежание проблем в работе
    -Также рекомендую не объявлять более чем одного ProjectContext в одном проекте

Наследуясь от Installer, мы получаем класс в котором можем объявить все необходимое
```c#
public class TestInstaller : Installer
{
    public override void Installing()
    {
    }
}
```
![s](.\images\testInstallerInspector.png)


Создадим некоторый TestService и объявим его в Installer
```c#
public class TestService
{
    public TestService()
    {
        Debug.Log("Success");
    }
}
```
Осуществлять его добавление в контейнер будем через BindSingle, так как он всего один будет
```c#
public class TestInstaller : Installer
{
    public override void Installing()
    {
        Container.BindSingle<TestService>();
    }
}
```
Запустив проект можно увидеть что все заработало как необходимо
![s](.\images\success.png)


