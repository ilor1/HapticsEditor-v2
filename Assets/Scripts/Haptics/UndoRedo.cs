using System;
using System.Collections.Generic;
using UnityEngine;

public class UndoRedo : MonoBehaviour
{
    public static UndoRedo Instance;
    
    private const int UNDO_POINT_COUNT = 10;

    private List<List<Haptics>> _undoStack = new List<List<Haptics>>();
    private int _undoIndex = -1;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(this);
    }

    private void OnEnable()
    {
        ChangeManager.OnChange += SaveUndoPoint;
    }

    private void OnDisable()
    {
        ChangeManager.OnChange -= SaveUndoPoint;
    }

    private void Update()
    {
        bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

#if UNITY_EDITOR
        // Ctrl-Z does undo in the editor which can break things...
        var undoKeyCode = KeyCode.T;
#else
        var undoKeyCode = KeyCode.Z;
#endif
        if (Input.GetKeyDown(undoKeyCode) && control)
        {
            Undo();
        }

        if (Input.GetKeyDown(KeyCode.Y) && control)
        {
            Redo();
        }
    }

    // When layers get manipulated it's easier to just reset the undo stack
    public void ResetUndoStack()
    {
        _undoIndex = -1;
        _undoStack.Clear();
        SaveUndoPoint();
    }
    
    private void Undo()
    {
        _undoIndex--;
        if (_undoIndex < 0)
        {
            Debug.Log("Tried to undo, but already at first restore point");
            _undoIndex = 0;
            return;
        }
        //Debug.Log("Undo");
        LoadUndoPoint();
    }

    private void Redo()
    {
        _undoIndex++;
        if (_undoIndex >= _undoStack.Count)
        {
            Debug.Log("Tried to redo, but already at last restore point");
            _undoIndex = _undoStack.Count - 1;
            return;
        }
        //Debug.Log("Redo");
        LoadUndoPoint();
    }

    private void SaveUndoPoint()
    {
        // remove undo points after current index
        for (int i = _undoStack.Count - 1; i > _undoIndex; i--)
        {
            _undoStack.RemoveAt(i);
        }

        // Limit the size of the undoStack
        while (_undoStack.Count > UNDO_POINT_COUNT)
        {
            _undoStack.RemoveAt(0);
        }

        // Debug.Log("Storing Undo point");
        var haptics = new List<Haptics>();

        // Handle value/reference types
        for (int i = 0; i < FunscriptRenderer.Singleton.Haptics.Count; i++)
        {
            var src = FunscriptRenderer.Singleton.Haptics[i];

            var actions = new List<FunAction>();
            if (src.Funscript.actions != null)
            {
                actions.AddRange(src.Funscript.actions);
            }

            var notes = new List<Note>();
            if (src.Funscript.notes != null)
            {
                notes.AddRange(src.Funscript.notes);
            }

            var funscript = new Funscript
            {
                actions = actions,
                notes = notes,
                inverted = src.Funscript.inverted,
                metadata = src.Funscript.metadata
            };

            var haptic = new Haptics
            {
                Name = src.Name,
                // we don't want the undo redo to affect layer selections
                // Visible = src.Visible,
                // Selected = src.Selected,
                LineRenderSettings = src.LineRenderSettings,
                Funscript = funscript,
            };

            haptics.Add(haptic);
        }

        _undoStack.Add(haptics);
        _undoIndex = Mathf.Clamp(_undoIndex + 1, 0, _undoStack.Count - 1);

        Debug.Log($"Saved restore point, stack size {_undoStack.Count}, undoIndex {_undoIndex}");
    }

    private void LoadUndoPoint()
    {
        // Debug.Log("Storing Undo point");
        var srcHaptics = _undoStack[_undoIndex];
        var haptics = new List<Haptics>();

        // Handle value/reference types
        for (int i = 0; i < srcHaptics.Count; i++)
        {
            var src = srcHaptics[i];

            var actions = new List<FunAction>();
            actions.AddRange(src.Funscript.actions);

            var notes = new List<Note>();
            notes.AddRange(src.Funscript.notes);

            var funscript = new Funscript
            {
                actions = actions,
                notes = notes,
                inverted = src.Funscript.inverted,
                metadata = src.Funscript.metadata
            };

            var haptic = new Haptics
            {
                Name = src.Name,
                // we don't want the undo redo to affect layer selections
                Visible = FunscriptRenderer.Singleton.Haptics[i].Visible,
                Selected = FunscriptRenderer.Singleton.Haptics[i].Selected,
                LineRenderSettings = src.LineRenderSettings,
                Funscript = funscript,
            };

            haptics.Add(haptic);
        }

        FunscriptRenderer.Singleton.Haptics = haptics;
        FunscriptOverview.Singleton.RenderHaptics();
        
        Debug.Log($"Loaded restore point, stack size {_undoStack.Count}, undoIndex {_undoIndex}");
    }
}