# Guide d'Utilisation du Tableau Interactif

Ce document explique comment configurer et utiliser le composant `TableauInteractif` dans votre projet Unity.

## Description

Le composant `TableauInteractif` permet aux joueurs d'interagir avec des tableaux ou autres objets similaires dans votre jeu. Lorsque le joueur appuie sur la touche F à proximité d'un tableau, la caméra effectue un zoom sur celui-ci et une narration audio est jouée. Le joueur reste fixé sur le tableau jusqu'à la fin de l'audio.

## Configuration

### Étape 1: Ajouter le composant au tableau

1. Sélectionnez l'objet tableau dans votre scène
2. Dans l'Inspector, cliquez sur "Add Component"
3. Recherchez et sélectionnez "Tableau Interactif"

### Étape 2: Configurer les paramètres

#### Configuration d'Interaction
- **Distance Interaction**: Distance maximale à laquelle le joueur peut interagir avec le tableau (en mètres)
- **Player Tag**: Le tag du joueur (par défaut: "Player")

#### Configuration Audio
- **Audio Source**: La source audio qui jouera la narration (sera créée automatiquement si non assignée)
- **Narration Audio**: Le clip audio de narration à jouer
- **Volume Narration**: Volume de la narration (entre 0 et 1)

#### Configuration de Caméra
- **Player Camera**: La caméra du joueur (sera détectée automatiquement si non assignée)
- **Zoom Target**: Position cible pour le zoom de la caméra
  - Utilisez le bouton "Créer une cible de zoom" dans l'éditeur pour générer automatiquement cette cible
  - Vous pouvez ajuster manuellement la position de cette cible pour modifier l'angle de vue
- **Zoom Speed**: Vitesse de transition du zoom
- **Normal FOV**: Champ de vision normal de la caméra
- **Zoom FOV**: Champ de vision en mode zoom (plus petit = plus de zoom)

## Utilisation en jeu

1. Le joueur s'approche du tableau (à la distance configurée)
2. Le joueur appuie sur la touche F pour interagir
3. La caméra zoome sur le tableau et la narration audio commence
4. Les contrôles du joueur sont désactivés pendant la narration
5. Une fois la narration terminée, la caméra revient à sa position initiale et les contrôles du joueur sont réactivés

## Conseils

- Assurez-vous que le joueur a le tag correct configuré dans le paramètre "Player Tag"
- Utilisez des clips audio de bonne qualité pour la narration
- Ajustez la position de la cible de zoom pour obtenir le meilleur angle de vue du tableau
- Vous pouvez voir la zone d'interaction dans l'éditeur grâce aux gizmos (sphère jaune)

## Dépannage

- Si la caméra ne zoome pas correctement, vérifiez que la cible de zoom est correctement positionnée
- Si l'audio ne joue pas, vérifiez que le clip audio est bien assigné et que le volume n'est pas à zéro
- Si les contrôles du joueur ne sont pas désactivés pendant la narration, vérifiez que le joueur utilise bien un CharacterController ou un PlayerInput