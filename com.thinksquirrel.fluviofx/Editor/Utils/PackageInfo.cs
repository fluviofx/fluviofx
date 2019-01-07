using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Thinksquirrel.FluvioFX.Editor
{
    public static class PackageInfo
    {
        static string _packagePath;

        public static string fileSystemPackagePath
        {
            get
            {
                if (_packagePath == null)
                {
                    var packages = ReflectionHelpers
                        .GetEditorType("Packages")
                        .Invoke<UnityEditor.PackageManager.PackageInfo[]>("GetAll");

                    foreach (var pkg in packages)
                    {
                        if (pkg.name == "com.thinksquirrel.fluviofx")
                        {
                            _packagePath = pkg.resolvedPath.Replace("\\", "/");
                            break;
                        }
                    }
                }
                return _packagePath;
            }
        }
        public static string assetPackagePath
        {
            get
            {
                return "Packages/com.thinksquirrel.fluviofx";
            }
        }
    }
}
