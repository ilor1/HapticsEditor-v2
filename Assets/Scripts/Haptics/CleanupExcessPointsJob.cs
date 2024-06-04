using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct DouglasPeuckerJob : IJob
{
    public NativeList<FunAction> Actions;

    [BurstCompile]
    public void Execute()
    {
        NativeList<FunAction> result = new NativeList<FunAction>(Allocator.Temp);
        Simplify(in Actions, 1f, ref result);
        Actions.CopyFrom(result);
    }

    [BurstCompile]
    private static void Simplify(in NativeList<FunAction> points, float epsilon, ref NativeList<FunAction> result)
    {
        if (points.Length < 3) result = points;
        else
        {
            NativeList<int> keep = new NativeList<int>(Allocator.Temp);
            keep.Add(0);
            keep.Add(points.Length - 1);

            DouglasPeuckerRecursive(in points, 0, points.Length - 1, epsilon, ref keep);

            keep.Sort();

            for (int i = 0; i < keep.Length; i++)
            {
                result.Add(points[keep[i]]);
            }
        }
    }

    [BurstCompile]
    private static void DouglasPeuckerRecursive(in NativeList<FunAction> points, int startIndex, int endIndex, float epsilon, ref NativeList<int> keep)
    {
        float maxDistance = 0;
        int index = startIndex;

        for (int i = startIndex + 1; i < endIndex; i++)
        {
            float distance = PerpendicularDistance(points[startIndex], points[endIndex], points[i]);
            if (distance > maxDistance)
            {
                index = i;
                maxDistance = distance;
            }
        }

        if (maxDistance > epsilon)
        {
            keep.Add(index);
            DouglasPeuckerRecursive(in points, startIndex, index, epsilon, ref keep);
            DouglasPeuckerRecursive(in points, index, endIndex, epsilon, ref keep);
        }
    }

    [BurstCompile]
    private static float PerpendicularDistance(in FunAction point1, in FunAction point2, in FunAction point)
    {
        float2 p1 = new float2(point1.at, point1.pos);
        float2 p2 = new float2(point2.at, point2.pos);
        float2 p = new float2(point.at, point.pos);

        float2 p1p2 = p2 - p1;
        float2 p1p = p - p1;

        float area = math.abs(p1p2.x * p1p.y - p1p2.y * p1p.x);
        float p1p2Length = math.length(p1p2);

        return area / p1p2Length;
    }
}

// [BurstCompile]
// public struct CleanupExcessPointsJob : IJob
// {
//     public NativeList<FunAction> Actions;
//
//     [BurstCompile]
//     public void Execute()
//     {
//         if (Actions.Length < 3) return;
//
//         for (int i = Actions.Length - 3; i >= 0; i--)
//         {
//             int pos0 = Actions[i].pos;
//             int pos1 = Actions[i + 1].pos;
//             int pos2 = Actions[i + 2].pos;
//             int at0 = Actions[i].at;
//             int at1 = Actions[i + 1].at;
//             int at2 = Actions[i + 2].at;
//
//             // if three consecutive pos values are the same, remove the excess middle one. 
//             if (pos0 == pos1 && pos0 == pos2)
//             {
//                 Actions.RemoveAt(i + 1);
//                 continue;
//             }
//
//             // if we have two points close to each other, and the pos value barely changes, remove the first one
//             if (math.abs(at0 - at2) < 15 && math.abs(pos0 - pos2) < 5)
//             {
//                 Actions.RemoveAt(i + 1);
//                 continue;
//             }
//         }
//     }
// }

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