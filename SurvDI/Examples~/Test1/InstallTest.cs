using SurvDI.Application;
using SurvDI.Core.Container;
using SurvDI.Core.Services;

namespace Tests
{
    public class InstallTest : Installer
    {
        public override void Installing()
        {
            Container.BindSingle<SignalBus>();
            Container.DeclareSignal(typeof(TestSignal));
            Container.BindSingle<Service>();
            Container.BindSingle<Service2>();
        }
    }
}