using UnityEngine;
using UnityEngine.UIElements;

public class LayersContainer : UIBehaviour
{
    public static LayersContainer Singleton;

    private VisualElement _layersContainer;

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
        _layersContainer = root.Query(className: "layers-container");

        // limit to three layers for now, just to not have to deal with scrollbars...?

        // Layers Title
        // show/hide, layer, toy1, toy2, toy3, toy4
        var layersTitle = Create("layers-title");
        _layersContainer.Add(layersTitle);

        // Layers
        // eye, layer1, (o), (o), (o), (o)
        // click to select
        // ctrl-click to select multiple
        // selected layers are highlighted
        // modifications always affect all selected layers, even if the layer is hidden!
        var layer1 = Create("layers-item");
        var layer2 = Create("layers-item");
        var layer3 = Create("layers-item");
        _layersContainer.Add(layer1);
        _layersContainer.Add(layer2);
        _layersContainer.Add(layer3);

        // Layers bottom
        // Add 'AddLayer' button
        // Add 'RemoveLayer' button, don't allow removing the last existing layer
        var bottom = Create("layers-bottom");
        _layersContainer.Add(bottom);
    }
}