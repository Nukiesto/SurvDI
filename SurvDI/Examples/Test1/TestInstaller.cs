using SurvDI.Core.Container;
using SurvDI.Core.Services;
using SurvDI.UnityIntegration.UnityIntegration;

namespace SurvDI.Examples.Test1
{
    public class TestInstaller : Installer
    {
        public override void Installing()
        {
            Container.FindAndAddSignals();
            Container.DeclareSignal<TestEvent>();
            
            Container.BindSingle<SignalBus>();
            
            Container.BindSingle<TestService>();
            Container.BindSingle<OtherService>();
        }
    }
}