using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FluvioFX.Editor
{
    public static class FluvioFXMenuItems
    {
        [MenuItem("Assets/Create/Visual Effects/FluvioFX Graph", false, 306)]
        public static void CreateVisualEffectAsset()
        {
            var templateString = "";
            try
            {
                templateString = File.ReadAllText(PackageInfo.templatePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Couldn't read template for new FluvioFX asset: {e.Message}");
                return;
            }

            ProjectWindowUtil.CreateAssetWithContent("New Fluid Particle System.vfx", templateString);
        }
    }
}
