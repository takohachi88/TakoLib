using System;

namespace TakoLib.Common
{
    public static partial class Defines
    {
        public const string UNITY_EDITOR = "UNITY_EDITOR";


#if UNITY_6000_6_OR_NEWER
        [Obsolete]
#endif
        public const string DEVELOPMENT_BUILD = "DEVELOPMENT_BUILD";
        
        /// <summary>
        /// ・editor
        /// ・development build
        /// </summary>
        public const string UNITY_ASSERTIONS = "UNITY_ASSERTIONS";

        /// <summary>
        /// ・editor
        /// ・build（managed code variants：Debug、Checked）
        /// </summary>
        public const string UNITY_ENABLE_CHECKS = "UNITY_ENABLE_CHECKS";

        /// <summary>
        /// ・editor
        /// ・build（managed code variants：Debug、Checked、Instrumented）
        /// </summary>
        public const string UNITY_INCLUDE_INSTRUMENTATION = "UNITY_INCLUDE_INSTRUMENTATION";
    }
}