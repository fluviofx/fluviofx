using System.IO;
using UnityEditor;
using UnityEditor.VFX;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluvioFX.Editor;
using FluvioFX.Editor.Blocks;
using UnityEngine.Experimental.VFX;

namespace FluvioFX.Editor
{
    internal static class ShaderPostprocessor
    {
        /* fixformat ignore:start */
        private static Dictionary<string, string> globalReplacements = new Dictionary<string, string>
        {
            // Remove bucket and neighbor defines
            { $@"#define VFX_USE_BUCKETS_[0-9]+_CURRENT 1{"\n"}", "" },
            { $@"#define VFX_USE_NEIGHBORS_[0-9]+_CURRENT 1{"\n"}", "" },
        };
        private static Dictionary<string, string> replacementFormat = new Dictionary<string, string>
        {
            // Get index
            { @"void {0}(_[0-9A-F]+){{0,1}}\((?!uint index,)", "void {0}$1(uint index, " },
            { @"(?<!void ){0}(_[0-9A-F]+){{0,1}}\((?!index,)", "{0}$1(index, " },

            // Remove bucket attributes (we get these manually)
            { @", inout uint buckets_[0-9]+", "" },
            { @",  /\*inout \*/buckets_[0-9]+", "" },
            { $@"uint buckets_[0-9]+ = \(attributeBuffer\.Load\(\(index \* 0x[0-9A-F]+ \+ 0x[0-9A-F]+\) << [0-9]+\)\);{"\n\t"}*", "" },
            { $@"attributeBuffer\.Store\(\(index \* 0x[0-9A-F]+ \+ 0x[0-9A-F]+\) << [0-9]+,asuint\(buckets_[0-9]+\)\);{"\n\t"}*", "" },

            // Remove neighbor attributes (we get these manually too)
            { @", inout uint neighbors_[0-9]+", "" },
            { @",  /\*inout \*/neighbors_[0-9]+", "" },
            { $@"uint neighbors_[0-9]+ = \(attributeBuffer\.Load\(\(index \* 0x[0-9A-F]+ \+ 0x[0-9A-F]+\) << [0-9]+\)\);{"\n\t"}*", "" },
            { $@"attributeBuffer\.Store\(\(index \* 0x[0-9A-F]+ \+ 0x[0-9A-F]+\) << [0-9]+,asuint\(neighbors_[0-9]+\)\);{"\n\t"}*", "" },
        };
        /* fixformat ignore:end */

        public static StringBuilder ModifyShader(VFXContext context, StringBuilder source)
        {
            var blockFunctionNames = typeof(FluvioFXBlock)
                .Assembly
                .GetTypes()
                .Where(t =>
                    (typeof(FluvioFXBlock).IsAssignableFrom(t) || typeof(ICollisionSettings).IsAssignableFrom(t)) &&
                    !t.IsAbstract &&
                    !t.IsInterface)
                .Select(t => t.Name);

            var shouldModify =
                context.activeChildrenWithImplicit.Any((b) =>
                    typeof(FluvioFXBlock).IsAssignableFrom(b.GetType()) ||
                    typeof(ICollisionSettings).IsAssignableFrom(b.GetType()));

            if (shouldModify)
            {
                var data = context.GetData() as VFXDataParticle;
                var defines = FormattableString.Invariant($@"
// FluvioFX simulation constants
#define FLUVIO_EPSILON {FluvioFXSettings.kEpsilon}
#define FLUVIO_MAX_SQR_VELOCITY_CHANGE {FluvioFXSettings.kMaxSqrVelocityChange}
#define FLUVIO_MAX_BUCKET_COUNT {FluvioFXSettings.kMaxBucketCount}
#define FLUVIO_MAX_NEIGHBOR_COUNT {FluvioFXSettings.kMaxNeighborCount}
#define FLUVIO_AUTO_PARTICLE_SIZE_FACTOR {FluvioFXSettings.kAutoParticleSizeFactor}
#define FLUVIO_MAX_GRID_SIZE {(uint)Mathf.Pow(data?.capacity ?? 262144, 1.0f / 3.0f)}

");

                var result = defines + source.ToString().Replace("\r\n", "\n");

                // Special case: replacement
                var replacementBlock =
                    context
                    .activeChildrenWithImplicit
                    .FirstOrDefault((b) => !string.IsNullOrWhiteSpace((b as FluvioFXBlock)?.replacementKernel))
                as FluvioFXBlock;

                if (replacementBlock != null)
                {
                    result = result.Substring(0, result.LastIndexOf("[numthreads(NB_THREADS_PER_GROUP,1,1)]"));
                    result += $@"[numthreads(NB_THREADS_PER_GROUP,1,1)]
void CSMain(uint3 groupId          : SV_GroupID,
            uint3 groupThreadId    : SV_GroupThreadID,
			uint3 dispatchThreadId : SV_DispatchThreadID)
{{
{replacementBlock.replacementKernel}
}}";
                }

                foreach (var kvp in globalReplacements)
                {
                    var pattern = kvp.Key;
                    var replacement = kvp.Value;
                    result = Regex.Replace(result, pattern, replacement);
                }

                foreach (var functionName in blockFunctionNames)
                {
                    foreach (var kvp in replacementFormat)
                    {
                        var pattern = string.Format(kvp.Key, functionName);
                        var replacement = string.Format(kvp.Value, functionName);
                        result = Regex.Replace(result, pattern, replacement);
                    }
                }

                return new StringBuilder(result);
            }

            return source;
        }
    }
}
