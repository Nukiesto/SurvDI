using SurvDI.Core.Container;

namespace SurvDI.Core.Common
{
    public static class ContainerInitEvents
    {
        public static void InitEvents(DiContainer container)
        {
            container.OnBindNewInstanceEvent += OnBindForMultyNeed;
            container.OnRemoveInstanceEvent  += OnRemoveDispose;
        }

        private static void OnBindForMultyNeed(DiContainer container, ContainerUnit s)
        {
            container.AddUnitToWaitingMultiInjects(s);
        }
        private static void OnRemoveDispose(DiContainer container, ContainerUnit s)
        {
            s.InvokeDisposable();
        }
    }
}
