using SurvDI.UnityIntegration.Settings;
using UnityEngine;

namespace SurvDI.UnityIntegration.Debugging
{
    public static class Debugger
    {
        private static bool _showInfo;
        private static bool _showWarnings;
        private static bool _showErrors;
        
        public static void SetSettings(SurvDISettings settings)
        {
            _showInfo = settings.showInfo;
            _showErrors = settings.showErrors;
            _showWarnings = settings.showWarnings;
        }

        public static void Log(string text)
        {
            if (_showInfo)
                Debug.Log(text);
        }

        public static void LogWarning(string text)
        {
            if (_showWarnings)
                Debug.LogWarning(text);
        }
        
        public static void LogErrors(string text)
        {
            if (_showErrors)
                Debug.LogError(text);
        }
    }
}