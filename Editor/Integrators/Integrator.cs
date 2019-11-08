using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluvioFX.Editor.Blocks;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace FluvioFX.Editor.Integrators
{
    internal abstract class Integrator
    {
        private static Lazy<Euler> _euler = new Lazy<Euler>(() => new Euler());
        private static Lazy<Verlet> _verlet = new Lazy<Verlet>(() => new Verlet());
        private static Lazy<VelocityVerlet> _velocityVerlet = new Lazy<VelocityVerlet>(() => new VelocityVerlet());
        public static Integrator Get(IntegrationMode integrationMode)
        {
            switch (integrationMode)
            {
                default:
                    case IntegrationMode.Verlet:
                    return _verlet.Value;
                case IntegrationMode.VelocityVerlet:
                        return _velocityVerlet.Value;
                case IntegrationMode.Euler:
                        return _euler.Value;
            }
        }

        protected virtual IEnumerable<Type> inputPropertyTypes => null;
        protected virtual IEnumerable<VFXNamedExpression> parameters => null;
        protected virtual IEnumerable<VFXAttributeInfo> attributes => null;
        public virtual SolverDataParameters solverDataParameters => SolverDataParameters.None;
        protected abstract string source
        {
            get;
        }

        public IEnumerable<VFXPropertyWithValue> GetInputProperties(IEnumerable<VFXPropertyWithValue> baseProperties)
        {
            foreach (var inputProperty in baseProperties)
            {
                yield return inputProperty;
            }

            if (inputPropertyTypes != null)
            {
                foreach (var type in inputPropertyTypes)
                {
                    if (type != null)
                    {
                        var typeProperties = FluvioFXBlock.PropertiesFromType(type);
                        foreach (var inputProperty in typeProperties)
                        {
                            yield return inputProperty;
                        }
                    }
                }
            }
        }

        public IEnumerable<VFXNamedExpression> GetParameters()
        {
            yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "dt");

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    yield return parameter;
                }
            }
        }

        public IEnumerable<VFXAttributeInfo> GetAttributes()
        {
            yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
            yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(FluvioFXAttribute.Force, VFXAttributeMode.Read);
            yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);

            if (attributes != null)
            {
                foreach (var attribute in attributes)
                {
                    yield return attribute;
                }
            }
        }

        public string GetSource(VFXData data)
        {
            return $@"{FluvioFXBlock.CheckAlive(data)}
float invMass = 1.0f / mass;
float3 acceleration = force * invMass;
float3 v = dt * acceleration;

// Discard excessive velocities
if (any(isnan(v) || isinf(v)) || dot(v, v) > FLUVIO_MAX_SQR_VELOCITY_CHANGE)
{{
    v = 0;
}}
{source}";
        }
    }
}
