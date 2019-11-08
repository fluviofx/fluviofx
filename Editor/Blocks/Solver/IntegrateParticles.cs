using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluvioFX.Editor.Integrators;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace FluvioFX.Editor.Blocks
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
            Integrator.Get(IntegrationMode).GetInputProperties(base.inputProperties);

        protected internal override SolverDataParameters solverDataParameters =>
            Integrator.Get(IntegrationMode).solverDataParameters;

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in base.parameters.Concat(Integrator.Get(IntegrationMode).GetParameters()))
                {
                    yield return p;
                }
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes => Integrator.Get(IntegrationMode).GetAttributes();

        public override string source => Integrator.Get(IntegrationMode).GetSource(GetData());
    }
}
