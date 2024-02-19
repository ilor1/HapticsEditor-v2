using System;
using System.Collections.Generic;
using Buttplug.Client;
using Buttplug.Client.Connectors.WebsocketConnector;
using Unity.Mathematics;
using UnityEngine;

public class IntifaceManager : MonoBehaviour
{
    public static IntifaceManager Singleton;

    [SerializeField, Range(0, 1)] private float _intensity = 0.5f;

    private ButtplugClient _client;
    private List<ButtplugClientDevice> _devices { get; } = new List<ButtplugClientDevice>();

    private float _timeSinceLastUpdate = 0f;
    private const float _updateInterval = 0.33f;

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
            // Only play haptics when playing
            if (TimelineManager.Instance.IsPlaying)
            {
                _intensity = GetHapticValue();
                UpdateDevices();
            }
            else
            {
                StopDevices();
                _timeSinceLastUpdate = 0;
            }
        }
    }

    private float GetHapticValue()
    {
        // No funscript
        if (FunscriptRenderer.Singleton.Haptics.Count <= 0) return 0f;

        int at = TimelineManager.Instance.TimeInMilliseconds;
        var actions = FunscriptRenderer.Singleton.Haptics[0].Funscript.actions;

        // exit early cases
        if (actions.Count <= 0) return 0f; // no funactions
        if (actions.Count == 1) return actions[0].pos * 0.01f; // only one funaction
        if (actions[^1].at < at) return actions[^1].pos * 0.01f; // last action is before current at

        // find the range where "at" is 
        for (int i = 0; i < actions.Count - 1; i++)
        {
            if (actions[i].at >= at)
            {
                return actions[i].pos * 0.01f;
            }

            if (actions[i + 1].at > at)
            {
                float t = (at - actions[i].at) / (float)(actions[i + 1].at - actions[i].at);
                return math.lerp(actions[i].pos, actions[i + 1].pos, t) * 0.01f;
            }
        }

        // failed somehow
        return 0f;
    }

    private void OnValidate()
    {
        UpdateDevices();
    }

    private void UpdateDevices()
    {
        foreach (ButtplugClientDevice device in _devices)
        {
            // TODO: linear
            if (device.OscillateAttributes.Count > 0) device.OscillateAsync(_intensity);
            if (device.RotateAttributes.Count > 0) device.RotateAsync(_intensity, true);
            if (device.VibrateAttributes.Count > 0) device.VibrateAsync(_intensity);
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
        UpdateDevices();
    }

    private void RemoveDevice(object sender, DeviceRemovedEventArgs e)
    {
        Log($"Device {e.Device.Name} Removed!");
        _devices.Remove(e.Device);
        UpdateDevices();
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