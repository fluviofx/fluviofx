using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Thinksquirrel.FluvioFX.Editor.Integrators;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace Thinksquirrel.FluvioFX.Editor.Blocks
{
    [VFXInfo(category = "FluvioFX/Solver")]
    class IntegrateParticles : FluvioFXBlock
    {
        [VFXSetting]
        public IntegrationMode IntegrationMode = IntegrationMode.Verlet;

        public override string name
        {
            get
            {
                return "Integrate Particles";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties =>
            Integrator.Get(IntegrationMode).GetInputProperties();

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this).Concat(Integrator.Get(IntegrationMode).GetParameters()))
                {
                    yield return p;
                }
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes => Integrator.Get(IntegrationMode).GetAttributes(hasLifetime);

        public override string source => Integrator.Get(IntegrationMode).GetSource();
    }
}
