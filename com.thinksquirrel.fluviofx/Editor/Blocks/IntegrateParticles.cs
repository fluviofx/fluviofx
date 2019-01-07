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
    class IntegrateParticles : FluvioFXBlock
    {
        public override string name
        {
            get
            {
                return "Integrate Particles";
            }
        }
        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this))
                    yield return p;

                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "dt");
            }
        }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(FluvioFXAttribute.Force, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
            }
        }
#pragma warning disable 649
        public class InputProperties
        {
            [Tooltip("Controls the mass of each fluid particle.")]
            public float ParticleMass = 7.625f;
            public Vector4 KernelSize = Vector4.one;
            public uint SolverIterations = 2;
        }
#pragma warning restore 649
        public override string source => $@"
float3 t;
float invMass = 1.0f / ParticleMass;
float3 acceleration = force * invMass;
float dtIter = dt * (1.0f / float(SolverIterations));
for (uint iter = 0; iter < SolverIterations; ++iter)
{{
    t = dtIter * acceleration;
    if (any(isnan(t) || isinf(t)) || dot(t, t) > (FLUVIO_MAX_SQR_VELOCITY_CHANGE * KernelSize.w * KernelSize.w))
    {{
        t = 0;
    }}

    velocity += t;
}}";
    }
}
