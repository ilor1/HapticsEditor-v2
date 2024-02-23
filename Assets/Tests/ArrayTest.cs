using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class ArrayTest : MonoBehaviour
{
    // 2h * 60min * 60s * 1000ms
    private int[] Pos = new int[2 * 60 * 60 * 1000];

    private Random _random = new Unity.Mathematics.Random(1);

    private void Start()
    {
        for (int i = 0; i < Pos.Length; i++)
        {
            Pos[i] = _random.NextInt(0, 100);
        }
    }

    public void Update()
    {
        WriteToArray();
    }

    private void WriteToArray()
    {
        int timeInMilliseconds = (int)math.round(Time.time * 1000f);
        Pos[timeInMilliseconds] = _random.NextInt(0, 100);
    }
}