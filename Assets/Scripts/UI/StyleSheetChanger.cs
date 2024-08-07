using UnityEngine;
using UnityEngine.UIElements;

public class StyleSheetChanger : MonoBehaviour
{
    [SerializeField] private StyleSheet[] _stylesheets;
    
    private UIDocument _document;
    private MainUI _mainUI;
    private int _currentStylesheet;

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        _mainUI = GetComponent<MainUI>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            _currentStylesheet++;
            if (_currentStylesheet >= _stylesheets.Length) _currentStylesheet = 0;

            // refresh stylesheet
            var root = _document.rootVisualElement;
            root.styleSheets.Clear();
            root.styleSheets.Add(_stylesheets[_currentStylesheet]);
            _mainUI.StyleSheet = _stylesheets[_currentStylesheet];
        }
    }
}