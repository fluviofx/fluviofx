namespace FluvioFX.Editor.Blocks
{
    internal interface ICollisionSettings
    {
        bool BoundaryPressure
        {
            get;
        }
        bool BoundaryViscosity
        {
            get;
        }
        bool RepulsionForce
        {
            get;
        }
        bool RoughSurface
        {
            get;
        }
    }
}
