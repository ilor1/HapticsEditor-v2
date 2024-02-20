using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class Timemarkers : UIBehaviour
{
    private VisualElement _container;

    private VisualElement[] _verticalMarkers;
    private Label[] _labels;
    private bool _initialized = false;

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

        _labels = new Label[11];
        _verticalMarkers = new VisualElement[11];
        for (int i = 0; i < _verticalMarkers.Length; i++)
        {
            _verticalMarkers[i] = Create("timemarker");
            var line = Create("timemarker-line");
            _verticalMarkers[i].Add(line);

            _labels[i] = Create<Label>();
            _verticalMarkers[i].Add(_labels[i]);

            _verticalMarkers[i].pickingMode = PickingMode.Ignore;
            _container.Add(_verticalMarkers[i]);
        }

        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;

        int time0 = (int)math.round(TimelineManager.Instance.TimeInMilliseconds - TimelineManager.Instance.LengthInMilliseconds * 0.5f);
        int time1 = time0 + TimelineManager.Instance.LengthInMilliseconds;
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