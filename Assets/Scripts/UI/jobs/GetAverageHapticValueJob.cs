using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct GetAverageHapticValueJob : IJobParallelFor
{
    [ReadOnly] public float MillisecondsPerPixel;
    [ReadOnly] public NativeArray<FunAction> Funactions;

    [WriteOnly] public NativeParallelHashMap<int, float>.ParallelWriter AverageHapticAtPixel;
    [WriteOnly] public NativeParallelHashMap<int, float>.ParallelWriter HighestHapticAtPixel;
    [WriteOnly] public NativeParallelHashMap<int, float>.ParallelWriter LowestHapticAtPixel;

    [BurstCompile]
    public void Execute(int x)
    {
        int at0 = (int)math.round(x * MillisecondsPerPixel);
        int at1 = (int)math.round(x * MillisecondsPerPixel + MillisecondsPerPixel);

        float highest = 0f;
        float lowest = 1f;
        float average = 0f;

        // no funactions
        if (Funactions.Length <= 0)
        {
            AverageHapticAtPixel.TryAdd(x, 0f);
            HighestHapticAtPixel.TryAdd(x, 0f);
            return;
        }

        // only one funaction
        if (Funactions.Length == 1)
        {
            float value = Funactions[0].pos * 0.01f;
            AverageHapticAtPixel.TryAdd(x, value);
            HighestHapticAtPixel.TryAdd(x, value);
            return;
        }

        float aPos = GetHapticValue(at0);
        float aAt = at0;
        float bPos = GetHapticValue(at1);
        float bAt = at1;

        highest = math.max(highest, aPos);
        highest = math.max(highest, bPos);
        lowest = math.min(lowest, aPos);
        lowest = math.min(lowest, bPos);


        for (int i = 0; i < Funactions.Length; i++)
        {
            if (Funactions[i].at <= at0) continue;
            if (Funactions[i].at >= at1)
            {
                average += (aPos + bPos) * 0.5f * (bAt - aAt);
                break;
            }

            average += (aPos + Funactions[i].pos * 0.01f) * 0.5f * (Funactions[i].at - aAt);
            aAt = Funactions[i].at;
            aPos = Funactions[i].pos * 0.01f;
            highest = math.max(highest, aPos);
            lowest = math.min(lowest, aPos);
        }

        // We don't need to walk through each millisecond... we can multiply the values with atvalues or something

        average /= (at1 - at0);

        AverageHapticAtPixel.TryAdd(x, average);
        HighestHapticAtPixel.TryAdd(x, highest);
        LowestHapticAtPixel.TryAdd(x, lowest);
    }

    private float GetHapticValue(int at)
    {
        if (Funactions[^1].at < at)
        {
            // last action is before current at
            return Funactions[^1].pos * 0.01f;
        }

        // find the range where "at" is 
        for (int i = 0; i < Funactions.Length - 1; i++)
        {
            if (Funactions[i].at >= at)
            {
                return Funactions[i].pos * 0.01f;
            }

            if (Funactions[i + 1].at > at)
            {
                float t = (at - Funactions[i].at) / (float)(Funactions[i + 1].at - Funactions[i].at);
                return math.lerp(Funactions[i].pos, Funactions[i + 1].pos, t) * 0.01f;
            }
        }

        return 0f;
    }
}