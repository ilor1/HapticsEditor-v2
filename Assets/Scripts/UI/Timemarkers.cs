using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class Timemarkers : UIBehaviour
{
    public static Timemarkers Singleton;

    private VisualElement _container;
    private VisualElement[] _verticalMarkers;
    private Label[] _labels;
    private bool _initialized;
    private int _pointerAt;
    private VisualElement _startMarker;
    public int StartAt = -1;
    private VisualElement _endMarker;
    public int EndAt = -1;
    private bool _dragStartMarker;
    private bool _dragEndMarker;

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
        _container = root.Query("timemarkers");

        _startMarker = Create("marker-start", "accent-tint-color");
        _startMarker.style.left = 0;
        _container.Add(_startMarker);

        _endMarker = Create("marker-end", "accent-tint-color");
        _endMarker.style.right = 0;
        _container.Add(_endMarker);

        _labels = new Label[11];
        _verticalMarkers = new VisualElement[11];
        for (int i = 0; i < _verticalMarkers.Length; i++)
        {
            _verticalMarkers[i] = Create("timemarker");
            var line = Create("vertical-line");
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
        _container.RegisterCallback<PointerEnterEvent>(OnPointerEnter);

        _initialized = true;
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        _dragStartMarker = false;
        _dragEndMarker = false;
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        int time0 = (int)math.round(TimelineManager.Instance.TimeInMilliseconds - TimelineManager.Instance.LengthInMilliseconds * 0.5f);
        _pointerAt = (int)math.round(evt.localPosition.x * TimelineManager.Instance.LengthInMilliseconds / _container.contentRect.width) + time0;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        int time0 = (int)math.round(TimelineManager.Instance.TimeInMilliseconds - TimelineManager.Instance.LengthInMilliseconds * 0.5f);
        _pointerAt = (int)math.round(evt.localPosition.x * TimelineManager.Instance.LengthInMilliseconds / _container.contentRect.width) + time0;

        if (!_dragEndMarker && !_dragStartMarker) return;

        if (_dragStartMarker)
        {
            StartAt = _pointerAt;
        }
        else if (_dragEndMarker)
        {
            EndAt = _pointerAt;
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        StartAt = StartAt == -1 ? 0 : StartAt;
        EndAt = EndAt == -1 ? TimelineManager.Instance.LengthInMilliseconds : EndAt;

        _dragStartMarker = false;
        _dragEndMarker = false;

        // swap the markers if the end is before start
        if (EndAt < StartAt)
        {
            int start = math.min(StartAt, EndAt);
            int end = math.max(StartAt, EndAt);
            StartAt = start;
            EndAt = end;
        }
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        int time0 = (int)math.round(TimelineManager.Instance.TimeInMilliseconds - TimelineManager.Instance.LengthInMilliseconds * 0.5f);
        _pointerAt = (int)math.round(evt.localPosition.x * TimelineManager.Instance.LengthInMilliseconds / _container.contentRect.width) + time0;

        if (evt.button == 0 && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            // left click + shift, place right marker
            _dragEndMarker = true;
            EndAt = _pointerAt;
        }
        else if (evt.button == 0)
        {
            // left click, place left marker
            _dragStartMarker = true;
            StartAt = _pointerAt;
        }
    }

    private void Update()
    {
        if (!_initialized) return;
        bool sortFunscript = false;

        // Cut (CTRL-X)
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand))
            && Input.GetKeyDown(KeyCode.X))
        {
            int selectedHapticLayer = GetSelectedHapticLayer();

            if (selectedHapticLayer != -1)
            {
                FunscriptCutPaste.Singleton.StartTimeInMilliseconds = StartAt;
                FunscriptCutPaste.Singleton.EndTimeInMilliseconds = EndAt;
                FunscriptCutPaste.Singleton.Cut(selectedHapticLayer);
                sortFunscript = true;
            }
        }

        // Copy (CTRL-C)
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand))
            && Input.GetKeyDown(KeyCode.C))
        {
            int selectedHapticLayer = GetSelectedHapticLayer();

            if (selectedHapticLayer != -1)
            {
                FunscriptCutPaste.Singleton.StartTimeInMilliseconds = StartAt;
                FunscriptCutPaste.Singleton.EndTimeInMilliseconds = EndAt;
                FunscriptCutPaste.Singleton.Copy(selectedHapticLayer);
            }
        }

        // Paste from the start (CTRL-V), if SHIFT is pressed paste ending at the cursor location
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand))
            && Input.GetKeyDown(KeyCode.V))
        {
            int selectedHapticLayer = GetSelectedHapticLayer();

            if (selectedHapticLayer != -1)
            {
                FunscriptCutPaste.Singleton.Paste(selectedHapticLayer, !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift));
                sortFunscript = true;
            }
        }

        if (sortFunscript)
        {
            FunscriptRenderer.Singleton.SortFunscript();
            FunscriptRenderer.Singleton.CleanupExcessPoints();
            TitleBar.MarkLabelDirty();
        }


        int time0 = (int)math.round(TimelineManager.Instance.TimeInMilliseconds - TimelineManager.Instance.LengthInMilliseconds * 0.5f);
        int time1 = time0 + TimelineManager.Instance.LengthInMilliseconds;

        ShowHideStartMarker(time0, time1);
        ShowHideEndMarker(time0, time1);
        ShowHideVerticalMarkers(time0);
    }

    private int GetSelectedHapticLayer()
    {
        int selectedHapticLayerCount = 0;
        int selectedHapticLayer = -1;

        for (int i = 0; i < FunscriptRenderer.Singleton.Haptics.Count; i++)
        {
            if (FunscriptRenderer.Singleton.Haptics[i].Selected)
            {
                selectedHapticLayerCount++;
                selectedHapticLayer = i;
            }

            if (selectedHapticLayerCount > 1) return -1;
        }

        return selectedHapticLayer;
    }

    private void ShowHideVerticalMarkers(int time0)
    {
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

    private void ShowHideEndMarker(int time0, int time1)
    {
        if (EndAt < 0 || time0 > EndAt || time1 < EndAt)
        {
            _endMarker.style.display = DisplayStyle.None;
        }
        else
        {
            _endMarker.style.display = DisplayStyle.Flex;
            float x = (EndAt - time0) / (float)(TimelineManager.Instance.LengthInMilliseconds);
            x *= _container.contentRect.width;
            x -= _endMarker.contentRect.width;
            _endMarker.style.left = x;
        }
    }

    private void ShowHideStartMarker(int time0, int time1)
    {
        if (StartAt < 0 || time0 > StartAt || time1 < StartAt)
        {
            _startMarker.style.display = DisplayStyle.None;
        }
        else
        {
            _startMarker.style.display = DisplayStyle.Flex;
            float x = (StartAt - time0) / (float)(TimelineManager.Instance.LengthInMilliseconds);
            x *= _container.contentRect.width;
            _startMarker.style.left = x;
        }
    }
}