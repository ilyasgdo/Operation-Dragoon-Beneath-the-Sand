using UnityEngine;
using UnityEditor;

public class MeshScaler : MonoBehaviour
{
    [MenuItem("Tools/Scale Selected Mesh")]
    static void ScaleSelectedMesh()
    {
        // Ensure a GameObject is selected
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        GameObject selectedObject = Selection.activeGameObject;
        MeshFilter meshFilter = selectedObject.GetComponent<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("Selected GameObject does not have a MeshFilter with a valid mesh.");
            return;
        }

        // Prompt the user for a scale factor
        string input = EditorUtility.DisplayDialogComplex("Scale Mesh", "Choose a scale factor:", "0.5x", "1x", "2x").ToString();
        float scaleFactor = 1f;

        switch (input)
        {
            case "0":
                scaleFactor = 0.5f;
                break;
            case "1":
                scaleFactor = 1f;
                break;
            case "2":
                scaleFactor = 2f;
                break;
            default:
                Debug.LogWarning("Invalid selection.");
                return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;

        // Scale each vertex
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] *= scaleFactor;
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        // Save the scaled mesh as a new asset
        string path = "Assets/Scaled_" + mesh.name + ".asset";
        AssetDatabase.CreateAsset(Object.Instantiate(mesh), path);
        AssetDatabase.SaveAssets();

        Debug.Log("Mesh scaled and saved to: " + path);
    }
}
