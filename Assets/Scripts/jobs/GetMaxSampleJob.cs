using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct GetMaxSampleJob : IJob
{
    [ReadOnly] public NativeArray<float> Samples;
    [WriteOnly] public NativeArray<float> MaxSample;

    [BurstCompile]
    public void Execute()
    {
        float maxSample = 0f;
        for (int i = 0; i < Samples.Length; i++)
        {
            maxSample = math.max(maxSample, math.abs(Samples[i]));
        }

        MaxSample[0] = maxSample;
    }
}