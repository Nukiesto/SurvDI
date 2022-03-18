using SurvDI.Core.Container;
using SurvDI.UnityIntegration.UnityIntegration;

namespace SurvDI.Examples.Test1
{
    public class TestInstaller : Installer
    {
        public override void Installing()
        {
            Container.BindSingle<TestService>();
            Container.BindSingle<OtherService>();
        }
    }
}