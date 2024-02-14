using UnityEngine;
using UnityEngine.UIElements;

public static class SettingsDropdownMenu
{
    public static void OnEditBindingsClick()
    {
        BindingsMenu.Singleton.Open();
    }
}