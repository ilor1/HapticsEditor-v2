using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

public class MenuBar : UIBehaviour
{
    [SerializeField]
    private MainUI _mainUI;

    private CustomDropDownMenu _fileDropdown;
    private CustomDropDownMenu _settingsDropdown;

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
        var menuBar = Create("menu-bar");
        root.Add(menuBar);

        // File Dropdown
        _fileDropdown = CreateDropdownMenu(root);
        _fileDropdown.Append("Load Audio", FileDropdownMenu.OnLoadAudioClick);
        _fileDropdown.Append("Load Funscript", FileDropdownMenu.OnLoadFunscriptClick);
        _fileDropdown.Append("Save Funscript", FileDropdownMenu.OnSaveClick);
        _fileDropdown.Append("Exit", FileDropdownMenu.OnExitClick);

        var fileButton = Create<Button>("menu-button");
        fileButton.text = "File";
        fileButton.clicked += () =>
        {
            _fileDropdown.style.left = new StyleLength(10f); // menu-bar padding-left
            _fileDropdown.style.top = new StyleLength(80f);

            // Toggle dropdown on click
            _fileDropdown.Toggle(fileButton);
        };
        menuBar.Add(fileButton);

        // Settings Dropdown
        _settingsDropdown = CreateDropdownMenu(root);
        _settingsDropdown.Append("Edit bindings", SettingsDropdownMenu.OnEditBindingsClick);

        var settingsButton = Create<Button>("menu-button");
        settingsButton.text = "Settings";
        settingsButton.clicked += () =>
        {
            // menu-bar padding-left + fileButton padding-left + padding-right
            _settingsDropdown.style.left = new StyleLength(fileButton.contentRect.width + 30);
            _settingsDropdown.style.top = new StyleLength(80f);

            // Toggle dropdown on click
            _settingsDropdown.Toggle(settingsButton);
        };
        menuBar.Add(settingsButton);

        var patternsButton = Create<Button>("menu-button");
        patternsButton.text = "Patterns";
        patternsButton.clicked += () =>
        {
            // Open Patterns window
        };
        menuBar.Add(patternsButton);

        var metadataButton = Create<Button>("menu-button");
        metadataButton.text = "Metadata";
        metadataButton.clicked += () =>
        {
            // Open Metadata window
        };
        metadataButton.SetEnabled(false); // enabled only when a file is loaded
        menuBar.Add(metadataButton);
    }

    private CustomDropDownMenu CreateDropdownMenu(VisualElement root)
    {
        var dropdown = Create<CustomDropDownMenu>("menu-dropdown");
        dropdown.style.display = DisplayStyle.None;
        root.Add(dropdown);
        return dropdown;
    }
}