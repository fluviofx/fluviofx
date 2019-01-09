using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Thinksquirrel.FluvioFX.Install
{
    [InitializeOnLoad]
    internal static class FluvioFXInstall
    {
        /* fixformat ignore:start */
        private const string integrationFile = @"
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""Thinksquirrel.FluvioFX.Editor"")]";
/* fixformat ignore:end */
        private const string integrationFileMeta = @"fileFormatVersion: 2
guid: 2d81f0f187dd44d2aa907b6dda8b8b85
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: { instanceID: 0 }
  userData:
  assetBundleName:
  assetBundleVariant:
";
        private const string codeGenEventCall = @"// Replace defines
            SubstituteMacros(stringBuilder);";
        private const string codeGenEventCallReplace = @"// Replace defines
            SubstituteMacros(stringBuilder);

            // START FluvioFX
            if (OnGenerateCode != null)
            {
                stringBuilder = OnGenerateCode(context, stringBuilder);
            }
            // END FluvioFX";

        private const string codeGenEvent = @"return stringBuilder;
        }";
        private const string codeGenEventReplace = @"return stringBuilder;
        }

        // START FluvioFX
        public static event Func<VFXContext, StringBuilder, StringBuilder> OnGenerateCode;
        // END FluvioFX";

        static FluvioFXInstall()
        {
            EditorApplication.update += UpdateEvt;
        }

        private static void UpdateEvt()
        {
            // Only execute once
            EditorApplication.update -= UpdateEvt;
            Install();
        }

        [MenuItem("Tools/FluvioFX/Install...")]
        private static void InstallFluvioFX()
        {
            Install(true);
        }

        private static void Install(bool force = false)
        {

            // VFX path does not exist
            var vfxPath = GetPackagePath("com.unity.visualeffectgraph");
            if (vfxPath == null)
            {
                Debug.LogWarning(
                    "Cannot install FluvioFX. " +
                    "Is the Visual Effect Graph installed?");
                SetDefine(false);
                return;
            }

            // FluvioFX path doesn't exist (sanity check)
            var fluvioFxPath = GetPackagePath("com.thinksquirrel.fluviofx");
            if (fluvioFxPath == null)
            {
                Debug.LogWarning(
                    "Cannot install FluvioFX. " +
                    "Package path is missing. This is not supported.");
                SetDefine(false);
                return;
            }

            // Does FluvioFXIntegration.cs exist?
            var integrationFileExists = File.Exists($"{vfxPath}/Editor/FluvioFXIntegration.cs");

            // VFXCodeGenerator.cs can't be found
            var vfxCodeGenPath = $"{vfxPath}/Editor/Compiler/VFXCodeGenerator.cs";
            string vfxCodeGenFile;
            try
            {
                vfxCodeGenFile = File
                    .ReadAllText(vfxCodeGenPath)
                    .Replace("\r\n", "\n");
            }
            catch
            {
                Debug.LogWarning(
                    "Cannot install FluvioFX. " +
                    "Unable to open VFXCodeGenerator.cs, which is not supported");
                SetDefine(false);
                return;
            }

            // Check if already installed
            if (!force &&
                integrationFileExists &&
                vfxCodeGenFile.Contains(codeGenEventReplace) &&
                vfxCodeGenFile.Contains(codeGenEventCallReplace))
            {
                SetDefine(true);
                return;
            }

            var inBatchMode = InternalEditorUtility.inBatchMode;
            var hasKey = EditorPrefs.HasKey("FluvioFXInstall");
            var showDialog = !hasKey && !inBatchMode;
            if (!showDialog ||
                EditorUtility.DisplayDialog(
                    "FluvioFX Install",
                    "FluvioFX installation requires some minor changes to " +
                    "the VFX graph. Press OK to make these edits " +
                    "automatically.",
                    "OK"))
            {
                // Modify VFXCodeGenerator.cs
                vfxCodeGenFile = vfxCodeGenFile
                    .Replace(codeGenEventCallReplace.Replace("\r\n", "\n"), codeGenEventCall.Replace("\r\n", "\n"))
                    .Replace(codeGenEventReplace.Replace("\r\n", "\n"), codeGenEvent.Replace("\r\n", "\n"))
                    .Replace(codeGenEventCall.Replace("\r\n", "\n"), codeGenEventCallReplace.Replace("\r\n", "\n"))
                    .Replace(codeGenEvent.Replace("\r\n", "\n"), codeGenEventReplace.Replace("\r\n", "\n"));

                try
                {
                    // Save VFXCodeGenerator.cs
                    SaveReadOnlyFile(vfxCodeGenPath, vfxCodeGenFile);

                    // Add FluvioFXIntegration.cs
                    var integrationFilePath = $"{vfxPath}/Editor/FluvioFXIntegration.cs";
                    SaveReadOnlyFile(integrationFilePath, integrationFile);
                    SaveReadOnlyFile($"{integrationFilePath}.meta", integrationFileMeta);

                    // Add scripting define
                    SetDefine(true);

                    // Set editor prefs
                    if (!inBatchMode && !hasKey)
                    {
                        Debug.Log("FluvioFX install successful!");
                        EditorPrefs.SetBool("FluvioFXInstall", true);
                    }

                    // Refresh
                    AssetDatabase.Refresh();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError("Error installing FluvioFX. Packages may need to be reimported");
                }
            }
        }
        private static string GetPackagePath(string pkgName)
        {
            var packages =
                ReflectionHelpers
                .GetEditorType("Packages")
                .Invoke<UnityEditor.PackageManager.PackageInfo[]>("GetAll");

            foreach (var pkg in packages)
            {
                if (pkg.name == pkgName)
                {
                    return pkg.resolvedPath.Replace("\\", "/");
                }
            }

            return null;
        }

        private static void SetDefine(bool add)
        {
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            var definesStr = PlayerSettings
                .GetScriptingDefineSymbolsForGroup(buildTarget);
            var defines = new HashSet<string>(definesStr.Split(';'));
            bool changed;

            if (add)
            {
                changed = defines.Add("FLUVIOFX");
            }
            else
            {
                changed = defines.Remove("FLUVIOFX");
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    buildTarget,
                    string.Join(";", defines.ToArray())
                );
            }
        }

        private static void SaveReadOnlyFile(string path, string text)
        {
            path = path.Replace("\\", "/");

            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
            File.WriteAllText(path, text);
            File.SetAttributes(path, FileAttributes.ReadOnly);
        }
    }
}
