using System.Diagnostics;

namespace TakoLib.Common
{
    public static class DebugTako
    {
        private static string LogString(object[] objects) => string.Join(", ", objects);

        [Conditional(Defines.UNITY_EDITOR)]
        public static void Logs(params object[] objects)
        {
            UnityEngine.Debug.Log(LogString(objects));
        }

        [Conditional(Defines.UNITY_EDITOR)]
        public static void LogWarnings(params object[] objects)
        {
            UnityEngine.Debug.LogWarning(LogString(objects));
        }

        [Conditional(Defines.UNITY_EDITOR)]
        public static void LogErrors(params object[] objects)
        {
            UnityEngine.Debug.LogError(LogString(objects));
        }
    }
}
