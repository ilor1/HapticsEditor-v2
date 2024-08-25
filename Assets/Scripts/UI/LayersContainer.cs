using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class LayersContainer : UIBehaviour
{
    public static LayersContainer Singleton;

    public List<VisualElement> Layers = new();

    private VisualElement _title;
    private VisualElement _layersContainer;
    private VisualElement _internalLayersContainer;
    private HashSet<VisualElement> _visibleLayers = new();
    private List<VisualElement> _selectedLayers = new();
    private int _layerRunningNumber = 1;

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
        _layersContainer = root.Query("layers-container");

        // limit to three layers for now, just to not have to deal with scrollbars...?

        // Layers Title
        // show/hide, layer, toy1, toy2, toy3, toy4
        _title = CreateTitle();
        _layersContainer.Add(_title);

        _internalLayersContainer = Create<ScrollView>("items");
        _layersContainer.Add(_internalLayersContainer);

        // Layers
        // eye, layer1, (o), (o), (o), (o)
        // click to select/deselect
        // selected layers are highlighted
        // modifications always affect all selected layers, even if the layer is hidden!

        // Layers bottom
        // Add 'AddLayer' button
        // Add 'RemoveLayer' button, don't allow removing the last existing layer
        _layersContainer.Add(CreateBottomBar());
    }


    private VisualElement CreateTitle()
    {
        var layersTitle = Create("title", "border-bottom", "row");

        // show/hide
        var eye = Create("layers-eye", "open");
        eye.RegisterCallback<ClickEvent>(OnTitleEyeClick);
        layersTitle.Add(eye);

        // name
        var layerName = Create<Label>();
        layerName.text = "Name";
        layersTitle.Add(layerName);

        return layersTitle;
    }

    public void CreateHapticsLayer(Haptics haptics)
    {
        _internalLayersContainer.Add(CreateLayer(haptics));
    }

    private VisualElement CreateLayer(Haptics haptics = null)
    {
        var layer = Create("item", "border-bottom", "row", "background--light");
        Layers.Add(layer);
        _visibleLayers.Add(layer);
        _selectedLayers.Add(layer);

        // show/hide
        var eye = Create("layers-eye", "open");
        eye.RegisterCallback<ClickEvent>(OnLayerEyeClick);
        layer.Add(eye);

        // color
        var layerColor = Create("layers-color");
        layerColor.style.backgroundColor = haptics.LineRenderSettings.StrokeColor;
        layer.Add(layerColor);

        // label
        var label = Create<Label>();
        label.text = haptics == null ? $"Layer{_layerRunningNumber++}" : haptics.Name;
        label.pickingMode = PickingMode.Ignore;
        layer.Add(label);

        layer.RegisterCallback<ClickEvent>(OnLayerClick);
        return layer;
    }

    private VisualElement CreateBottomBar()
    {
        var bottomBar = Create("row", "bottom-bar", "border-top", "background--medium");

        // create layer
        var createButton = Create<Button>();
        createButton.text = "+";
        createButton.clicked += OnAddLayerClicked;
        bottomBar.Add(createButton);

        // remove layer
        var removeButton = Create<Button>();
        removeButton.text = "-";
        removeButton.clicked += OnRemoveLayerClicked;
        bottomBar.Add(removeButton);

        return bottomBar;
    }

    private void OnAddLayerClicked()
    {
        int hapticsCount = FunscriptRenderer.Singleton.Haptics.Count;
        string pathWithoutExtension = FileDropdownMenu.Singleton.FunscriptPathWithoutExtension;

        string path = string.IsNullOrEmpty(pathWithoutExtension)
            ? $"{Application.streamingAssetsPath}/new funscript.funscript"
            : $"{pathWithoutExtension}[{hapticsCount}].funscript";

        // Create new haptics file
        var haptics = FunscriptSaver.Singleton.CreateNewHaptics(path);
        FunscriptRenderer.Singleton.Haptics.Add(haptics);

        _internalLayersContainer.Add(CreateLayer(haptics));

        UndoRedo.Instance.ResetUndoStack();
    }

    private void OnRemoveLayerClicked()
    {
        for (int i = _selectedLayers.Count - 1; i >= 0; i--)
        {
            // Also remove the haptic when the layer gets removed
            for (int j = Layers.Count - 1; j >= 0; j--)
            {
                if (_selectedLayers[i] != Layers[j]) continue;

                FunscriptRenderer.Singleton.Haptics.RemoveAt(j);
                VisualElement layer = _selectedLayers[i];
                _selectedLayers.Remove(layer);
                _visibleLayers.Remove(layer);
                _internalLayersContainer.Remove(layer);
                Layers.RemoveAt(j);
                break;
            }
        }

        // Update OverallHaptics
        FunscriptOverview.Singleton.RenderHaptics();
        
        UndoRedo.Instance.ResetUndoStack();
    }


    private void OnTitleEyeClick(ClickEvent evt)
    {
        evt.StopPropagation();

        VisualElement clickedEye = evt.target as VisualElement;

        if (clickedEye.GetClasses().Contains("open"))
        {
            // Hide all layers
            foreach (VisualElement layer in Layers)
            {
                _visibleLayers.Remove(layer);

                // if a layer gets hidden, lets also deselect it to avoid unwanted edits.
                _selectedLayers.Remove(layer);
                layer.RemoveFromClassList("background--light");
                layer.AddToClassList("background--dark");

                foreach (VisualElement child in layer.Children())
                {
                    if (child.ClassListContains("layers-eye"))
                    {
                        child.RemoveFromClassList("open");
                        child.AddToClassList("closed");
                    }
                }
            }

            // eye
            clickedEye.RemoveFromClassList("open");
            clickedEye.AddToClassList("closed");
        }
        else
        {
            // Show all layers
            foreach (VisualElement layer in Layers)
            {
                _visibleLayers.Add(layer);
                foreach (VisualElement child in layer.Children())
                {
                    if (child.ClassListContains("layers-eye"))
                    {
                        child.AddToClassList("open");
                        child.RemoveFromClassList("closed");
                    }
                }
            }

            // eye
            clickedEye.AddToClassList("open");
            clickedEye.RemoveFromClassList("closed");
        }

        SetHapticVisibilities();
        SetHapticSelections();

        // Update OverallHaptics
        FunscriptOverview.Singleton.RenderHaptics();
    }

    private void SetHapticVisibilities()
    {
        for (int i = 0; i < Layers.Count; i++)
        {
            bool visible = _visibleLayers.Contains(Layers[i]);
            FunscriptRenderer.Singleton.Haptics[i].Visible = visible;
        }
    }

    private void SetHapticSelections()
    {
        for (int i = 0; i < Layers.Count; i++)
        {
            bool selected = _selectedLayers.Contains(Layers[i]);
            FunscriptRenderer.Singleton.Haptics[i].Selected = selected;
        }
    }

    private void OnLayerEyeClick(ClickEvent evt)
    {
        evt.StopPropagation();

        // Get the clicked VisualElement
        VisualElement clickedEye = evt.target as VisualElement;

        // select/deselect layer
        if (clickedEye != null)
        {
            // get layer
            VisualElement clickedLayer = clickedEye.parent;

            if (_visibleLayers.Contains(clickedLayer))
            {
                // layer
                _visibleLayers.Remove(clickedLayer);

                // if a layer gets hidden, lets also deselect it to avoid unwanted edits.
                _selectedLayers.Remove(clickedLayer);
                clickedLayer.RemoveFromClassList("background--light");
                clickedLayer.AddToClassList("background--dark");

                // eye
                clickedEye.RemoveFromClassList("open");
                clickedEye.AddToClassList("closed");
            }
            else
            {
                // layer
                _visibleLayers.Add(clickedLayer);

                // eye
                clickedEye.AddToClassList("open");
                clickedEye.RemoveFromClassList("closed");
            }
        }

        SetHapticVisibilities();
        SetHapticSelections();

        // Update OverallHaptics
        FunscriptOverview.Singleton.RenderHaptics();
    }

    private void OnLayerClick(ClickEvent evt)
    {
        evt.StopPropagation();

        // Get the clicked VisualElement
        VisualElement clickedLayer = evt.target as VisualElement;

        // select/deselect layer
        if (clickedLayer != null)
        {
            if (_selectedLayers.Contains(clickedLayer))
            {
                // deselect
                _selectedLayers.Remove(clickedLayer);
                clickedLayer.RemoveFromClassList("background--light");
                clickedLayer.AddToClassList("background--dark");
            }
            else
            {
                // select
                _selectedLayers.Add(clickedLayer);
                clickedLayer.AddToClassList("background--light");
                clickedLayer.RemoveFromClassList("background--dark");

                // force layer visible
                _visibleLayers.Add(clickedLayer);
                foreach (var child in clickedLayer.Children())
                {
                    if (child.ClassListContains("layers-eye"))
                    {
                        child.AddToClassList("open");
                        child.RemoveFromClassList("closed");
                    }
                }
            }
        }

        SetHapticVisibilities();
        SetHapticSelections();

        // Update OverallHaptics
        FunscriptOverview.Singleton.RenderHaptics();
    }
}