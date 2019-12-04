namespace FluvioFX.Editor
{
    public static class FluvioFXSettings
    {
        public const float kEpsilon = 9.99999944e-11f;
        public const float kMaxSqrVelocityChange = 10000.0f;
        public const int kMaxBucketCount = 32;
        public const int kMaxNeighborCount = kMaxBucketCount * 27 - 1;
        public const float kAutoParticleSizeFactor = 0.25f;
    }
}
