
namespace Plugins.SurvDI.Application.Interfaces
{
    public interface ITickable
    {
#if UNITY_2019_4
        void Tick();
#else
        public void Tick();
#endif
    }
}