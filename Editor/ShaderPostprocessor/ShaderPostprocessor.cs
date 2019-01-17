using System.IO;
using UnityEditor;
using UnityEditor.VFX;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Thinksquirrel.FluvioFX.Editor;
using Thinksquirrel.FluvioFX.Editor.Blocks;
using UnityEngine.Experimental.VFX;

namespace Thinksquirrel.FluvioFX.Editor
{
    internal static class ShaderPostprocessor
    {
        private static string[] patternFormat = new []
        {
            "void {0}(_[0-9A-F]+){{0,1}}\\((?!uint index)",
            "(?<!void ){0}(_[0-9A-F]+){{0,1}}\\((?!index)",
        };
        private static string[] replacementFormat = new []
        {
            "void {0}$1(uint index, ",
            "{0}$1(index, ",
        };

        public static StringBuilder ModifyShader(VFXContext context, StringBuilder source)
        {
            var blockFunctionNames = typeof(FluvioFXBlock)
                .Assembly
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(FluvioFXBlock)) && !t.IsAbstract)
                .Select(t => t.Name);

            if (context.activeChildrenWithImplicit.Any((b) => typeof(FluvioFXBlock).IsAssignableFrom(b.GetType())))
            {
                var result = source.ToString();
                foreach (var functionName in blockFunctionNames)
                {
                    for (var i = 0; i < patternFormat.Length; ++i)
                    {
                        var pattern = string.Format(patternFormat[i], functionName);
                        var replacement = string.Format(replacementFormat[i], functionName);
                        result = Regex.Replace(result, pattern, replacement);
                    }
                }

                return new StringBuilder(result);
            }
            return source;
        }
    }
}
