using System;
using UnityEngine;

public class ChangeManager : MonoBehaviour
{
    public static ChangeManager Instance;

    public static Action OnChange;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }
}