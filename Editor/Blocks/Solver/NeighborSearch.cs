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
    class NeighborSearch : FluvioFXBlock
    {
        public override string name
        {
            get
            {
                return "Neighbor Search";
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(FluvioFXAttribute.NeighborCount, VFXAttributeMode.ReadWrite);

                foreach (var attr in FluvioFXAttribute.Neighbors)
                {
                    yield return new VFXAttributeInfo(attr, VFXAttributeMode.ReadWrite);
                }
            }
        }

        protected internal override SolverDataParameters solverDataParameters => SolverDataParameters.KernelSize;

        public override string source => $@"{CheckAlive()}
// Get location3
int3 location3 = GetLocation3(position, solverData_KernelSize.x);
int3 offset;
float3 dist;
uint location;
uint n = 0;
for (offset.x = -1; offset.x <= 1; ++offset.x)
{{
    for (offset.y = -1; offset.y <= 1; ++offset.y)
    {{
        for (offset.z = -1; offset.z <= 1; ++offset.z)
        {{
            location = GetLocation(location3 + offset, asuint(nbMax));

            for (uint bucketIndex = 0; bucketIndex < FLUVIO_MAX_BUCKET_COUNT; ++bucketIndex)
            {{
                {LoadBucket("neighborIndex", "location", "bucketIndex")}
                if (neighborIndex == 0) break;

                neighborIndex--; // Get true neighbor index

                if (index == neighborIndex) continue;

                {Load(VFXAttribute.Position, "neighborPosition", "neighborIndex")}
                dist = position - neighborPosition;

                if (dot(dist, dist) < solverData_KernelSize.y)
                {{
                    {StoreNeighbor("neighborIndex", "index", "n++")}

                    if (neighborCount >= FLUVIO_MAX_NEIGHBOR_COUNT)
                    {{
                        neighborCount = FLUVIO_MAX_NEIGHBOR_COUNT;
                        return;
                    }}
                }}
            }}
        }}
    }}
}}

neighborCount = n;
";
    }
}
