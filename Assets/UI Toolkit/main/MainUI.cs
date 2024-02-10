using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUI : UIBehaviour
{
    [Header("UI Panel")]
    [SerializeField]
    protected UIDocument _document;

    [SerializeField]
    protected StyleSheet _styleSheet;


    public Action<VisualElement> RootCreated;

    private void Start()
    {
        StartCoroutine(Generate());
    }

    private IEnumerator Generate()
    {
        yield return null; // fix race condition

        // Create Root
        var root = _document.rootVisualElement;
        root.Clear();
        root.styleSheets.Add(_styleSheet);
        root.AddToClassList("root");

        // Send event
        RootCreated?.Invoke(root);
    }
}