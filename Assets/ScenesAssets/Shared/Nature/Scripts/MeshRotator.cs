using UnityEngine;

public class MeshRotator : MonoBehaviour
{
    [ContextMenu("Rotate Mesh Vertices")]
    void RotateMeshVertices()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter not found.");
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;

        Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f); // Adjust angles as needed

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = rotation * vertices[i];
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Debug.Log("Mesh vertices rotated.");
    }
}
