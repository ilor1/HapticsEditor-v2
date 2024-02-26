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
    
    protected TextField CreateInputTextField(string title, VisualElement parent, string className)
    {
        var container = Create(className);
        var label = Create<Label>();
        label.text = title;

        var inputField = Create<TextField>();
        container.Add(label);
        container.Add(inputField);

        parent.Add(container);

        return inputField;
    }

    protected IntegerField CreateInputIntegerField(string title, VisualElement parent, string className)
    {
        var container = Create(className);
        var label = Create<Label>();
        label.text = title;

        var inputField = Create<IntegerField>();
        container.Add(label);
        container.Add(inputField);

        parent.Add(container);

        return inputField;
    }

    protected Toggle CreateInputToggleField(string title, VisualElement parent, string className)
    {
        var container = Create(className);
        var label = Create<Label>();
        label.text = title;

        var inputField = Create<Toggle>();
        container.Add(label);
        container.Add(inputField);

        parent.Add(container);

        return inputField;
    }

}