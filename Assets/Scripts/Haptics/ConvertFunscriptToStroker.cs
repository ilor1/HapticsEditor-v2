using UnityEngine;

public class ConvertFunscriptToStroker : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() { }

    // Update is called once per frame
    void Update()
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||  Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        
        // avoid ctrl-x/ctrl-c/ctrl-v triggering a destructive function by accident!
        if (shift || ctrl || alt) return;
        
        if (InputManager.Singleton.GetKeyDown(ControlName.ConvertForStroker))
        {
            Convert();
        }
    }


    public void Convert()
    {
        for (int i = 0; i < FunscriptRenderer.Singleton.Haptics.Count; i++)
        {
            var haptics = FunscriptRenderer.Singleton.Haptics[i];
            if (!haptics.Selected || !haptics.Visible) continue;

            var actions = haptics.Funscript.actions;

            // reverse through all the points and see if the direction changes
            for (int j = actions.Count - 1; j >= 2; j--)
            {
                FunAction a = actions[j - 2];
                FunAction b = actions[j - 1];
                FunAction c = actions[j];

                // check if direction has changed between these three points
                bool directionChanged = (a.pos < b.pos && c.pos < b.pos) || (a.pos > b.pos && c.pos > b.pos);
                
                // remove middle point if not
                if (!directionChanged)
                {
                    actions.RemoveAt(j-1);
                }
            }
        }
    }
}