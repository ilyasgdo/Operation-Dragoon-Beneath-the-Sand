# Préfabs pour le Système d'Objectifs

Ce document explique comment créer les préfabs nécessaires pour le système d'objectifs.

## Préfab du Système d'Objectifs

1. Créez un GameObject vide et nommez-le "ObjectiveSystem"
2. Ajoutez le composant `ObjectiveManager` à ce GameObject
3. Configurez les références dans l'inspecteur (après avoir créé les préfabs UI)

## Préfabs de l'Interface Utilisateur

### Panneau d'Objectifs

1. Créez un Canvas (UI > Canvas)
2. Ajoutez un Panel comme enfant du Canvas et nommez-le "ObjectivePanel"
   - Positionnez-le dans un coin de l'écran (par exemple, en haut à droite)
   - Ajustez sa taille et son opacité selon vos préférences
3. Ajoutez un Text comme enfant du Panel pour le titre "Objectifs"
4. Ajoutez un Text pour afficher le compteur d'objectifs
5. Ajoutez un ScrollRect avec un Viewport et un Content pour la liste des objectifs
6. Ajoutez le composant `ObjectiveUI` au Panel

### Préfab d'Élément d'Objectif

1. Créez un GameObject vide dans le Content du ScrollRect et nommez-le "ObjectiveItem"
2. Ajoutez un Image comme fond
3. Ajoutez un Text pour le titre de l'objectif
4. Ajoutez un Text pour la description de l'objectif
5. Ajoutez un Toggle pour indiquer l'état de complétion
6. Ajoutez le composant `ObjectiveItem` à ce GameObject
7. Configurez les références dans l'inspecteur
8. Créez un préfab à partir de ce GameObject (faites glisser vers le dossier Prefabs)

### Panneau de Notification

1. Créez un Panel comme enfant du Canvas et nommez-le "NotificationPanel"
   - Positionnez-le en bas de l'écran
   - Ajustez sa taille et son opacité
2. Ajoutez un Text pour afficher les messages de notification

## Configuration dans l'Inspecteur

### ObjectiveManager

Dans l'inspecteur du composant ObjectiveManager, configurez les références suivantes :

- **objectiveUIPanel**: Référence au ObjectivePanel
- **objectivePrefab**: Référence au préfab ObjectiveItem
- **objectivesContainer**: Référence au Content du ScrollRect
- **objectiveCountText**: Référence au texte du compteur d'objectifs
- **notificationPanel**: Référence au NotificationPanel
- **notificationText**: Référence au texte de notification

### ObjectiveUI

Dans l'inspecteur du composant ObjectiveUI, configurez les références similaires.

## Zones d'Objectifs

1. Créez un GameObject vide et nommez-le "ObjectiveZone"
2. Ajoutez un Collider (Box, Sphere, etc.) et cochez "Is Trigger"
3. Ajoutez le composant `ObjectiveZone`
4. Configurez les paramètres de l'objectif dans l'inspecteur
5. Créez un préfab à partir de ce GameObject

## Intégration avec le Joueur

1. Trouvez le GameObject du joueur avec le FirstPersonController
2. Ajoutez le composant `ObjectiveIntegration`
3. Configurez les zones d'objectifs dans l'inspecteur

Vous pouvez maintenant dupliquer et placer des zones d'objectifs dans votre scène pour créer des missions et des objectifs pour le joueur.