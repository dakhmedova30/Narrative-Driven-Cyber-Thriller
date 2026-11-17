using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Tools > Audio > Add Audio Manager To Open Scene
///
/// Yarn's &lt;&lt;sfx&gt;&gt; / &lt;&lt;ambience&gt;&gt; / &lt;&lt;music&gt;&gt; only make sound if the scene has an AudioManager
/// with the Sound Library assigned. This puts the shared "Audio" prefab into the open scene
/// (and creates the prefab the first time, already linked to Assets/Audio/SoundLibrary.asset).
/// </summary>
public static class AudioSetupTool
{
    private const string Title = "Audio";
    private const string PrefabPath = "Assets/Prefabs/Audio/Audio.prefab";
    private const string LibraryPath = "Assets/Audio/SoundLibrary.asset";

    [MenuItem("Tools/Audio/Add Audio Manager To Open Scene")]
    public static void AddToScene()
    {
        if (Object.FindAnyObjectByType<AudioManager>() != null)
        {
            Selection.activeObject = Object.FindAnyObjectByType<AudioManager>().gameObject;
            EditorUtility.DisplayDialog(Title, "This scene already has an AudioManager (now selected).\nCheck that its Library field is set.", "OK");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) ?? CreatePrefab();
        if (prefab == null) return;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Add Audio Manager");
        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(instance.scene);
        Debug.Log("[Audio] Added the Audio prefab to " + instance.scene.name + ". Save the scene (Ctrl+S).");
    }

    private static GameObject CreatePrefab()
    {
        var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
        if (library == null)
        {
            EditorUtility.DisplayDialog(Title, "Can't find " + LibraryPath, "OK");
            return null;
        }

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
        var go = new GameObject("Audio");
        var manager = go.AddComponent<AudioManager>();
        var so = new SerializedObject(manager);
        so.FindProperty("library").objectReferenceValue = library;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }
}
