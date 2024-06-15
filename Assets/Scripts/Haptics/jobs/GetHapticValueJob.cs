using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct GetHapticValueJob : IJob
{
    public int At;
    public bool Inverted;
    
    [ReadOnly] public NativeArray<FunAction> actions;
    
    [WriteOnly] public NativeReference<float> Haptic;

    [BurstCompile]
    public void Execute()
    {
        // exit early cases
        if (actions.Length <= 0)
        {
            Haptic.Value = 0f;
            return; // no funactions
        }

        if (actions.Length == 1)
        {
            Haptic.Value = actions[0].pos * 0.01f; // only one funaction
            return;
        }

        if (actions[^1].at < At)
        {
            Haptic.Value = actions[^1].pos * 0.01f;
            return; // last action is before current at   
        }

        // find the range where "at" is 
        for (int i = 0; i < actions.Length - 1; i++)
        {
            if (actions[i].at >= At)
            {
                float value = actions[i].pos * 0.01f;
                Haptic.Value = Inverted ? 1f - value : value;
                return;
            }

            if (actions[i + 1].at > At)
            {
                float t = (At - actions[i].at) / (float)(actions[i + 1].at - actions[i].at);
                float value = math.lerp(actions[i].pos, actions[i + 1].pos, t) * 0.01f;
                Haptic.Value = Inverted ? 1f - value : value;
                return;
            }
        }
    }
}