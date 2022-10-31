
namespace SurvDI.Application.Interfaces
{
    public interface IFixTickable
    {
#if UNITY_2019_4
        void FixTick();
#else
        public void FixTick();
#endif
    }
}