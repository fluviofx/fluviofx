using System;
using System.Collections.Generic;
using System.Linq;
using Thinksquirrel.FluvioFX.Editor;
using UnityEditor.VFX;
using UnityEngine;

namespace Thinksquirrel.FluvioFX.Editor.Blocks
{
    [VFXInfo(category = "FluvioFX/Solver")]
    class InitializeSolver : FluvioFXBlock
    {
        public override string name
        {
            get
            {
                return "Initialize Solver";
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(FluvioFXAttribute.Force, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.OldPosition, VFXAttributeMode.Write);
                // yield return new VFXAttributeInfo(FluvioFXAttribute.GridIndex, VFXAttributeMode.Write);
                // yield return new VFXAttributeInfo(FluvioFXAttribute.NeighborCount, VFXAttributeMode.Write);
            }
        }

        protected override bool solverDataProperty => true;
        protected override bool findSolverData => false;

        public override string source => @"
// Set old position
oldPosition = position;

// Clear forces
force = 0;

// Set mass
mass = solverData_Fluid_ParticleMass;

#ifdef FLUVIO_INDEX_GRID
// Clear grid index
gridIndex = 0;

// Clear neighbor count
neighborCount = 0;
#endif";
    }
}
