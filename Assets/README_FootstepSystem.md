# Système de Sons de Pas

Ce système permet de jouer différents sons de pas lorsque le joueur marche sur différentes surfaces dans le jeu.

## Configuration

### 1. Configuration des surfaces

Pour chaque surface sur laquelle vous souhaitez avoir un son de pas spécifique :

1. Ajoutez un Collider à l'objet de surface (terrain, plancher, etc.)
2. Cochez l'option "Is Trigger" sur ce Collider
3. Ajoutez le script approprié selon la surface :
   - `SoundWalkWood.cs` pour les surfaces en bois
   - `SoundWalkSand.cs` pour les surfaces en sable
   - Vous pouvez créer d'autres scripts similaires pour d'autres types de surfaces

### 2. Configuration des sons

Pour chaque script de surface :

1. Assignez un `AudioClip` contenant le son de pas approprié
2. Ajustez le volume si nécessaire
3. Assurez-vous que le tag du joueur est correctement défini (par défaut : "Player")

### 3. Configuration du joueur

1. Assurez-vous que votre personnage joueur a le tag "Player"
2. Ajoutez le script `FootstepManager.cs` au personnage joueur
3. Configurez l'intervalle entre les sons de pas

## Fonctionnement

Le système fonctionne de deux manières :

1. **Détection automatique** : Lorsque le joueur entre dans le trigger d'une surface, le son de pas correspondant est joué
2. **Détection par raycast** : Le `FootstepManager` lance un rayon vers le bas pour détecter la surface sur laquelle se trouve le joueur et joue le son approprié à intervalles réguliers

## Personnalisation

Vous pouvez facilement étendre ce système en :

1. Créant de nouveaux scripts pour différents types de surfaces (métal, herbe, etc.)
2. Ajoutant des variations de sons pour chaque type de surface
3. Ajustant les paramètres comme le volume, la fréquence des pas, etc.

## Conseils d'utilisation

- Assurez-vous que les colliders des surfaces ne se chevauchent pas trop pour éviter des conflits de sons
- Utilisez des sons courts et de bonne qualité pour les pas
- Ajustez l'intervalle des pas en fonction de la vitesse de déplacement du joueur pour plus de réalisme