using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct RemovePointsJob : IJob
{
    public int Start;
    public int End;

    public NativeList<FunAction> Actions;

    [BurstCompile]
    public void Execute()
    {
        for (int i = Actions.Length - 1; i >= 0; i--)
        {
            int at = Actions[i].at;
            if (at < Start) continue;
            if (at > End) continue;

            Actions.RemoveAt(i);
        }
    }
}