using System;
using UnityEngine;
using UnityEngine.UIElements;

public class TitleBar : UIBehaviour
{
    private Label _titleText;

    public static Action TitleBarCreated;

    public static TitleBar Singleton;

    private bool _isDirty = false;

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    private void OnEnable()
    {
        MainUI.RootCreated += Generate;
        FunscriptLoader.FunscriptLoaded += UpdateLabel;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
        FunscriptLoader.FunscriptLoaded -= UpdateLabel;
    }

    private void Generate(VisualElement root)
    {
        VisualElement titleBar = root.Query(className: "title-bar");

        _titleText = Create<Label>("title-label");
        _titleText.text = "No funscript loaded.";
        titleBar.Add(_titleText);

        TitleBarCreated?.Invoke();
    }

    private void UpdateLabel(string funscriptPath)
    {
        _titleText.text = funscriptPath;
    }

    public static void MarkLabelDirty()
    {
        if (!Singleton._isDirty && !Singleton._titleText.text.EndsWith("*"))
        {
            Singleton._titleText.text = $"{Singleton._titleText.text}*";
            Singleton._isDirty = true;
        }
    }

    public static void MarkLabelClean()
    {
        if (Singleton._titleText.text.EndsWith("*"))
        {
            Singleton._titleText.text = Singleton._titleText.text.Substring(0, Singleton._titleText.text.Length - 1);
        }

        Singleton._isDirty = false;
    }
}