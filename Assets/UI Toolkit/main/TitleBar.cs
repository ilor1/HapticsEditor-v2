using UnityEngine;
using UnityEngine.UIElements;

public class TitleBar : UIBehaviour
{
    [SerializeField]
    private MainUI _mainUI;

    private void OnEnable()
    {
        _mainUI.RootCreated += Generate;
    }

    private void OnDisable()
    {
        _mainUI.RootCreated -= Generate;
    }

    private void Generate(VisualElement root)
    {
        var titleBar = Create("title-bar");
        root.Add(titleBar);

        var titleText = Create<Label>("title-label");
        titleText.text = "No file loaded.";
        titleBar.Add(titleText);
    }
}