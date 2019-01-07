using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Thinksquirrel.FluvioFX.Editor
{
    public static class FluvioFXMenuItems
    {
        private static string _templatePath = null;
        private const string _templateAssetName = "Fluid Particle System.vfx";

        private static string GetTemplatePath()
        {
            if (_templatePath == null)
            {
                _templatePath = $"{PackageInfo.assetPackagePath}/Editor/Templates/";
            }
            return _templatePath;
        }

        [MenuItem("Assets/Create/Visual Effects/FluvioFX Graph", false, 306)]
        public static void CreateVisualEffectAsset()
        {
            var templateString = "";
            try
            {
                templateString = File.ReadAllText($"{GetTemplatePath()}{_templateAssetName}");
            }
            catch (Exception e)
            {
                Debug.LogError("Couldn't read template for new FluvioFX asset : " + e.Message);
                return;
            }

            ProjectWindowUtil.CreateAssetWithContent("New Fluid Particle System.vfx", templateString);
        }
    }
}
