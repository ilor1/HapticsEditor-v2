﻿using System;
using UnityEngine.UIElements;

public class TitleBar : UIBehaviour
{
    private Label _titleText;

    public static Action TitleBarCreated;
    public static TitleBar Singleton;

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    private void OnEnable()
    {
        MainUI.RootCreated += Generate;
        FunscriptLoader.FunscriptLoaded += UpdateLabel;
        ChangeManager.OnChange += MarkLabelDirty;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
        FunscriptLoader.FunscriptLoaded -= UpdateLabel;
        ChangeManager.OnChange -= MarkLabelDirty;
    }

    private void Generate(VisualElement root)
    {
        VisualElement titleBar = root.Query("title-bar");

        _titleText = Create<Label>();
        _titleText.text = "No funscript loaded.";
        titleBar.Add(_titleText);

        TitleBarCreated?.Invoke();
    }

    private void UpdateLabel(string funscriptPath)
    {
        _titleText.text = funscriptPath;
    }

    private void MarkLabelDirty()
    {
        if (!Singleton._titleText.text.EndsWith("*"))
        {
            Singleton._titleText.text = $"{Singleton._titleText.text}*";
        }
    }

    public static void MarkLabelClean()
    {
        if (Singleton._titleText.text.EndsWith("*"))
        {
            Singleton._titleText.text = Singleton._titleText.text.Substring(0, Singleton._titleText.text.Length - 1);
        }
    }
}