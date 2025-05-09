using UnityEngine;
using UnityEditor;

public class MeshCombiner : MonoBehaviour
{
    [MenuItem("Tools/Combine Selected Meshes")]
    static void CombineMeshes()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length < 2)
        {
            Debug.LogWarning("Select at least 2 GameObjects with meshes.");
            return;
        }

        CombineInstance[] combine = new CombineInstance[selectedObjects.Length];

        for (int i = 0; i < selectedObjects.Length; i++)
        {
            MeshFilter mf = selectedObjects[i].GetComponent<MeshFilter>();
            if (mf == null)
            {
                Debug.LogWarning("One of the selected objects does not have a MeshFilter.");
                return;
            }

            combine[i].mesh = mf.sharedMesh;
            combine[i].transform = mf.transform.localToWorldMatrix;
        }

        Mesh finalMesh = new Mesh();
        finalMesh.CombineMeshes(combine);

        GameObject combinedObj = new GameObject("CombinedMesh");
        combinedObj.AddComponent<MeshFilter>().sharedMesh = finalMesh;
        combinedObj.AddComponent<MeshRenderer>().sharedMaterial = selectedObjects[0].GetComponent<MeshRenderer>()?.sharedMaterial;

        // Optionally save mesh as asset
        AssetDatabase.CreateAsset(finalMesh, "Assets/CombinedMesh.asset");
        AssetDatabase.SaveAssets();

        Debug.Log("Mesh combined successfully and saved as asset.");
    }
}
