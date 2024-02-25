using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct PreProcessSamplesJob : IJobParallelFor
{
    [ReadOnly] public int SamplesPerPixel;
    [ReadOnly] public NativeArray<float> Samples;
    [ReadOnly] public int Channels;
    [ReadOnly] public float MaxSampleValue;

    [WriteOnly] public NativeArray<float> LeftHigh;
    [WriteOnly] public NativeArray<float> LeftRMS;
    [WriteOnly] public NativeArray<float> RightHigh;
    [WriteOnly] public NativeArray<float> RightRMS;
    
    public void Execute(int index)
    {
        float leftRms = 0;
        float rightRms = 0;
        float leftHighestSample = 0;
        float rightHighestSample = 0;
        int firstSampleIndex = SamplesPerPixel * index;
        int lastSampleIndex = firstSampleIndex + SamplesPerPixel;
        
        for (int i = firstSampleIndex; i < lastSampleIndex; i++)
        {
            // https://manual.audacityteam.org/man/audacity_waveform.html
            float leftSample = 0;
            float rightSample = 0;
       
            int leftIndex = i * Channels;
            int rightIndex = i * Channels + 1;

            leftIndex = leftIndex % Samples.Length;
            rightIndex = rightIndex % Samples.Length;
            
            // Get samples
            leftSample = math.abs(Samples[leftIndex]) / MaxSampleValue;
            rightSample = math.abs(Samples[rightIndex]) / MaxSampleValue;

            // Get highest sample values
            leftHighestSample = leftSample > leftHighestSample ? leftSample : leftHighestSample;
            rightHighestSample = rightSample > rightHighestSample ? rightSample : rightHighestSample;

            // Get RMS sample values
            leftRms += leftSample;
            rightRms += rightSample;
        }
        
        LeftHigh[index] = leftHighestSample;
        RightHigh[index] = rightHighestSample;

        // average the samples
        LeftRMS[index] = leftRms / SamplesPerPixel;
        RightRMS[index] = rightRms / SamplesPerPixel;
    }
}