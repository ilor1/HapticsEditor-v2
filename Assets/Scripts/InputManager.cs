using System.IO;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Singleton;
    public Bindings Controls;
    private string _bindingsPath = $"{Application.streamingAssetsPath}/bindings.json";

    // To create a new binding:
    // 1. Add it to Bindings.cs (struct)
    // 2. Add it to ControlName.cs (enum)
    // 3. Add it to BindingsMenu.SetBindingKey
    // 4. Access it using InputManager.Singleton.Controls.YourBind
    // - it gets added to bindings.json through the runtime binding system
    // - you should still add all bindings in the inspector to have default values (if json doesn't exist)

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    private void Start()
    {
        LoadBindings();
    }

    public void SaveBindings()
    {
        // Save bindings
        string json = JsonUtility.ToJson(Controls, true);
        File.WriteAllText(_bindingsPath, json);
        Debug.Log($"InputManager: Bindings saved: ({_bindingsPath})");
    }

    public void LoadBindings()
    {
        if (File.Exists(_bindingsPath))
        {
            // Load Bindings if the file exists
            string json = File.ReadAllText(_bindingsPath);

            Controls = JsonUtility.FromJson<Bindings>(json);
            Debug.Log($"InputManager: Bindings loaded: ({_bindingsPath})");
            
            // Note if the file exists and any of the binds are missing, those will be unbound (null)
            // This is fine and expected I guess...
        }
        else
        {
            // Otherwise save default bindings from what's setup in the inspector.
            SaveBindings();
        }
    }
}