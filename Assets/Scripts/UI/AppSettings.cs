public struct AppSettings
{
    public bool fillMode;
    public bool timelineMarkersAsTime;
    public ScriptingMode Mode;
}

public enum ScriptingMode
{
    Default,
    Pattern,
    Free,
    Stroker
}
