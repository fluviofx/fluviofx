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
    [VFXInfo(category = "FluvioFX")]
    class IndexGrid : FluvioFXBlock
    {
        public override string name
        {
            get
            {
                return "Index Grid";
            }
        }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                // yield return new VFXAttributeInfo(FluvioFXAttribute.GridIndex, VFXAttributeMode.Write);
            }
        }
#pragma warning disable 649
        public class InputProperties
        {
            public Vector4 KernelSize = Vector4.one;
            public Texture2D FluvioSolverData;
            public Vector2 FluvioSolverDataSize;
        }
#pragma warning restore 649
        public override string source => @"
#ifdef FLUVIO_INDEX_GRID
// indices are +1 for GPU grids (0 = not alive)
gridIndex = GetGridIndexFromPosition(position, KernelSize.x) + 1;
#endif
";
    }
}
