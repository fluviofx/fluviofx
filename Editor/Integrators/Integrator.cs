using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace Thinksquirrel.FluvioFX.Editor.Integrators
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

        protected virtual bool preIntegrate => false;
        protected virtual IEnumerable<Type> inputPropertyTypes => null;
        protected virtual IEnumerable<VFXNamedExpression> parameters => null;
        protected virtual IEnumerable<VFXAttributeInfo> attributes => null;
        protected abstract string source
        {
            get;
        }
        protected static IEnumerable<VFXPropertyWithValue> PropertiesFromType(Type type)
        {
            if (type == null)
            {
                return Enumerable.Empty<VFXPropertyWithValue>();
            }

            var instance = System.Activator.CreateInstance(type);
            return type.GetFields()
                .Where(f => !f.IsStatic)
                .Select(f =>
                {
                    var p = new VFXPropertyWithValue();
                    p.property = new VFXProperty(f);
                    p.value = f.GetValue(instance);
                    return p;
                });
        }

#pragma warning disable 649
        private class InputProperties
        {
            public SolverData solverData;
        }
#pragma warning restore 649

        public IEnumerable<VFXPropertyWithValue> GetInputProperties()
        {
            var baseProperties = PropertiesFromType(typeof(InputProperties));
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
                        var typeProperties = PropertiesFromType(type);
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
        public IEnumerable<VFXAttributeInfo> GetAttributes(bool hasLifetime)
        {
            yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
            if (hasLifetime)
            {
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
            }
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

        public string GetSource()
        {
            return $@"
{(preIntegrate ? "position += velocity * dt;" : "")}

float invMass = 1.0f / mass;
float3 acceleration = force * invMass;
float3 v = dt * acceleration;

// Discard excessive velocities
if (any(isnan(v) || isinf(v)) || dot(v, v) > (FLUVIO_MAX_SQR_VELOCITY_CHANGE * solverData_KernelSize.w * solverData_KernelSize.w))
{{
    v = 0;
}}

{source}";
        }
    }
}
