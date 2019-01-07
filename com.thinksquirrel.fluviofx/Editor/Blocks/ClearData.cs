using System;
using System.Collections.Generic;
using System.Linq;
using Thinksquirrel.FluvioFX.Editor;
using UnityEditor.VFX;
using UnityEngine;

namespace Thinksquirrel.FluvioFX.Editor.Blocks
{
    [VFXInfo(category = "FluvioFX")]
    class ClearData : FluvioFXBlock
    {
        public override string name
        {
            get
            {
                return "Clear Data";
            }
        }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(FluvioFXAttribute.Force, VFXAttributeMode.Write);
                // yield return new VFXAttributeInfo(FluvioFXAttribute.GridIndex, VFXAttributeMode.Write);
                // yield return new VFXAttributeInfo(FluvioFXAttribute.NeighborCount, VFXAttributeMode.Write);
            }
        }
        public override string source => @"
// Clear forces
force = 0;

#ifdef FLUVIO_INDEX_GRID
// Clear grid index
gridIndex = 0;

// Clear neighbor count
neighborCount = 0;
#endif";
    }
}
