using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ProjectVersionReader : MonoBehaviour
{
    [SerializeField] private ProjectInfoSO _projectInfoSo;

    void Start()
    {
#if UNITY_EDITOR
        string projectVersion = GetProjectVersion();
        Debug.Log("Project Version: " + projectVersion);
        _projectInfoSo.Version = projectVersion;
#endif
    }

#if UNITY_EDITOR
    string GetProjectVersion()
    {
        var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var versionProperty = playerSettings.FindProperty("bundleVersion");
        return versionProperty.stringValue;
    }
#endif
}