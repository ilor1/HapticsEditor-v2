using UnityEngine;
using UnityEngine.UIElements;

public class TitleBar : UIBehaviour
{
    [SerializeField]
    private MainUI _mainUI;

    private Label _titleText;

    private void OnEnable()
    {
        _mainUI.RootCreated += Generate;
        FileDropdownMenu.FunscriptPathLoaded += UpdateLabel;
    }

    private void OnDisable()
    {
        _mainUI.RootCreated -= Generate;
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