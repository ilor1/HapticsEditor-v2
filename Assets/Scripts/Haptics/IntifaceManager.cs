using System;
using System.Collections.Generic;
using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using Buttplug.Core.Messages;
using Unity.Mathematics;
using UnityEngine;


public class IntifaceManager : MonoBehaviour
{
    public static IntifaceManager Singleton;

    private ButtplugClient _client;
    private List<ButtplugClientDevice> _devices { get; } = new();

    private float _timeSinceLastUpdate = 0f;
    private const float _updateInterval = 0f; //0.33f;

    public Dictionary<ButtplugClientDevice, List<GenericDeviceMessageAttributes>> DeviceFeatures = new();
    public Dictionary<GenericDeviceMessageAttributes, double> PositionTargets = new();

    public bool Inverted { get; set; } // This inverted is on top of the "Inverted" value inside the funscript. So you can Invert while you Invert.

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    public async void OnEnable()
    {
        _client = new ButtplugClient("Haptics Editor");
        Log("Trying to create client");

        // Set up client event handlers before we connect.
        _client.DeviceAdded += AddDevice;
        _client.DeviceRemoved += RemoveDevice;
        _client.ScanningFinished += ScanFinished;
        // Creating a Websocket Connector is as easy as using the right
        // options object.
        var connector = new ButtplugWebsocketConnector(new Uri("ws://localhost:12345/buttplug"));
        await _client.ConnectAsync(connector);

        await _client.StartScanningAsync();
    }

    public async void OnDisable()
    {
        _devices.Clear();
        DeviceFeatures.Clear();

        // On object shutdown, disconnect the client and just kill the server
        // process. Server process shutdown will be cleaner in future builds.
        if (_client != null)
        {
            _client.DeviceAdded -= AddDevice;
            _client.DeviceRemoved -= RemoveDevice;
            _client.ScanningFinished -= ScanFinished;
            await _client.DisconnectAsync();
            _client.Dispose();
            _client = null;
        }

        Log("I am destroyed now");
    }

    private void Update()
    {
        _timeSinceLastUpdate += Time.deltaTime;

        // Update at certain intervals so Intiface can keep up
        if (_timeSinceLastUpdate > _updateInterval)
        {
            // Not playing
            if (!TimelineManager.Instance.IsPlaying)
            {
                StopDevices();
                _timeSinceLastUpdate = 0;
                return;
            }

            var haptics = FunscriptRenderer.Singleton.Haptics;
            for (int i = 0; i < haptics.Count; i++)
            {
                // don't play haptics on hidden layers
                if (!haptics[i].Visible) continue;

                float value = GetHapticValue(haptics[i]);
                GetDurationAndPosition(haptics[i], out uint duration, out double position);

                // this Inverted is the Inverted Toggle not to be confused with the Funscript inverted
                float intensity = Inverted ? 1 - value : value;

                // Go through each feat and check if it should play for this layer
                foreach (var kvp in DeviceFeatures)
                {
                    for (int j = 0; j < kvp.Value.Count; j++)
                    {
                        int hapticLayer = DeviceContainer.Singleton.DeviceLayers[kvp.Value[j]];

                        // this attribute should play on this layer
                        if (hapticLayer == i)
                        {
                            UpdateDevice(kvp.Key, j, intensity, duration, position);
                        }
                    }
                }
            }
        }
    }

    private void GetDurationAndPosition(Haptics haptics, out uint duration, out double position)
    {
        duration = 0; // duration 0 => do nothing
        position = 0;

        // No funscript
        if (FunscriptRenderer.Singleton.Haptics.Count <= 0) return;

        int at = TimelineManager.Instance.TimeInMilliseconds;


        var actions = haptics.Funscript.actions;
        bool inverted = haptics.Funscript.inverted;

        if (actions.Count <= 0) return; // no funactions
        if (actions.Count == 1)
        {
            // only one funaction
            duration = (uint)math.max(0, actions[0].at - at);
            position = inverted ? 1f - actions[0].pos * 0.01 : actions[0].pos * 0.01;
            return;
        }

        if (actions[^1].at < at) return; // last action is before current at

        // set last point as target
        if (actions[^2].at <= at && actions[^1].at > at)
        {
            position = inverted ? 1f - actions[^1].pos * 0.01 : actions[^1].pos * 0.01;
            duration = (uint)math.max(0, actions[^1].at - at);
            return;
        }

        // set first point as target
        if (actions[0].at > at)
        {
            position = inverted ? 1f - actions[0].pos * 0.01 : actions[0].pos * 0.01;
            duration = (uint)math.max(0, actions[0].at - at);
            return;
        }

        // other
        for (int i = 0; i < actions.Count - 1; i++)
        {
            if (at >= actions[i].at && at < actions[i + 1].at)
            {
                position = inverted ? 1f - actions[i + 1].pos * 0.01 : actions[i + 1].pos * 0.01;
                duration = (uint)math.max(0, actions[i + 1].at - at);
            }
        }
    }

    private float GetHapticValue(Haptics haptics)
    {
        // No funscript
        if (FunscriptRenderer.Singleton.Haptics.Count <= 0) return 0f;

        int at = TimelineManager.Instance.TimeInMilliseconds;
        var actions = haptics.Funscript.actions;

        bool inverted = haptics.Funscript.inverted;

        // exit early cases
        if (actions.Count <= 0) return 0f; // no funactions
        if (actions.Count == 1) return actions[0].pos * 0.01f; // only one funaction
        if (actions[^1].at < at) return actions[^1].pos * 0.01f; // last action is before current at

        // find the range where "at" is 
        for (int i = 0; i < actions.Count - 1; i++)
        {
            if (actions[i].at >= at)
            {
                float value = actions[i].pos * 0.01f;
                return inverted ? 1f - value : value;
            }

            if (actions[i + 1].at > at)
            {
                float t = (at - actions[i].at) / (float)(actions[i + 1].at - actions[i].at);
                float value = math.lerp(actions[i].pos, actions[i + 1].pos, t) * 0.01f;
                return inverted ? 1f - value : value;
            }
        }

        // failed somehow
        return 0f;
    }

    private void UpdateDevice(ButtplugClientDevice device, int index, float intensity, uint duration, double position)
    {
        // Go through the attributes in the order they were stored in the Dictionary

        int attributeIndex = 0;
        for (int i = 0; i < device.LinearAttributes.Count; i++)
        {
            // found correct command
            if (attributeIndex == index && duration > 0)
            {
                // // store position targets..
                if (PositionTargets.TryGetValue(device.LinearAttributes[i], out double currentPosition))
                {
                    // position target is the same...
                    if (math.abs(currentPosition - position) < 0.01)
                    {
                        PositionTargets[device.LinearAttributes[i]] = position;
                    }
                    else
                    {
                        device.LinearAsync(duration, position);

                        // update position target
                        PositionTargets[device.LinearAttributes[i]] = position;
                    }
                }
                else
                {
                    device.LinearAsync(duration, position);

                    // store new position target
                    PositionTargets.Add(device.LinearAttributes[i], position);
                }

                return;
            }

            attributeIndex++;
        }

        for (int i = 0; i < device.OscillateAttributes.Count; i++)
        {
            // found correct command
            if (attributeIndex == index)
            {
                device.OscillateAsync(intensity);
                return;
            }

            attributeIndex++;
        }

        for (int i = 0; i < device.RotateAttributes.Count; i++)
        {
            // found correct command
            if (attributeIndex == index)
            {
                device.RotateAsync(intensity, true);
                return;
            }

            attributeIndex++;
        }

        for (int i = 0; i < device.VibrateAttributes.Count; i++)
        {
            // found correct command
            if (attributeIndex == index)
            {
                device.VibrateAsync(intensity);
                return;
            }

            attributeIndex++;
        }
    }

    private void StopDevices()
    {
        foreach (ButtplugClientDevice device in _devices)
        {
            device.Stop();
        }
    }

    private void AddDevice(object sender, DeviceAddedEventArgs e)
    {
        Log($"Device {e.Device.Name} Connected!");
        _devices.Add(e.Device);

        // Get GenericDeviceMessageAttributes
        var features = new List<GenericDeviceMessageAttributes>();
        features.AddRange(e.Device.LinearAttributes);
        features.AddRange(e.Device.OscillateAttributes);
        features.AddRange(e.Device.RotateAttributes);
        features.AddRange(e.Device.VibrateAttributes);
        DeviceFeatures.Add(e.Device, features);

        // UpdateDevices();
    }

    private void RemoveDevice(object sender, DeviceRemovedEventArgs e)
    {
        Log($"Device {e.Device.Name} Removed!");

        DeviceFeatures.Remove(e.Device);
        _devices.Remove(e.Device);

        // UpdateDevices();
    }

    private void ScanFinished(object sender, EventArgs e)
    {
        Log("Device scanning is finished!");
    }

    private void Log(object text)
    {
        Debug.Log("<color=red>Buttplug:</color> " + text, this);
    }
}