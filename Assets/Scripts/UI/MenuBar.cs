using UnityEngine.UIElements;

public class MenuBar : UIBehaviour
{
    private CustomDropDownMenu _fileDropdown;
    private CustomDropDownMenu _editDropdown;

    private VisualElement _layersWindow;
    private VisualElement _devicesContainer;

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
        VisualElement menuBar = root.Query("menu-bar");

        // File Dropdown
        _fileDropdown = CreateDropdownMenu(root);
        _fileDropdown.Append("Load Audio", FileDropdownMenu.OnLoadAudioClick);
        _fileDropdown.Append("Load Funscript", FileDropdownMenu.OnLoadFunscriptClick);
        _fileDropdown.Append("Save Funscript", FileDropdownMenu.OnSaveClick);
        _fileDropdown.Append("Exit", FileDropdownMenu.OnExitClick);

        var fileButton = Create<Button>("no-border");
        fileButton.text = "File";
        fileButton.clicked += () =>
        {
            _fileDropdown.style.left = new StyleLength(0f); // menu-bar padding-left
            // Toggle dropdown on click
            _fileDropdown.Toggle(fileButton);
        };
        menuBar.Add(fileButton);

        // Settings Dropdown
        _editDropdown = CreateDropdownMenu(root);
        _editDropdown.Append("Settings", EditDropdownMenu.OnSettingsClick);
        _editDropdown.Append("Bindings", EditDropdownMenu.OnBindingsClick);

        var editButton = Create<Button>("no-border");
        editButton.text = "Edit";
        editButton.clicked += () =>
        {
            // menu-bar padding-left + fileButton padding-left + padding-right
            _editDropdown.style.left = new StyleLength(50f);

            // Toggle dropdown on click
            _editDropdown.Toggle(editButton);
        };
        menuBar.Add(editButton);

        var patternsButton = Create<Button>("no-border");
        patternsButton.text = "Patterns";
        patternsButton.clicked += () => { PatternCreatorMenu.Singleton.Open(); };
        menuBar.Add(patternsButton);

        var hapticGeneratorButton = Create<Button>("no-border");
        hapticGeneratorButton.text = "Generator";
        hapticGeneratorButton.clicked += () => { HapticGeneratorMenu.Singleton.Open(root); };
        menuBar.Add(hapticGeneratorButton);
        
        var metadataButton = Create<Button>("no-border");
        metadataButton.text = "Metadata";
        metadataButton.clicked += () => { MetadataMenu.Singleton.Open(root); };
        menuBar.Add(metadataButton);

        var aboutButton = Create<Button>("no-border");
        aboutButton.text = "About";
        aboutButton.clicked += () => { AboutMenu.Singleton.Open(root); };
        menuBar.Add(aboutButton);

        var toggleLayersLabel = Create<Label>();
        toggleLayersLabel.text = "Show Layers";
        var toggleLayers = Create<Toggle>();
        toggleLayers.SetValueWithoutNotify(true);
        toggleLayers.RegisterValueChangedCallback(OnToggleLayers);
        _layersWindow = root.Query("layers-container");
        _devicesContainer = root.Query("devices-container");
        menuBar.Add(toggleLayersLabel);
        menuBar.Add(toggleLayers);

        var intifaceContainer = Create("row-reverse");
        intifaceContainer.name = "intiface-container";
        var intifaceLabel = Create<Label>();
        intifaceLabel.text = "Intiface Central";
        var intifaceToggle = Create<Toggle>();
        intifaceToggle.RegisterValueChangedCallback(OnIntifaceToggle);

        intifaceContainer.Add(intifaceToggle);
        intifaceContainer.Add(intifaceLabel);
        menuBar.Add(intifaceContainer);
    }

    private void OnToggleLayers(ChangeEvent<bool> evt)
    {
        _layersWindow.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        _devicesContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnIntifaceToggle(ChangeEvent<bool> evt)
    {
        IntifaceManager.Singleton.enabled = evt.newValue;
    }

    private CustomDropDownMenu CreateDropdownMenu(VisualElement root)
    {
        var dropdown = Create<CustomDropDownMenu>("dropdown-menu", "column", "background--medium", "border-bottom", "border-left", "border-right", "rounded", "hide-overflow");
        dropdown.style.display = DisplayStyle.None;
        root.Add(dropdown);
        return dropdown;
    }
}