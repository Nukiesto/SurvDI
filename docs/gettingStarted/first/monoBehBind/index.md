# Инъекция MonoBehaviour

Для того чтобы добавит MonoBehaviour в контейнер,
необходимо пометить класс аттрибутом Bind, и разрешить добавление MonoBehaviour на сцене, в MonoContext
```c#
[Bind]
public class MonoBeh : MonoBehaviour
{

}
```
Если планируется что MonoBeh на сцене будет больше чем то следует, сделать это

[Bind(Multy = true)]

```c#
[Bind(Multy = true)]
public class MonoBeh : MonoBehaviour
{
    
}
```

Для MonoBehaviour`s, так же работают IInit, IPost, IPreInit и другие интерфейсы

    В работе с MonoBehaviour, которые в контейнере, рекомендую использовать ITick(и другие), вместо Update()