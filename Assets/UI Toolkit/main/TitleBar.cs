using UnityEngine;
using UnityEngine.UIElements;

public class TitleBar : UIBehaviour
{
    private Label _titleText;

    private void OnEnable()
    {
        MainUI.RootCreated += Generate;
        FileDropdownMenu.FunscriptPathLoaded += UpdateLabel;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
        FileDropdownMenu.FunscriptPathLoaded -= UpdateLabel;
    }

    private void Generate(VisualElement root)
    {
        var titleBar = Create("title-bar");
        root.Add(titleBar);

        _titleText = Create<Label>("title-label");
        _titleText.text = "No funscript loaded.";
        titleBar.Add(_titleText);
    }

    private void UpdateLabel(string funscriptPath)
    {
        _titleText.text = funscriptPath;
    }
}