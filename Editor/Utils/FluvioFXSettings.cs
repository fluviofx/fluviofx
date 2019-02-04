namespace Thinksquirrel.FluvioFX.Editor
{
    public static class FluvioFXSettings
    {
        public const float kEpsilon = 0.000001f;
        public const float kMaxSqrVelocityChange = 100.0f;
        public const int kMaxBucketCount = 64;
        public const int kMaxNeighborCount = kMaxBucketCount * 27 - 1;
        public const float kAutoParticleSizeFactor = 0.25f;
    }
}
