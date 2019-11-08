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

namespace FluvioFX.Editor
{
    [InitializeOnLoad]
    internal static class FluvioFXInstall
    {
        #region Replacements
        /* fixformat ignore:start */
        private const string integrationFile = @"
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""FluvioFX.Editor"")]";
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
        private const string templateFileMeta = @"fileFormatVersion: 2
guid: {GUID}
VisualEffectImporter:
  externalObjects: {}
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

        private const string attributeSummary = @"foreach (var attr in attributes)
                    {";
        private const string attributeSummaryReplace = @"foreach (var attr in attributes)
                    {
                        // START FluvioFX
                        if (attr.attrib.name.StartsWith(""neighbors_"") ||
                            attr.attrib.name.StartsWith(""buckets_"")) continue;
                        // END FluvioFX
";
        private const string sourceAttributeLayout = @"DoAttributeLayoutGUI(""Source Attribute Layout"", source);";
        private const string sourceAttributeLayoutReplace = @"// START FluvioFX
                    DoAttributeLayoutGUI(
                    ""Source Attribute Layout (FluvioFX: buckets/neighbors hidden)"",
                    current
                        .Where((bucket) =>
                            !bucket.attributes.All((attr) =>
                                attr.name.StartsWith(""neighbors_"") ||
                                attr.name.StartsWith(""buckets_""))
                        )
                        .ToArray()
                    );
                    // END FluvioFX
";
        private const string currentAttributeLayout = @"DoAttributeLayoutGUI(""Current Attribute Layout"", current);";
        private const string currentAttributeLayoutReplace = @"// START FluvioFX
                    DoAttributeLayoutGUI(
                    ""Current Attribute Layout (FluvioFX: buckets/neighbors hidden)"",
                    current
                        .Where((bucket) =>
                            !bucket.attributes.All((attr) =>
                                attr.name.StartsWith(""neighbors_"") ||
                                attr.name.StartsWith(""buckets_""))
                        )
                        .ToArray()
                    );
                    // END FluvioFX
";
        private const string serializer = @"if (type == null) // resolve runtime type if editor assembly didnt work
                {
                    splitted[1] = splitted[1].Replace("".Editor"", "".Runtime"");
                    name = string.Join("","", splitted);
                    type = Type.GetType(name);
                }";
        private const string serializerReplace = @"// START FluvioFX
                if (type == null) // resolve FluvioFX type if editor assembly didnt work
                {
                    splitted[1] = splitted[1].Replace(""Unity.VisualEffectGraph"", ""FluvioFX"");
                    name = string.Join("","", splitted);
                    type = Type.GetType(name);
                }
                // END FluvioFX

                if (type == null) // resolve runtime type if editor assembly didnt work
                {
                    splitted[1] = splitted[1].Replace("".Editor"", "".Runtime"");
                    name = string.Join("","", splitted);
                    type = Type.GetType(name);
                }";
        /* fixformat ignore:end */
        #endregion Replacements

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
                Debug.LogWarning("Cannot install FluvioFX. Is the Visual Effect Graph installed?");
                SetDefine(false);
                return;
            }

            // FluvioFX path doesn't exist (sanity check)
            var fluvioFxPath = GetPackagePath("com.fluvio.fx");
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

            // Does VFXCodeGenerator.cs exist?
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
                Debug.LogWarning("Cannot install FluvioFX. Unable to open VFXCodeGenerator.cs, which is not supported");
                SetDefine(false);
                return;
            }

            // Does VFXContextEditor.cs exist?
            var vfxContextEditorPath = $"{vfxPath}/Editor/Inspector/VFXContextEditor.cs";
            string vfxContextEditorFile;
            try
            {
                vfxContextEditorFile = File
                    .ReadAllText(vfxContextEditorPath)
                    .Replace("\r\n", "\n");
            }
            catch
            {
                Debug.LogWarning("Cannot install FluvioFX. Unable to open VFXContextEditor.cs, which is not supported");
                SetDefine(false);
                return;
            }

            // Does VFXSerializer.cs exist?
            var vfxSerializerPath = $"{vfxPath}/Editor/Core/VFXSerializer.cs";
            string vfxSerializerFile;
            try
            {
                vfxSerializerFile = File
                    .ReadAllText(vfxSerializerPath)
                    .Replace("\r\n", "\n");
            }
            catch
            {
                Debug.LogWarning("Cannot install FluvioFX. Unable to open VFXSerializer.cs, which is not supported");
                SetDefine(false);
                return;
            }

            // Check if already installed
            if (!force &&
                integrationFileExists &&
                vfxCodeGenFile.Contains(codeGenEventReplace) &&
                vfxCodeGenFile.Contains(codeGenEventCallReplace) &&
                vfxContextEditorFile.Contains(attributeSummaryReplace) &&
                vfxContextEditorFile.Contains(sourceAttributeLayoutReplace) &&
                vfxContextEditorFile.Contains(currentAttributeLayoutReplace) &&
                vfxSerializerFile.Contains(sourceAttributeLayoutReplace) &&
                vfxSerializerFile.Contains(currentAttributeLayoutReplace))
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

                // Modify VFXContextEditor.cs
                vfxContextEditorFile = vfxContextEditorFile
                    .Replace(attributeSummaryReplace.Replace("\r\n", "\n"), attributeSummary.Replace("\r\n", "\n"))
                    .Replace(sourceAttributeLayoutReplace.Replace("\r\n", "\n"), sourceAttributeLayout.Replace("\r\n", "\n"))
                    .Replace(currentAttributeLayoutReplace.Replace("\r\n", "\n"), currentAttributeLayout.Replace("\r\n", "\n"))
                    .Replace(attributeSummary.Replace("\r\n", "\n"), attributeSummaryReplace.Replace("\r\n", "\n"))
                    .Replace(sourceAttributeLayout.Replace("\r\n", "\n"), sourceAttributeLayoutReplace.Replace("\r\n", "\n"))
                    .Replace(currentAttributeLayout.Replace("\r\n", "\n"), currentAttributeLayoutReplace.Replace("\r\n", "\n"));

                // Modify VFXSerializer.cs
                vfxSerializerFile = vfxSerializerFile
                    .Replace(serializerReplace.Replace("\r\n", "\n"), serializer.Replace("\r\n", "\n"))
                    .Replace(serializer.Replace("\r\n", "\n"), serializerReplace.Replace("\r\n", "\n"));

                try
                {
                    // Save VFXCodeGenerator.cs
                    SaveReadOnlyFile(vfxCodeGenPath, vfxCodeGenFile, true);

                    // Save VFXContextEditor.cs
                    SaveReadOnlyFile(vfxContextEditorPath, vfxContextEditorFile, true);

                    // Save VFXSerializer.cs
                    SaveReadOnlyFile(vfxSerializerPath, vfxSerializerFile, true);

                    // Add FluvioFXIntegration.cs
                    var integrationFilePath = $"{vfxPath}/Editor/FluvioFXIntegration.cs";
                    SaveReadOnlyFile(integrationFilePath, integrationFile, false);
                    SaveReadOnlyFile($"{integrationFilePath}.meta", integrationFileMeta, false);

                    // Copy templates
                    foreach (var(fluvioTemplatePath, vfxTemplatePath, newGuid) in GetTemplatePaths())
                    {
                        CopyReadOnlyFile(fluvioTemplatePath, vfxTemplatePath);
                        SaveReadOnlyFile($"{vfxTemplatePath}.meta", templateFileMeta.Replace("{GUID}", newGuid), false);
                    }

                    // Add scripting define
                    SetDefine(true);

                    // Set editor prefs
                    if (force || !inBatchMode && !hasKey)
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
                typeof(UnityEditor.PackageManager.PackageInfo)
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

        private static IEnumerable < (string fluvioPath, string vfxPath, string newGuid) > GetTemplatePaths()
        {
            var fluvioRoot = GetPackagePath($"com.fluvio.fx");
            var vfxRoot = GetPackagePath($"com.unity.visualeffectgraph");

            var template1 = "/Editor/Templates/Fluid Particle System.vfx";
            yield return (
                $"{fluvioRoot}{template1}",
                $"{vfxRoot}{template1}",
                "f2a9e77d93bc41debe400bd75de79d13"
            );
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
                    string.Join(";", defines)
                );
            }
        }

        private static void SaveReadOnlyFile(string path, string text, bool backup)
        {
            path = path.Replace("\\", "/");

            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                if (backup)
                {
                    var backupPath = Path.Combine(
                        Path.GetDirectoryName(path),
                        $".{Path.GetFileName(path)}.backup"
                    );
                    if (!File.Exists(backupPath))
                    {
                        File.Copy(path, backupPath);
                    }
                }
            }
            File.WriteAllText(path, text);
        }

        private static void CopyReadOnlyFile(string from, string to)
        {
            from = from.Replace("\\", "/");
            to = to.Replace("\\", "/");

            if (File.Exists(to))
            {
                File.SetAttributes(to, FileAttributes.Normal);
            }
            if (File.Exists(from))
            {
                File.Copy(from, to, true);
            }
        }
    }
}
