using UnityEngine;

public class FreeformManager : MonoBehaviour
{
    public static FreeformManager Singleton;
    
    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }
}