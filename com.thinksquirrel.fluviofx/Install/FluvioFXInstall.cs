using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
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

        [MenuItem("Tools/FluvioFX/Install/Reinstall...")]
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
            var vfxCodeGenDir = $"{vfxPath}/Editor/Compiler";
            var vfxCodeGenFileName = "VFXCodeGenerator.cs";
            var vfxCodeGenPath = $"{vfxCodeGenDir}/{vfxCodeGenFileName}";
            string vfxCodeGenFile;
            try
            {
                vfxCodeGenFile = File.ReadAllText(vfxCodeGenPath);
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

            if (InternalEditorUtility.inBatchMode ||
                EditorUtility.DisplayDialog(
                    "FluvioFX Install",
                    "FluvioFX installation requires some minor changes to " +
                    "the VFX graph. Press OK to make these edits " +
                    "automatically.",
                    "OK"))
            {
                // Modify VFXCodeGenerator.cs
                vfxCodeGenFile = vfxCodeGenFile
                    .Replace(codeGenEventCallReplace, codeGenEventCall)
                    .Replace(codeGenEventReplace, codeGenEvent)
                    .Replace(codeGenEventCall, codeGenEventCallReplace)
                    .Replace(codeGenEvent, codeGenEventReplace);

                try
                {
                    SaveFileWithPermissions(vfxCodeGenDir, vfxCodeGenFileName, vfxCodeGenFile);

                    // Add integration file
                    var integrationFilePath = $"{vfxPath}/Editor/FluvioFXIntegration.cs";
                    File.WriteAllText(integrationFilePath, integrationFile);
                    File.WriteAllText($"{integrationFilePath}.meta", integrationFileMeta);

                    Debug.Log("FluvioFX install successful!");

                    // Add scripting define
                    SetDefine(true);
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

        private static void SaveFileWithPermissions(string directoryName, string filename, string text)
        {
            var path = Path.Combine(directoryName, filename);

#if UNITY_EDITOR_WIN
            var dirInfo = new DirectoryInfo(directoryName);
            var dirSecurity = dirInfo.GetAccessControl();

            var user = Environment.UserDomainName + "\\" + Environment.UserName;
            dirSecurity.AddAccessRule(new FileSystemAccessRule(
                user,
                FileSystemRights.Read | FileSystemRights.Write | FileSystemRights.Delete,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));

            dirInfo.SetAccessControl(dirSecurity);
#else
            var chmod = new Process
            {
                StartInfo = {
                UseShellExecute = false,
                FileName = "chmod",
                Arguments = "+w \"" + path + "\""
                }
            };
            chmod.Start();
            chmod.WaitForExit();
#endif

            File.WriteAllText(path, text);
        }
    }
}
