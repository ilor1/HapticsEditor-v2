using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.InputSystem;

public class CustomDropDownMenu : VisualElement
{
    private VisualElement _dropdownButton;
    
    public void Append(string text, System.Action action)
    {
        var button = new Button();
        button.focusable = false;
        button.AddToClassList("menu-button");
        button.text = text;
        button.clicked += action;
        Add(button);
    }

    public void Toggle(VisualElement button)
    {
        if (style.display == DisplayStyle.Flex)
        {
            _dropdownButton.SetEnabled(true);
            focusable = false;
            UnregisterCallback<FocusOutEvent>(OnFocusOut);
            style.display = DisplayStyle.None;
        }
        else
        {
            _dropdownButton = button;
            _dropdownButton.SetEnabled(false);
            focusable = true;
            RegisterCallback<FocusOutEvent>(OnFocusOut);
            style.display = DisplayStyle.Flex;
            BringToFront();
            Focus();
        }
    }

    private void OnFocusOut(FocusOutEvent evt)
    {
        Toggle(_dropdownButton);
    }
}