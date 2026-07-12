namespace SDFX.VectorTextureCompiler.Core.Optimize
{
    public enum OptimizationProfile
    {
        Pc = 0,
        Quest = 1
    }

    public readonly struct OptimizationSettings
    {
        public OptimizationSettings(
            float mergeDistanceEpsilon,
            float mergeSizeEpsilon,
            float positionStep,
            float sizeStep,
            float rotationStep,
            float softnessStep)
        {
            MergeDistanceEpsilon = mergeDistanceEpsilon;
            MergeSizeEpsilon = mergeSizeEpsilon;
            PositionStep = positionStep;
            SizeStep = sizeStep;
            RotationStep = rotationStep;
            SoftnessStep = softnessStep;
        }

        public float MergeDistanceEpsilon { get; }
        public float MergeSizeEpsilon { get; }
        public float PositionStep { get; }
        public float SizeStep { get; }
        public float RotationStep { get; }
        public float SoftnessStep { get; }

        public static OptimizationSettings FromProfile(OptimizationProfile profile)
        {
            switch (profile)
            {
                case OptimizationProfile.Quest:
                    return new OptimizationSettings(
                        mergeDistanceEpsilon: 0.0015f,
                        mergeSizeEpsilon: 0.0015f,
                        positionStep: 1f / 256f,
                        sizeStep: 1f / 256f,
                        rotationStep: 0.5f,
                        softnessStep: 1f / 127f);
                default:
                    return new OptimizationSettings(
                        mergeDistanceEpsilon: 0.0005f,
                        mergeSizeEpsilon: 0.0005f,
                        positionStep: 1f / 1024f,
                        sizeStep: 1f / 1024f,
                        rotationStep: 0.25f,
                        softnessStep: 1f / 255f);
            }
        }
    }
}