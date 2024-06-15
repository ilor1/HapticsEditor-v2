using UnityEngine;
using UnityEngine.UIElements;

public class SettingsManager : UIBehaviour
{
    public static AppSettings ApplicationSettings;

    private static SettingsManager Singleton;

    private VisualElement _root;
    private VisualElement _popup;
    private VisualElement _container;

    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
        else if (Singleton != this)
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        LoadSettings();
    }

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
        _root = root;
        _popup = Create("settings-popup");
        _container = Create("settings-container");

        Toggle fillModeToggle = CreateInputToggleField("Render funscripts as filled:", _container, "settings-field");
        fillModeToggle.SetValueWithoutNotify(ApplicationSettings.fillMode);

        // Subscribe to value change events if needed
        fillModeToggle.RegisterValueChangedCallback(evt => ApplicationSettings.fillMode = evt.newValue);

        var settingsButtons = Create("settings-buttons");
        var saveButton = Create<Button>();
        saveButton.clicked += OnSave;
        saveButton.text = "Save";
        settingsButtons.Add(saveButton);

        var cancelButton = Create<Button>();
        cancelButton.text = "Cancel";
        cancelButton.clicked += OnCancel;
        settingsButtons.Add(cancelButton);

        _container.Add(settingsButtons);
        _popup.Add(_container);
    }

    private void OnCancel()
    {
        // Close without saving
        _root.Remove(_popup);

        InputManager.InputBlocked = false;
    }

    private void OnSave()
    {
        // Save and close
        SaveAppSettings();
        _root.Remove(_popup);

        InputManager.InputBlocked = false;
    }

    public static void Open()
    {
        // No funscript loaded -> return
        if (FunscriptRenderer.Singleton.Haptics.Count <= 0) return;

        InputManager.InputBlocked = true;
        Singleton._root.Add(Singleton._popup);
    }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey("AppSettings"))
        {
            string json = PlayerPrefs.GetString("AppSettings");
            ApplicationSettings = JsonUtility.FromJson<AppSettings>(json);
        }
        else
        {
            ApplicationSettings = new AppSettings
            {
                fillMode = false
            };
        }
    }

    private void SaveAppSettings()
    {
        var settingsToSave = ApplicationSettings;
        settingsToSave.Mode = ScriptingMode.Default; // Don't store the scripting mode changes.
        
        string json = JsonUtility.ToJson(settingsToSave);
        PlayerPrefs.SetString("AppSettings", json);
        PlayerPrefs.Save();
    }
}