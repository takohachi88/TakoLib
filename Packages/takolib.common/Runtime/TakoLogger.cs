using System.Diagnostics;

namespace TakoLib.Common
{
    public static class TakoLogger
    {
        private static string Join(object[] objects) => string.Join(", ", objects);

#if UNITY_6000_6_OR_NEWER
        [Conditional(Defines.UNITY_INCLUDE_INSTRUMENTATION)]
#else
        [Conditional(Defines.DEVELOPMENT_BUILD)]
        [Conditional(Defines.UNITY_EDITOR)]
#endif
        public static void Logs(params object[] objects)
        {
            UnityEngine.Debug.Log(Join(objects));
        }

#if UNITY_6000_6_OR_NEWER
        [Conditional(Defines.UNITY_INCLUDE_INSTRUMENTATION)]
#else
        [Conditional(Defines.DEVELOPMENT_BUILD)]
        [Conditional(Defines.UNITY_EDITOR)]
#endif
        public static void LogWarnings(params object[] objects)
        {
            UnityEngine.Debug.LogWarning(Join(objects));
        }

#if UNITY_6000_6_OR_NEWER
        [Conditional(Defines.UNITY_INCLUDE_INSTRUMENTATION)]
#else
        [Conditional(Defines.DEVELOPMENT_BUILD)]
        [Conditional(Defines.UNITY_EDITOR)]
#endif
        public static void LogErrors(params object[] objects)
        {
            UnityEngine.Debug.LogError(Join(objects));
        }
    }
}
