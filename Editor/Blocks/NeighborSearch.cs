using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace Thinksquirrel.FluvioFX.Editor.Blocks
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
                if (hasLifetime)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                }
                // yield return new VFXAttributeInfo(FluvioFXAttribute.GridIndex, VFXAttributeMode.Read);
                // yield return new VFXAttributeInfo(FluvioFXAttribute.NeighborCount, VFXAttributeMode.Write);
            }
        }

        public override string source =>
            $@"
#ifdef FLUVIO_INDEX_GRID
// TODO: Grid structure/neighbor search is not optimized, due to VFX limitations.
// At the moment, we are unable to easily pass custom buffers to the system,
// and not all platforms support texture atomics, which would be required
// for a true spatial partition

uint3 indexVector = GetGridIndexVector(position, solverData_KernelSize.x);
int3 indexVectorOff;
uint3 indexVectorOffset;
uint currentGridIndex;
uint nCount = 0;
float3 candidatePosition, dist;
float d;

for (uint candidate = 0; candidate < nbMax; ++candidate)
{{
    if (index == candidate) continue;

    {""/*FluvioFXAttribute.GetLoadAttributeCode(this, FluvioFXAttribute.GridIndex, "candidateGridIndex", "candidate")*/}
    if (candidateGridIndex == 0) continue;

    for (indexVectorOff.x = -1; indexVectorOff.x <= 1; indexVectorOff.x++)
    {{
        for (indexVectorOff.y = -1; indexVectorOff.y <= 1; indexVectorOff.y++)
        {{
            for (indexVectorOff.z = -1; indexVectorOff.z <= 1; indexVectorOff.z++)
            {{
                indexVectorOffset = (uint3)(indexVector + indexVectorOff);
                currentGridIndex = GetGridIndex(indexVectorOffset);

                // indices are +1 for GPU grids (0 = not alive)
                if (candidateGridIndex - 1 == currentGridIndex)
                {{
                    {FluvioFXAttribute.GetLoadAttributeCode(
                        this,
                        VFXAttribute.Position,
                        "candidatePosition",
                        "candidate")}
                    dist = candidatePosition - position;
                    d = dot(dist, dist);

                    if (d < solverData_KernelSize.y)
                    {{
                        SetNeighborIndex(solverData_Tex, solverData_TexSize, index, nCount++, candidate);

                        if (nCount >= FLUVIO_MAX_NEIGHBORS)
                        {{
                            neighborCount = FLUVIO_MAX_NEIGHBORS;
                            return;
                        }}
                    }}
                }}
            }}
        }}
    }}
}}

neighborCount = nCount;
#endif
";
    }
}
