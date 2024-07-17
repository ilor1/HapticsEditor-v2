using UnityEngine;
using UnityEngine.UIElements;

public class HapticGeneratorMenu : UIBehaviour
{
    public static HapticGeneratorMenu Singleton;

    private VisualElement _root;
    private VisualElement _popup;
    private VisualElement _container;

    private MinMaxSlider _minMaxSlider;
    private Slider _strengthSlider;

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
        _popup = Create("popup");
        _container = Create("popup-container", "background--medium", "bordered", "rounded");
        _container.name = "haptic-generator-popup";

        // min, max -hz
        _minMaxSlider = Create<MinMaxSlider>("min-max-hz");
        _minMaxSlider.lowLimit = 0f;
        _minMaxSlider.highLimit = 1f;
        _minMaxSlider.value = new Vector2(0.25f, 0.5f);
        _minMaxSlider.label = "Min-Max Hz range";
        _container.Add(_minMaxSlider);

        // strength
        _strengthSlider = Create<Slider>("strength");
        _strengthSlider.label = "Strength multiplier";
        _strengthSlider.lowValue = 0.01f;
        _strengthSlider.value = 0.1f;
        _strengthSlider.highValue = 1f;
        _container.Add(_strengthSlider);

        // Generate!
        var generatorButtons = Create("popup-container-buttons");
        var generateButton = Create<Button>();
        generateButton.clicked += GenerateHaptics;
        generateButton.text = "Generate";
        generatorButtons.Add(generateButton);

        // close
        var closeButton = Create<Button>();
        closeButton.clicked += OnClose;
        closeButton.text = "Close";
        generatorButtons.Add(closeButton);

        _container.Add(generatorButtons);
        _popup.Add(_container);
    }

    private void GenerateHaptics()
    {
        Debug.Log("Generating haptics!");
        AudioClipFrequencyAnalyzer.Singleton.AnalyzeClip(_strengthSlider.value, _minMaxSlider.value.x, _minMaxSlider.value.y);
    }

    private void OnClose()
    {
        _root.Remove(_popup);
        InputManager.InputBlocked = false;
    }

    public void Open(VisualElement root)
    {
        InputManager.InputBlocked = true;
        root.Add(_popup);
    }
}