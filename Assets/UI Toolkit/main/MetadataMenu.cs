using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class MetadataMenu : UIBehaviour
{
    public static MetadataMenu Singleton;

    private VisualElement _popup;
    private VisualElement _container;

    private Metadata _data;

    private TextField _creatorField;
    private TextField _descriptionField;
    private IntegerField _durationField;
    private TextField _licenseField;
    private TextField _notesField;
    private TextField _performersField;
    private TextField _scriptUrlField;
    private TextField _tagsField;
    private TextField _titleField;
    private TextField _typeField;
    private TextField _videoUrlField;
    private IntegerField _rangeField;
    private TextField _versionField;

    private bool _initialized = false;

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

    private void Generate(VisualElement root)
    {
        _popup = Create("metadata-popup");
        _container = Create("metadata-container");

        // Assuming you have TextField elements with the following names in your UXML
        // You may need to adjust the names based on your UXML structure


        _creatorField = CreateInputTextField("Creator", _container);
        _descriptionField = CreateInputTextField("Description", _container);
        _durationField =  CreateInputIntegerField("Duration", _container);
        _licenseField = CreateInputTextField("License", _container);
        _notesField = CreateInputTextField("Notes", _container);
        _performersField = CreateInputTextField("Performers", _container);
        _scriptUrlField = CreateInputTextField("Script-URL", _container);
        _tagsField = CreateInputTextField("Tags", _container);
        _titleField = CreateInputTextField("Title", _container);
        _typeField = CreateInputTextField("Type", _container);
        _videoUrlField = CreateInputTextField("Video-URL", _container);
        _rangeField = CreateInputIntegerField("Range", _container);
        _versionField = CreateInputTextField("Version", _container);

        // Subscribe to value change events if needed
        _creatorField.RegisterValueChangedCallback(evt => _data.creator = evt.newValue);
        _descriptionField.RegisterValueChangedCallback(evt => _data.description = evt.newValue);
        _durationField.RegisterValueChangedCallback(evt => _data.duration = evt.newValue);
        _licenseField.RegisterValueChangedCallback(evt => _data.license = evt.newValue);
        _notesField.RegisterValueChangedCallback(evt => _data.notes = evt.newValue);
        _performersField.RegisterValueChangedCallback(evt => _data.performers = evt.newValue.Split(','));
        _scriptUrlField.RegisterValueChangedCallback(evt => _data.script_url = evt.newValue);
        _tagsField.RegisterValueChangedCallback(evt => _data.tags = evt.newValue.Split(','));
        _titleField.RegisterValueChangedCallback(evt => _data.title = evt.newValue);
        _typeField.RegisterValueChangedCallback(evt => _data.type = evt.newValue);
        _videoUrlField.RegisterValueChangedCallback(evt => _data.video_url = evt.newValue);
        _rangeField.RegisterValueChangedCallback(evt => _data.range = evt.newValue);
        _versionField.RegisterValueChangedCallback(evt => _data.version = evt.newValue);

        _popup.Add(_container);
    }

    private TextField CreateInputTextField(string title, VisualElement parent)
    {
        var container = Create("metadata-field");
        var label = Create<Label>();
        label.text = title;

        var inputField = Create<TextField>();
        container.Add(label);
        container.Add(inputField);

        parent.Add(container);

        return inputField;
    }
    
    private IntegerField CreateInputIntegerField(string title, VisualElement parent)
    {
        var container = Create("metadata-field");
        var label = Create<Label>();
        label.text = title;

        var inputField = Create<IntegerField>();
        container.Add(label);
        container.Add(inputField);

        parent.Add(container);

        return inputField;
    }

    public void Open(VisualElement root)
    {
        // No funscript loaded -> return
        if (FunscriptRenderer.Singleton.Haptics.Count <= 0) return;

        // Initialize when opened the first time
        if (!_initialized)
        {
            Generate(root);
            _initialized = true;
        }

        LoadMetaData();
        root.Add(_popup);
    }

    public void Close(VisualElement root)
    {
        // TODO Create close button?
        
        SaveMetaData();
        root.Remove(_popup);
    }

    private void LoadMetaData()
    {
        // Read data
        _data = FunscriptRenderer.Singleton.Haptics[0].Funscript.metadata;

        // Set data
        _creatorField.value = _data.creator;
        _descriptionField.value = _data.description;
        _durationField.value = _data.duration;
        _licenseField.value = _data.license;
        _notesField.value = _data.notes;
        _performersField.value = string.Join(",", _data.performers);
        _scriptUrlField.value = _data.script_url;
        _tagsField.value = string.Join(",", _data.tags);
        _titleField.value = _data.title;
        _typeField.value = _data.type;
        _videoUrlField.value = _data.video_url;
        _rangeField.value = _data.range;
        _versionField.value = _data.version;
    }

    private void SaveMetaData()
    {
        var haptics = FunscriptRenderer.Singleton.Haptics[0];
        haptics.Funscript.metadata = _data;
        FunscriptRenderer.Singleton.Haptics[0] = haptics;
    }
}