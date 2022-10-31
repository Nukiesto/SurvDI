using UnityEngine;

namespace SurvDI.UnityIntegration.Settings
{
    public class SurvDISettings : ScriptableObject
    {
        [Header("Debugger")]
        public bool showInfo;
        public bool showWarnings = true;
        public bool showErrors = true;
    }
}