# Système d'Objectifs pour Operation Dragoon

Ce système permet d'ajouter et de gérer des objectifs dans votre jeu. Il comprend une interface utilisateur pour afficher les objectifs actifs et les notifications lorsque des objectifs sont ajoutés ou complétés.

## Comment utiliser le système

### Configuration initiale

1. Créez un GameObject vide dans votre scène et nommez-le "ObjectiveSystem"
2. Ajoutez le composant `ObjectiveManager` à ce GameObject
3. Créez l'interface utilisateur des objectifs (voir ci-dessous)
4. Liez les références UI dans l'inspecteur du composant ObjectiveManager

### Création de l'interface utilisateur

Vous aurez besoin de créer:

1. Un panneau d'objectifs (qui s'affiche lorsque le joueur appuie sur Tab)
   - Un conteneur pour la liste des objectifs
   - Un préfab d'élément d'objectif avec:
     - Text pour le titre
     - Text pour la description
     - Toggle (optionnel) pour indiquer l'état de complétion
   - Un texte pour afficher le nombre d'objectifs

2. Un panneau de notification
   - Un texte pour afficher les messages de notification

### Utilisation dans vos scripts

```csharp
// Obtenir une référence à l'ObjectiveManager
ObjectiveManager objectiveManager = ObjectiveManager.Instance;

// Ajouter un nouvel objectif
objectiveManager.AddObjective(
    "Titre de l'objectif", 
    "Description détaillée de l'objectif",
    false,  // Optionnel ou non
    10      // Points de récompense
);

// Marquer un objectif comme complété
objectiveManager.CompleteObjective("Titre de l'objectif");

// Vérifier si un objectif est complété
bool estComplete = objectiveManager.IsObjectiveCompleted("Titre de l'objectif");

// Vérifier si un objectif est actif
bool estActif = objectiveManager.IsObjectiveActive("Titre de l'objectif");

// Obtenir le total des points
int points = objectiveManager.GetTotalPoints();
```

### Exemple d'utilisation

Un script d'exemple `ObjectiveExample.cs` est inclus pour montrer comment utiliser le système d'objectifs. Il vous permet de:

- Ajouter des objectifs aléatoires en appuyant sur la touche 'O'
- Compléter des objectifs aléatoires en appuyant sur la touche 'P'

## Personnalisation

Vous pouvez personnaliser l'apparence de l'interface utilisateur en modifiant les préfabs et les styles de texte. Vous pouvez également ajuster les paramètres comme la durée d'affichage des notifications dans l'inspecteur.

## Intégration avec d'autres systèmes

Ce système d'objectifs peut être facilement intégré avec d'autres systèmes de votre jeu:

- Système de progression: Utilisez `IsObjectiveCompleted()` pour vérifier si certains objectifs sont complétés avant de débloquer de nouvelles zones ou fonctionnalités
- Système de récompenses: Utilisez `GetTotalPoints()` pour attribuer des récompenses basées sur les points accumulés
- Système de sauvegarde: Sauvegardez les objectifs complétés pour maintenir la progression du joueur entre les sessions