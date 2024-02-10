using UnityEngine;
using UnityEngine.UIElements;

public class UIBehaviour : MonoBehaviour
{
    protected VisualElement Create(params string[] classNames)
    {
        return Create<VisualElement>(classNames);
    }

    protected T Create<T>(params string[] classNames) where T : VisualElement, new()
    {
        var element = new T();
        foreach (var className in classNames)
        {
            element.AddToClassList(className);
        }

        return element;
    }
}