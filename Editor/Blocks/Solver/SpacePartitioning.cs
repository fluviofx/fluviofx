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
                if (hasLifetime)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                }
                // yield return new VFXAttributeInfo(FluvioFXAttribute.GridIndex, VFXAttributeMode.Write);
            }
        }

        public override string source =>
            @"
#ifdef FLUVIO_INDEX_GRID
// indices are +1 for GPU grids (0 = not alive)
gridIndex = GetGridIndexFromPosition(position, solverData_KernelSize.x) + 1;
#endif
";
    }
}
