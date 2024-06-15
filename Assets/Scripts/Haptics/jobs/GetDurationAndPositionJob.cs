using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


[BurstCompile]
public struct GetDurationAndPositionJob : IJob
{
    public int At;
    public bool Inverted;

    [ReadOnly] public NativeArray<FunAction> Actions;

    [WriteOnly] public NativeReference<uint> Duration;
    [WriteOnly] public NativeReference<double> Position;

    [BurstCompile]
    public void Execute()
    {
        if (Actions.Length <= 0) return; // no funactions
        if (Actions.Length == 1)
        {
            // only one funaction
            Duration.Value = (uint)math.max(0, Actions[0].at - At);
            Position.Value = Inverted ? 1f - Actions[0].pos * 0.01 : Actions[0].pos * 0.01;
            return;
        }

        if (Actions[^1].at < At) return; // last action is before current at

        // set last point as target
        if (Actions[^2].at <= At && Actions[^1].at > At)
        {
            Position.Value = Inverted ? 1f - Actions[^1].pos * 0.01 : Actions[^1].pos * 0.01;
            Duration.Value = (uint)math.max(0, Actions[^1].at - At);
            return;
        }

        // set first point as target
        if (Actions[0].at > At)
        {
            Position.Value = Inverted ? 1f - Actions[0].pos * 0.01 : Actions[0].pos * 0.01;
            Duration.Value = (uint)math.max(0, Actions[0].at - At);
            return;
        }

        // other
        for (int i = 0; i < Actions.Length - 1; i++)
        {
            if (At >= Actions[i].at && At < Actions[i + 1].at)
            {
                Position.Value = Inverted ? 1f - Actions[i + 1].pos * 0.01 : Actions[i + 1].pos * 0.01;
                Duration.Value = (uint)math.max(0, Actions[i + 1].at - At);
            }
        }
    }
}