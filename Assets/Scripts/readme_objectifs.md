# Système d'Objectifs - Guide d'Utilisation

Ce système permet d'ajouter et de gérer facilement des objectifs dans votre jeu. Les objectifs peuvent être déclenchés par différentes actions du joueur comme entrer dans une zone, interagir avec des objets, ou ouvrir des portes.

## Configuration de Base

1. Ajoutez le script `ObjectifManager` à un GameObject vide dans votre scène principale (de préférence un GameObject qui persiste entre les scènes).

2. Créez une interface utilisateur (UI) pour afficher les objectifs:
   - Créez un Canvas avec un Panel qui servira de conteneur pour les objectifs
   - Assignez ce Panel au champ `panneauObjectifs` du ObjectifManager
   - Créez un prefab pour chaque élément d'objectif contenant:
     - Un Text pour afficher la description
     - Un Toggle pour indiquer l'état de complétion
   - Ajoutez le script `ElementObjectif` à ce prefab
   - Assignez ce prefab au champ `prefabElementObjectif` du ObjectifManager
   - Créez un GameObject vide comme enfant du Panel pour servir de conteneur et assignez-le au champ `conteneurObjectifs`

3. Configurez les objectifs dans l'inspecteur du ObjectifManager:
   - Chaque objectif doit avoir un ID unique
   - Ajoutez une description qui sera affichée au joueur
   - Définissez si l'objectif est optionnel ou obligatoire
   - Pour créer une séquence d'objectifs, utilisez le champ `objetSuivantId` pour indiquer l'objectif qui sera débloqué une fois l'objectif actuel complété

## Comment Déclencher des Objectifs

Vous pouvez déclencher des objectifs de deux façons principales:

### 1. Avec des Zones (Triggers)

Utilisez le script `ObjectifTrigger` pour créer des zones qui complètent des objectifs quand le joueur les atteint:

1. Créez un GameObject avec un Collider (assurez-vous que "Is Trigger" est activé)
2. Ajoutez le script `ObjectifTrigger` au GameObject
3. Configurez l'ID de l'objectif à compléter
4. Choisissez le mode de déclenchement:
   - `declencherSurEntree`: L'objectif est complété dès que le joueur entre dans la zone
   - `declencherSurTouche`: L'objectif est complété quand le joueur appuie sur une touche spécifique dans la zone

### 2. Avec des Interactions Existantes

Utilisez le script `ObjectifInteraction` pour lier des objectifs aux interactions existantes dans votre jeu:

1. Créez un GameObject et ajoutez-y le script `ObjectifInteraction`
2. Configurez l'ID de l'objectif à compléter
3. Référencez l'un des objets interactifs suivants:
   - `TableauInteractif`: L'objectif sera complété quand le joueur aura fini de consulter le tableau
   - `FeuilleInteractive`: L'objectif sera complété quand le joueur aura fini de lire la feuille
   - `DoorController`: L'objectif sera complété quand le joueur ouvrira/fermera la porte

## Fonctionnalités Avancées

- **Actions supplémentaires**: Les scripts `ObjectifTrigger` et `ObjectifInteraction` incluent un UnityEvent qui peut être utilisé pour déclencher des actions supplémentaires lorsqu'un objectif est complété (ouvrir une porte, jouer un son, etc.).

- **Objectifs en séquence**: Vous pouvez créer une chaîne d'objectifs en utilisant le champ `objetSuivantId`. Les objectifs qui sont mentionnés comme "suivants" d'autres objectifs ne seront pas affichés au joueur tant que les objectifs précédents ne sont pas complétés.

- **Vérification de progression**: La méthode `TousObjectifsObligatoiresComplétés()` de l'ObjectifManager permet de vérifier si tous les objectifs obligatoires ont été complétés, ce qui peut être utilisé pour débloquer la fin du niveau ou du jeu.

## Exemple d'Utilisation

Voici un exemple de configuration simple avec trois objectifs:

1. Objectif "explorer_maison" (obligatoire): "Explorer la maison"
   - Débloquer l'objectif "trouver_cle" quand complété
   - Utilisez un ObjectifTrigger placé dans le hall d'entrée

2. Objectif "trouver_cle" (obligatoire): "Trouver la clé du sous-sol"
   - Débloquer l'objectif "ouvrir_porte" quand complété
   - Utilisez un ObjectifInteraction lié à une FeuilleInteractive

3. Objectif "ouvrir_porte" (obligatoire): "Ouvrir la porte du sous-sol"
   - Utilisez un ObjectifInteraction lié à un DoorController

Cette séquence guide le joueur dans une progression logique à travers votre niveau. 