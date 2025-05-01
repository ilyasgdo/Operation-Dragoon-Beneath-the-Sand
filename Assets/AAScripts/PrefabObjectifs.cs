using UnityEngine;

/// <summary>
/// Classe utilisée pour gérer les préfabs du système d'objectifs
/// Ce script est simplement informatif et peut être attaché à un GameObject vide
/// Il explique comment configurer le système d'objectifs dans votre jeu
/// </summary>
public class PrefabObjectifs : MonoBehaviour
{
    /*
    COMMENT CONFIGURER LE SYSTÈME D'OBJECTIFS DANS VOTRE JEU
    =======================================================
    
    1. CRÉATION DU PRÉFAB POUR L'ÉLÉMENT D'OBJECTIF
    -----------------------------------------------
    a) Créez un GameObject vide dans votre scène (UI > Empty)
    b) Ajoutez-y un composant Image et un composant Text pour la description
    c) Ajoutez-y un composant Toggle pour la case à cocher
    d) Ajoutez le script ElementObjectif.cs à cet objet
    e) Configurez les références:
       - texteDescription -> le Text de la description
       - caseObjectif -> le Toggle
       - couleurNonComplete -> par exemple Color.white
       - couleurCompletee -> par exemple Color.green
    f) Créez un préfab à partir de cet objet en le glissant dans votre dossier Prefabs
    
    2. MISE EN PLACE DU SYSTÈME D'OBJECTIFS
    --------------------------------------
    a) Créez un GameObject vide nommé "SystemeObjectifs" dans votre scène
    b) Ajoutez-y le script SystemeObjectifs.cs
    c) Ajoutez-y un AudioSource si vous voulez des sons pour la complétion d'objectifs
    d) Configurez les références:
       - prefabElementObjectif -> le préfab créé à l'étape 1
       - conteneurObjectifs -> un GameObject de type UI > Panel qui contiendra les objectifs
       - texteObjectifsCompletes -> Text à afficher quand tous les objectifs sont complétés
       - sonObjectifComplete -> Son à jouer quand un objectif est complété
       - sonTousObjectifsCompletes -> Son à jouer quand tous les objectifs sont complétés
    
    3. CONFIGURATION DES PORTES AVEC CODE
    -----------------------------------
    a) Pour chaque porte qui doit être un objectif:
       - Ajoutez le script DoorController.cs à l'objet de la porte
       - Configurez estObjectif = true
       - Donnez un identifiant unique dans doorId (ex: "porte_bureau", "porte_cave", etc.)
       - Référencez systemeObjectifs -> votre GameObject avec le script SystemeObjectifs
    
    4. CRÉATION D'INDICES DANS LE MONDE
    ---------------------------------
    a) Pour des objets qui contiennent des indices sur les codes:
       - Ajoutez le script ObjectifCodePorte.cs à l'objet
       - Configurez la référence vers systemeObjectifs
       - Spécifiez doorId pour indiquer à quelle porte l'indice se rapporte
       - Définissez codeIndice et descriptionIndice
       - Assurez-vous que l'objet possède un Collider pour l'interaction
    
    5. AJOUT D'OBJECTIFS PROGRAMMATIQUEMENT
    -------------------------------------
    a) Vous pouvez aussi ajouter/compléter des objectifs via script:
       
       // Ajouter un objectif
       systemeObjectifs.AjouterObjectif("objectif_1", "Trouver la clé du sous-sol");
       
       // Marquer un objectif comme complété
       systemeObjectifs.CompleterObjectif("objectif_1");
       
       // Vérifier si un objectif est complété
       bool estComplete = systemeObjectifs.EstObjectifComplete("objectif_1");
       
       // Réinitialiser tous les objectifs
       systemeObjectifs.ReinitialiserObjectifs();
    */
    
    void Start()
    {
        // Ce script est informatif et ne fait rien à l'exécution
        Debug.Log("Consultez le code source de PrefabObjectifs.cs pour des instructions sur la configuration du système d'objectifs");
    }
} 