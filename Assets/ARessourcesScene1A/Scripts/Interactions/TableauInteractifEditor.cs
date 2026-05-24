using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(TableauInteractif))]
public class TableauInteractifEditor : Editor
{
    SerializedProperty distanceInteraction;
    SerializedProperty playerTag;
    SerializedProperty audioSource;
    SerializedProperty narrationAudio;
    SerializedProperty volumeNarration;
    SerializedProperty playerCamera;
    SerializedProperty zoomTarget;
    SerializedProperty zoomSpeed;
    SerializedProperty normalFOV;
    SerializedProperty zoomFOV;
    
    private void OnEnable()
    {
        // Récupérer les propriétés sérialisées
        distanceInteraction = serializedObject.FindProperty("distanceInteraction");
        playerTag = serializedObject.FindProperty("playerTag");
        audioSource = serializedObject.FindProperty("audioSource");
        narrationAudio = serializedObject.FindProperty("narrationAudio");
        volumeNarration = serializedObject.FindProperty("volumeNarration");
        playerCamera = serializedObject.FindProperty("playerCamera");
        zoomTarget = serializedObject.FindProperty("zoomTarget");
        zoomSpeed = serializedObject.FindProperty("zoomSpeed");
        normalFOV = serializedObject.FindProperty("normalFOV");
        zoomFOV = serializedObject.FindProperty("zoomFOV");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        TableauInteractif tableau = (TableauInteractif)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Ce composant permet au joueur d'interagir avec un tableau en appuyant sur F pour déclencher un zoom de caméra et jouer une narration audio.", MessageType.Info);
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Configuration d'Interaction", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(distanceInteraction);
        EditorGUILayout.PropertyField(playerTag);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Configuration Audio", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(audioSource);
        EditorGUILayout.PropertyField(narrationAudio);
        
        if (narrationAudio.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Veuillez assigner un clip audio de narration.", MessageType.Warning);
        }
        
        EditorGUILayout.PropertyField(volumeNarration);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Configuration de Caméra", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(playerCamera);
        EditorGUILayout.PropertyField(zoomTarget);
        
        if (zoomTarget.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Veuillez créer et assigner un Transform comme cible de zoom.", MessageType.Warning);
            
            if (GUILayout.Button("Créer une cible de zoom"))
            {
                // Créer un nouvel objet comme cible de zoom
                GameObject zoomTargetObj = new GameObject("ZoomTarget_" + tableau.gameObject.name);
                zoomTargetObj.transform.position = tableau.transform.position + (tableau.transform.forward * -1.5f) + (Vector3.up * 0.5f);
                zoomTargetObj.transform.parent = tableau.transform;
                
                // Assigner la cible de zoom
                tableau.zoomTarget = zoomTargetObj.transform;
                EditorUtility.SetDirty(tableau);
            }
        }
        
        EditorGUILayout.PropertyField(zoomSpeed);
        EditorGUILayout.PropertyField(normalFOV);
        EditorGUILayout.PropertyField(zoomFOV);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Appuyez sur F à proximité du tableau pour déclencher l'interaction.", MessageType.Info);
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif