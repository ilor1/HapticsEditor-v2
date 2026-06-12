using UnityEngine;
using UnityEngine.UIElements;

public class CursorInfo : UIBehaviour
{
    public static CursorInfo Singleton;

    private VisualElement _funscriptHapticContainer;
    private VisualElement _lineCursorVertical;
    private VisualElement _lineCursorHorizontal;
    private Label _prevLabel;
    private Label _atLabel;
    private Label _posLabel;

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
        _lineCursorVertical = Create("line-cursor-vertical");
        _lineCursorVertical.focusable = false;
        _lineCursorVertical.pickingMode = PickingMode.Ignore;

        _lineCursorHorizontal = Create("line-cursor-horizontal");
        _lineCursorHorizontal.focusable = false;
        _lineCursorHorizontal.pickingMode = PickingMode.Ignore;

        _atLabel = Create<Label>("line-cursor-label");
        _atLabel.focusable = false;
        _atLabel.pickingMode = PickingMode.Ignore;

        _posLabel = Create<Label>("line-cursor-label");
        _posLabel.focusable = false;
        _posLabel.pickingMode = PickingMode.Ignore;

        _prevLabel = Create<Label>("line-cursor-label");
        _prevLabel.focusable = false;
        _prevLabel.pickingMode = PickingMode.Ignore;

        root.Add(_lineCursorVertical);
        root.Add(_lineCursorHorizontal);
        root.Add(_atLabel);
        root.Add(_posLabel);
        root.Add(_prevLabel);

        _funscriptHapticContainer = root.Query("funscript-container-right");
        _funscriptHapticContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _funscriptHapticContainer.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        _funscriptHapticContainer.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        Show();
    }

    private void Show()
    {
        _lineCursorVertical.style.display = DisplayStyle.Flex;
        _lineCursorHorizontal.style.display = DisplayStyle.Flex;
        _atLabel.style.display = DisplayStyle.Flex;
        _posLabel.style.display = DisplayStyle.Flex;
        _prevLabel.style.display = DisplayStyle.Flex;
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        Hide();
    }

    private void Hide()
    {
        _lineCursorVertical.style.display = DisplayStyle.None;
        _lineCursorHorizontal.style.display = DisplayStyle.None;
        _atLabel.style.display = DisplayStyle.None;
        _posLabel.style.display = DisplayStyle.None;
        _prevLabel.style.display = DisplayStyle.None;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        UpdateCursor(evt.position);
    }

    public void UpdateCursor(Vector2 panelPosition)
    {
        if (_funscriptHapticContainer == null) return;

        Show();

        // cross
        _lineCursorVertical.style.top = _funscriptHapticContainer.contentRect.y;
        _lineCursorVertical.style.left = panelPosition.x;
        _lineCursorHorizontal.style.top = panelPosition.y;
        _lineCursorHorizontal.style.left = 0;

        // at-label
        _atLabel.style.top = panelPosition.y - 3;
        _atLabel.text = $"at:{FunscriptMouseInput.MouseAt}";

        // pos-label
        _posLabel.style.top = panelPosition.y - 35;
        _posLabel.text = $"pos:{FunscriptMouseInput.MousePos}";

        // prev-label
        _prevLabel.style.top = panelPosition.y + 20;

        foreach (var haptics in FunscriptRenderer.Singleton.Haptics)
        {
            if (haptics.Selected)
            {
                _prevLabel.text = $"prev:{FunscriptMouseInput.GetPreviousAtValue(haptics) - FunscriptMouseInput.MouseAt}";
                break;
            }
        }
    }
}
