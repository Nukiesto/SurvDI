
namespace Plugins.SurvDI.Application.Interfaces
{
    public interface ILateTickable
    {
#if UNITY_2019_4
        void LateTick();
#else
        public void LateTick();
#endif
    }
}