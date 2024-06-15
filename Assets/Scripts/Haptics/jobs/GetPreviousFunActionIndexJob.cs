using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct GetPreviousFunActionIndexJob : IJob
{
    public bool Selected;
    public int At;

    [ReadOnly] public NativeArray<FunAction> Actions;
    [WriteOnly] public NativeReference<int> Index;

    [BurstCompile]
    public void Execute()
    {
        if (!Selected)
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

        // If there's only one action check if cursor is after it
        if (Actions.Length == 1)
        {
            Index.Value = Actions[0].at < At ? 0 : -1;
            return;
        }

        // Go through funActions
        for (int i = 0; i < Actions.Length; i++)
        {
            if (i == Actions.Length - 1 && Actions[i].at <= At)
            {
                Index.Value = i;
                return;
            }

            if (Actions[i].at <= At && Actions[i + 1].at > At)
            {
                // found next
                Index.Value = i;
                return;
            }

            if (Actions[i].at > At)
            {
                // failed to find next funAction
                Index.Value = -1;
                return;
            }
        }

        // failed to find next funAction
        Index.Value = -1;
    }
}