using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace FluvioFX.Editor.Blocks
{
    [VFXInfo(category = "FluvioFX/Solver")]
    class SpacePartitioning : FluvioFXBlock
    {
        public override string name
        {
            get
            {
                return "Space Partitioning";
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
            }
        }

        protected internal override SolverDataParameters solverDataParameters => SolverDataParameters.KernelSize;

        public override string source => $@"{CheckAlive()}
// Get location
int3 location3 = GetLocation3(position, solverData_KernelSize.x);
uint location = GetLocation(location3, asuint(nbMax));
uint original;

for (uint bucketIndex = 0; bucketIndex < FLUVIO_MAX_BUCKET_COUNT; ++bucketIndex)
{{
    // Store in bucket, +1 for bucket IDs
    {CompareExchangeBucket("location", "bucketIndex", "0", "index + 1", "original")}

    // Successfully stored
    if (original == 0) return;
}}";
    }
}
