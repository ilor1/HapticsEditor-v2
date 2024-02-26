using UnityEngine.UIElements;

public class MenuBar : UIBehaviour
{
    private CustomDropDownMenu _fileDropdown;
    private CustomDropDownMenu _editDropdown;

    private void OnEnable()
    {
        MainUI.RootCreated += Generate;
    }

    private void OnDisable()
    {
        MainUI.RootCreated -= Generate;
    }

    private void Generate(VisualElement root)
    {
        VisualElement menuBar = root.Query(className: "menu-bar");

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
            _fileDropdown.style.left = new StyleLength(0f); // menu-bar padding-left
            _fileDropdown.style.top = new StyleLength(64f);

            // Toggle dropdown on click
            _fileDropdown.Toggle(fileButton);
        };
        menuBar.Add(fileButton);

        // Settings Dropdown
        _editDropdown = CreateDropdownMenu(root);
        _editDropdown.Append("Settings", EditDropdownMenu.OnSettingsClick);
        _editDropdown.Append("Bindings", EditDropdownMenu.OnBindingsClick);

        var editButton = Create<Button>("menu-button");
        editButton.text = "Edit";
        editButton.clicked += () =>
        {
            // menu-bar padding-left + fileButton padding-left + padding-right
            _editDropdown.style.left = new StyleLength(fileButton.contentRect.width + 20);
            _editDropdown.style.top = new StyleLength(64f);

            // Toggle dropdown on click
            _editDropdown.Toggle(editButton);
        };
        menuBar.Add(editButton);

        var patternsButton = Create<Button>("menu-button");
        patternsButton.text = "Patterns";
        patternsButton.clicked += () =>
        {
            PatternCreatorMenu.Singleton.Open();
        };
        menuBar.Add(patternsButton);

        var metadataButton = Create<Button>("menu-button");
        metadataButton.text = "Metadata";
        metadataButton.clicked += () => { MetadataMenu.Singleton.Open(root); };
        menuBar.Add(metadataButton);

        var intifaceContainer = Create("intiface-container");
        var intifaceLabel = Create<Label>();
        intifaceLabel.text = "Intiface Central:";
        var intifaceToggle = Create<Toggle>();
        intifaceToggle.RegisterValueChangedCallback(OnIntifaceToggle);
        intifaceContainer.Add(intifaceToggle);
        intifaceContainer.Add(intifaceLabel);
        menuBar.Add(intifaceContainer);
    }

    private void OnIntifaceToggle(ChangeEvent<bool> evt)
    {
        IntifaceManager.Singleton.enabled = evt.newValue;
    }

    private CustomDropDownMenu CreateDropdownMenu(VisualElement root)
    {
        var dropdown = Create<CustomDropDownMenu>("menu-dropdown");
        dropdown.style.display = DisplayStyle.None;
        root.Add(dropdown);
        return dropdown;
    }
}