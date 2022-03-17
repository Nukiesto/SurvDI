namespace SurvDI.Application.Application.Interfaces
{
    /// <summary>
    /// Идет после всех конструкторов
    /// </summary>
    public interface IInit
    {
#if UNITY_2019_4
        void Init();
#else
        public void Init();
#endif
    }
    /// <summary>
    /// Идет после всех конструкторов и Init()
    /// </summary>
    public interface IPostInit
    {
#if UNITY_2019_4
        void PostInit();
#else
        public void PostInit();
#endif
    }
    /// <summary>
    /// Идет после конструктора и перед Init()
    /// </summary>
    public interface IPreInit
    {
#if UNITY_2019_4
        void PreInit();
#else
        public void PreInit();
#endif
    }
}