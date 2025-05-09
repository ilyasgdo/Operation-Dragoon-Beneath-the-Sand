using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class MeshUnlinker : EditorWindow
{
    [MenuItem("Tools/Unlink Selected Prefabs Meshes")]
    static void ShowWindow()
    {
        GetWindow<MeshUnlinker>("Mesh Unlinker");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Unlink Meshes on Selected Prefabs"))
        {
            UnlinkMeshes();
        }
    }

    static void UnlinkMeshes()
    {
        var selections = Selection.objects;
        foreach (var obj in selections)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) continue;

            // Instantiate prefab in a hidden scene
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            MeshFilter[] mfs = instance.GetComponentsInChildren<MeshFilter>(true);
            SkinnedMeshRenderer[] sms = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            bool changed = false;
            string dir = Path.GetDirectoryName(assetPath);
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh == null) continue;
                Mesh newMesh = Instantiate(mf.sharedMesh);
                newMesh.name = mf.sharedMesh.name + "_Unlinked";
                string meshPath = $"{dir}/{newMesh.name}.asset";
                AssetDatabase.CreateAsset(newMesh, meshPath);
                mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                changed = true;
            }

            foreach (var sk in sms)
            {
                if (sk.sharedMesh == null) continue;
                Mesh newMesh = Instantiate(sk.sharedMesh);
                newMesh.name = sk.sharedMesh.name + "_Unlinked";
                string meshPath = $"{dir}/{newMesh.name}.asset";
                AssetDatabase.CreateAsset(newMesh, meshPath);
                sk.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                changed = true;
            }

            if (changed)
            {
                // Apply overrides back to prefab
                PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
                Debug.Log($"Unlinked meshes and saved prefab: {assetPath}");
            }

            DestroyImmediate(instance);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
