using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class UIBehaviour : MonoBehaviour
{
    [Header("UI Panel")]
    [SerializeField]
    protected UIDocument _document;

    [SerializeField]
    protected StyleSheet _styleSheet;
    
    protected virtual IEnumerator Generate()
    {
        yield return null; // fix race condition
        
        // Create Root
        var root = _document.rootVisualElement;
        root.Clear();
        root.styleSheets.Add(_styleSheet);
        root.AddToClassList("root");
    }
    
    protected VisualElement Create(params string[] classNames)
    {
        return Create<VisualElement>(classNames);
    }

    private T Create<T>(params string[] classNames) where T : VisualElement, new()
    {
        var element = new T();
        foreach (var className in classNames)
        {
            element.AddToClassList(className);
        }

        return element;
    }
}