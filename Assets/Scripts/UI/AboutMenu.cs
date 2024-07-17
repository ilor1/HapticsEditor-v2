using UnityEngine;
using UnityEngine.UIElements;

public class AboutMenu : UIBehaviour
{
    public static AboutMenu Singleton;
    [SerializeField] private ProjectInfoSO _projectInfoSo;

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
        _container.name = "about-popup";
        
        // close
        var aboutButtons = Create();
        var closeButton = Create<Button>();
        closeButton.clicked += OnClose;
        closeButton.text = "Close";
        aboutButtons.Add(closeButton);

        // version
        var versionLabel = Create<Label>();
        versionLabel.enableRichText = true;
        versionLabel.text = $"<b>Haptics Editor</b> Version: {_projectInfoSo.Version}";
        _container.Add(versionLabel);
        
        // support links
        // ko-fi: https://ko-fi.com/ilori
        var kofiLabel = Create<Label>();
        kofiLabel.text = "If you like using this Haptics Editor, and want to donate towards its development, or just in general make my day, you can...";
        kofiLabel.enableRichText = true;
        
        var kofiButton = Create<Label>();
        kofiButton.enableRichText = true;
        kofiButton.text = "<b><color=#C840C0><size=24>Buy ilori a ko-fi</size></color></b>";
        kofiButton.RegisterCallback<ClickEvent>(evt => Application.OpenURL("https://ko-fi.com/ilori"));
        _container.Add(kofiLabel);
        _container.Add(kofiButton);

        // copyright
        var copyrightLabel = Create<Label>();
        copyrightLabel.text = $"Copyright \u00a9 2024 ilori";
        _container.Add(copyrightLabel);
        
        _container.Add(aboutButtons);
        _popup.Add(_container);
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