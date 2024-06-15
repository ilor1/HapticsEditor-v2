using System.Collections.Generic;
using Buttplug.Core.Messages;
using UnityEngine.UIElements;

public class DeviceContainer : UIBehaviour
{
    public static DeviceContainer Singleton;

    public Dictionary<GenericDeviceMessageAttributes, int> DeviceLayers = new();
    
    private VisualElement _devicesContainer;
    private int _previousFrameDeviceCount;
    private int _previousLayerCount;
    
    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
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
        _devicesContainer = root.Query(className: "devices-container");
    }

    private void FixedUpdate()
    {
        // Update if layers or devices change
        if (IntifaceManager.Singleton.DeviceFeatures.Count != _previousFrameDeviceCount
            || _previousLayerCount != LayersContainer.Singleton.Layers.Count)
        {
            _previousLayerCount = LayersContainer.Singleton.Layers.Count;
            _previousFrameDeviceCount = IntifaceManager.Singleton.DeviceFeatures.Count;
            UpdateDeviceRadioButtons();
        }
    }


    private void UpdateDeviceRadioButtons()
    {
        // Clear existing
        while (_devicesContainer.childCount > 0)
        {
            _devicesContainer.RemoveAt(0);
        }

        var title = Create("devices-title");
        _devicesContainer.Add(title);

        int layerCount = LayersContainer.Singleton.Layers.Count;
        for (int i = 0; i < layerCount; i++)
        {
            var layerLabel = Create<Label>();
            layerLabel.text = $"L{i}";
            title.Add(layerLabel);
        }

        foreach (var kvp in IntifaceManager.Singleton.DeviceFeatures)
        {
            foreach (var feat in kvp.Value)
            {
                var deviceItem = Create("device-item");

                var deviceName = Create<Label>("device-name");
                deviceName.text = $"{kvp.Key.Name}[{feat.Index}]";

                int activeRadioButton = 0;
                if (DeviceLayers.ContainsKey(feat))
                {
                    activeRadioButton = DeviceLayers[feat];
                }
                else
                {
                    DeviceLayers.Add(feat, activeRadioButton);
                }

                var radioButtonGroup = Create<RadioButtonGroup>("device-radiobuttons");
                for (int i = 0; i < layerCount; i++)
                {
                    var radioButton = Create<RadioButton>("device-radio-button");

                    if (i == activeRadioButton)
                    {
                        radioButton.value = true;
                    }

                    radioButtonGroup.Add(radioButton);
                }

                radioButtonGroup.value = activeRadioButton < layerCount ? activeRadioButton : layerCount - 1;
                radioButtonGroup.RegisterValueChangedCallback(evt => { DeviceLayers[feat] = evt.newValue; });

                deviceItem.Add(deviceName);
                deviceItem.Add(radioButtonGroup);
                _devicesContainer.Add(deviceItem);
            }
        }
    }
}