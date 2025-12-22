using System.Diagnostics;

namespace TakoLib.Common
{
    public static class TakoLogger
    {
        private static string Join(object[] objects) => string.Join(", ", objects);

        [Conditional(Defines.UNITY_EDITOR), Conditional(Defines.DEVELOPMENT_BUILD)]
        public static void Logs(params object[] objects)
        {
            UnityEngine.Debug.Log(Join(objects));
        }

        [Conditional(Defines.UNITY_EDITOR), Conditional(Defines.DEVELOPMENT_BUILD)]
        public static void LogWarnings(params object[] objects)
        {
            UnityEngine.Debug.LogWarning(Join(objects));
        }

        [Conditional(Defines.UNITY_EDITOR), Conditional(Defines.DEVELOPMENT_BUILD)]
        public static void LogErrors(params object[] objects)
        {
            UnityEngine.Debug.LogError(Join(objects));
        }
    }
}
