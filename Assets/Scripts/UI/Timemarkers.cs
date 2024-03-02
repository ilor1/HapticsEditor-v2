using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class Timemarkers : UIBehaviour
{
    private VisualElement _container;

    private VisualElement[] _verticalMarkers;
    private Label[] _labels;
    private bool _initialized = false;

    private VisualElement _startMarker;
    private int _startAt = -1;
    private VisualElement _endMarker;
    private int _endAt = -1;

    private bool _dragStartMarker = false;
    private bool _dragEndMarker = false;

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
        _container = root.Query(className: "timemarkers");

        _startMarker = Create("marker-start");
        _startMarker.style.left = 0;
        _container.Add(_startMarker);

        _endMarker = Create("marker-end");
        _endMarker.style.right = 0;
        _container.Add(_endMarker);

        _labels = new Label[11];
        _verticalMarkers = new VisualElement[11];
        for (int i = 0; i < _verticalMarkers.Length; i++)
        {
            _verticalMarkers[i] = Create("timemarker");
            var line = Create("timemarker-line");
            line.pickingMode = PickingMode.Ignore;
            _verticalMarkers[i].Add(line);


            _labels[i] = Create<Label>();
            _labels[i].pickingMode = PickingMode.Ignore;
            _verticalMarkers[i].Add(_labels[i]);

            _verticalMarkers[i].pickingMode = PickingMode.Ignore;
            _container.Add(_verticalMarkers[i]);
        }

        _container.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _container.RegisterCallback<PointerUpEvent>(OnPointerUp);
        _container.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _container.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

        _initialized = true;
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        _dragStartMarker = false;
        _dragEndMarker = false;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_dragEndMarker && !_dragStartMarker) return;

        int time0 = (int)math.round(TimelineManager.Instance.TimeInMilliseconds - TimelineManager.Instance.LengthInMilliseconds * 0.5f);
        int at = (int)math.round(evt.localPosition.x * TimelineManager.Instance.LengthInMilliseconds / _container.contentRect.width) + time0;

        if (_dragStartMarker)
        {
            _startAt = at;
        }
        else if (_dragEndMarker)
        {
            _endAt = at;
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        _dragStartMarker = false;
        _dragEndMarker = false;
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        int time0 = (int)math.round(TimelineManager.Instance.TimeInMilliseconds - TimelineManager.Instance.LengthInMilliseconds * 0.5f);
        int at = (int)math.round(evt.localPosition.x * TimelineManager.Instance.LengthInMilliseconds / _container.contentRect.width) + time0;

        if (evt.button == 0 && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            // left click + shift, place right marker
            _dragEndMarker = true;
            _endAt = at;
        }
        else if (evt.button == 0)
        {
            // left click, place left marker
            _dragStartMarker = true;
            _startAt = at;
        }

        if (evt.button == 2 && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            // right click + shift, offset using end marker
            int amountToMove = at - _endAt;

            // move
            bool copy = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            FunscriptCutPaste.Singleton.StartTimeInMilliseconds = _startAt;
            FunscriptCutPaste.Singleton.EndTimeInMilliseconds = _endAt;
            FunscriptCutPaste.Singleton.AmountToMoveInMilliseconds = amountToMove;
            FunscriptCutPaste.Singleton.Move(copy);

            FunscriptRenderer.Singleton.SortFunscript();
            FunscriptRenderer.Singleton.CleanupExcessPoints();
            TitleBar.MarkLabelDirty();

            _startAt += amountToMove;
            _endAt += amountToMove;
        }
        else if (evt.button == 2)
        {
            // right click, offset using start marker
            int amountToMove = at - _startAt;

            // move
            bool copy = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            FunscriptCutPaste.Singleton.StartTimeInMilliseconds = _startAt;
            FunscriptCutPaste.Singleton.EndTimeInMilliseconds = _endAt;
            FunscriptCutPaste.Singleton.AmountToMoveInMilliseconds = amountToMove;
            FunscriptCutPaste.Singleton.Move(copy);

            FunscriptRenderer.Singleton.SortFunscript();
            FunscriptRenderer.Singleton.CleanupExcessPoints();
            TitleBar.MarkLabelDirty();

            _startAt += amountToMove;
            _endAt += amountToMove;
        }
    }

    private void DragStartMarker()
    {
    }

    private void DragEndMarker()
    {
    }

    private void Update()
    {
        if (!_initialized) return;

        int time0 = (int)math.round(TimelineManager.Instance.TimeInMilliseconds - TimelineManager.Instance.LengthInMilliseconds * 0.5f);
        int time1 = time0 + TimelineManager.Instance.LengthInMilliseconds;

        // Show/Hide start marker
        if (_startAt < 0 || time0 > _startAt || time1 < _startAt)
        {
            _startMarker.style.display = DisplayStyle.None;
        }
        else
        {
            _startMarker.style.display = DisplayStyle.Flex;
            float x = (_startAt - time0) / (float)(TimelineManager.Instance.LengthInMilliseconds);
            x *= _container.contentRect.width;
            _startMarker.style.left = x;
        }

        // Show/Hide end marker
        if (_endAt < 0 || time0 > _endAt || time1 < _endAt)
        {
            _endMarker.style.display = DisplayStyle.None;
        }
        else
        {
            _endMarker.style.display = DisplayStyle.Flex;
            float x = (_endAt - time0) / (float)(TimelineManager.Instance.LengthInMilliseconds);
            x *= _container.contentRect.width;
            x -= _endMarker.contentRect.width;
            _endMarker.style.left = x;
        }


        int spacingInMilliseconds = (int)math.round(TimelineManager.Instance.LengthInMilliseconds / 10f);
        int offsetInMilliseconds = time0 % spacingInMilliseconds;

        for (int i = 0; i < _verticalMarkers.Length; i++)
        {
            _verticalMarkers[i].style.display = DisplayStyle.None;
        }

        float ratio = _container.contentRect.width / TimelineManager.Instance.LengthInMilliseconds;
        float offset = offsetInMilliseconds * ratio;
        float increment = spacingInMilliseconds * ratio;

        int index = 0;
        for (float x = offset; x < _container.contentRect.width; x += increment)
        {
            _verticalMarkers[index].style.right = x;
            _verticalMarkers[index].style.display = DisplayStyle.Flex;

            int time = ((int)math.round(time0 / spacingInMilliseconds) - index) * spacingInMilliseconds;
            time += TimelineManager.Instance.LengthInMilliseconds;
            _labels[index].text = $"{time}";

            index++;
        }
    }
}