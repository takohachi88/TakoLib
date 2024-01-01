using System;
using UnityEngine;
using UnityEngine.Rendering;
using TakoLib.Urp.Editor;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace TakoLib.Urp.PostProcess
{
    [Serializable]
    public class CustomPostProcessData : ScriptableObject
    {
#if UNITY_EDITOR
        //CA1812: internal class that is apparently never instantiated
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateDataAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                CustomPostProcessData instance = CreateInstance<CustomPostProcessData>();
                AssetDatabase.CreateAsset(instance, pathName);
                ResourceReloader.ReloadAllNullIn(instance, TakoLibUrpEditor.GetBasePath());
                Selection.activeObject = instance;
            }
        }

        [MenuItem("Assets/Create/Rendering/URP TakoLib Post-process Data", priority = CoreUtils.Sections.section5 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        static void CreateData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateDataAsset>(), "CustomPostProcessData.asset", null, null);
        }
#endif

        [Serializable, ReloadGroup]
        public class CustomPostProcessResources
        {
            [Reload("Shaders/PostProcessing/SmartDof.shader")]
            public Shader smartDof;

            [Reload("Shaders/PostProcessing/RadialBlur.shader")]
            public Shader radialBlur;

            [Reload("Shaders/PostProcessing/AdvancedVignette.shader")]
            public Shader advancedVignette;
        }

        public CustomPostProcessResources resources;
    }
}