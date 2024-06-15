using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct GetNextFunActionIndexJob : IJob
{
    public bool IgnoreSelection;
    public bool Selected;
    public int At;

    [ReadOnly] public NativeArray<FunAction> Actions;
    [WriteOnly] public NativeReference<int> Index;

    [BurstCompile]
    public void Execute()
    {
        // No funscript
        if (!Selected && !IgnoreSelection)
        {
            Index.Value = -1;
            return;
        }

        // we assume fun actions are sorted in correct order
        // no funActions
        if (Actions.Length == 0)
        {
            Index.Value = -1;
            return;
        }

        // Go through funActions
        for (int i = 0; i < Actions.Length; i++)
        {
            if (Actions[i].at >= At)
            {
                // found funAction that is later than current
                Index.Value = i;
                return;
            }
        }

        // failed to find next funAction
        Index.Value = -1;
    }
}