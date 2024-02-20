using System;
using UnityEngine;
using UnityEngine.UIElements;

public class TitleBar : UIBehaviour
{
    private Label _titleText;

    public static Action TitleBarCreated;
    
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
}