# Ray Tracing simplifié pour URP

Ce dossier contient tous les outils nécessaires pour implémenter un ray tracing simplifié dans Unity avec URP (Universal Render Pipeline).

## Contenu du dossier

- `SimpleRayTracing.shader` - Shader de ray marching qui simule le ray tracing
- `RayTracingRenderFeature.cs` - Script pour intégrer le ray tracing dans le pipeline URP
- `RayTracingExample.cs` - Script d'exemple pour contrôler les paramètres du ray tracing

## Fonctionnalités visuelles

Le système de ray tracing implémente les effets suivants:

- **Ombres douces** avec contrôle de l'intensité et de la douceur
- **Réflexions** avec variation selon les matériaux
- **Brouillard volumétrique** pour ajouter de la profondeur
- **Éclairage avancé** avec composantes diffuse, spéculaire et ambiante
- **Formes primitives variées** (sphères, tores, boîtes)
- **Multiples objets** incluant des patterns de répétition
- **Matériaux différents** avec contrôle de la réflectivité et du lissage
- **Rendu de ciel** dynamique

## Guide d'implémentation

### 1. Configuration de base

1. **Créer un matériau de ray tracing**:
   - Dans le Project window, cliquez-droit → Create → Material
   - Nommez-le "RayTracingMaterial"
   - Dans l'Inspector, sélectionnez le shader "Custom/SimpleRayTracing"
   - Ajustez les paramètres selon vos préférences

2. **Configurer le Renderer URP**:
   - Ouvrez votre "Universal Render Pipeline Asset"
   - Trouvez le "Renderer" que vous utilisez (Forward Renderer, etc.)
   - Cliquez sur ce Renderer pour l'ouvrir

3. **Ajouter le RenderFeature**:
   - Dans l'Inspector du Forward Renderer, cliquez sur "Add Render Feature"
   - Sélectionnez "RayTracingRenderFeature"
   - Assignez le "RayTracingMaterial" créé précédemment au champ "Ray Tracing Material"

### 2. Utilisation dans une scène

1. **Créer un GameObject pour contrôler le ray tracing**:
   - Dans la Hierarchy, cliquez-droit → Create Empty
   - Nommez-le "RayTracingController"
   - Ajoutez le composant "RayTracingExample" (Add Component → Scripts → RayTracingExample)
   - Assignez le "RayTracingMaterial" au champ correspondant

2. **Ajuster les paramètres**:
   - Dans l'Inspector du RayTracingController, vous pouvez modifier:
   
     **Paramètres de base:**
     - Max Steps: nombre maximum d'étapes pour le ray marching (plus élevé = plus précis mais plus lent)
     - Max Distance: distance maximale de rendu
     - Hit Distance: précision de détection des collisions
     - Reflection Strength: intensité des réflexions
   
     **Sphères:**
     - Positions et couleurs des sphères
     - Rayon des sphères
   
     **Éclairage:**
     - Light Color: couleur de la lumière principale
     - Ambient Color: couleur de l'éclairage ambiant
     - Shadow Intensity: intensité des ombres
     - Shadow Softness: douceur des ombres
     - Specular Power: concentration des reflets spéculaires
     - Specular Intensity: intensité des reflets spéculaires
   
     **Effets atmosphériques:**
     - Fog Density: densité du brouillard volumétrique
     - Fog Color: couleur du brouillard
   
     **Animation:**
     - Animate Spheres: activer/désactiver l'animation des sphères
     - Animation Speed: vitesse d'animation des sphères
     - Animate Light: activer/désactiver l'animation de la lumière
     - Light Animation Speed: vitesse d'animation de la lumière

### 3. Personnalisation avancée

Pour personnaliser davantage le rendu ray tracing:

1. **Modifier le shader**:
   - Ouvrez `SimpleRayTracing.shader` pour ajouter de nouvelles formes
   - Modifiez la fonction `SceneDistance` pour introduire d'autres primitives
   - Ajoutez des effets comme la réfraction, les ombres colorées, etc.

2. **Ajouter de nouvelles fonctionnalités**:
   - Étendez le script `RayTracingExample.cs` pour contrôler d'autres paramètres
   - Implémentez des variations dans la fonction `RayMarch` pour des effets spéciaux
   - Ajoutez des contrôles pour la direction de la lumière

## Optimiser les performances

Pour améliorer les performances du ray tracing:

1. **Ajustez les paramètres critiques**:
   - Réduisez Max Steps pour les scènes simples (32-64)
   - Augmentez Hit Distance pour des collisions moins précises mais plus rapides
   - Réduisez Shadow Softness pour des ombres plus nettes mais plus rapides à calculer

2. **Optimisez la complexité de la scène**:
   - Simplifiez les fonctions SDF dans le shader
   - Limitez le nombre d'objets avec réflexions
   - Désactivez les ombres pour les objets distants

3. **Utilisez une résolution adaptative**:
   - Vous pouvez modifier la RenderFeature pour rendre le ray tracing à une résolution plus basse

## Notes importantes

- Cette implémentation utilise une technique de ray marching, qui est une approximation du ray tracing
- Les performances dépendent fortement de la puissance de votre GPU
- Pour de meilleurs résultats visuels, augmentez Max Steps et réduisez Hit Distance
- Pour un ray tracing avancé, envisagez de passer à HDRP qui supporte le ray tracing hardware

## Dépannage

- **Performances faibles**: Réduisez Max Steps, Shadow Softness ou augmentez Hit Distance
- **Artefacts visuels**: Augmentez la précision en réduisant Hit Distance
- **Ombres trop prononcées**: Réduisez Shadow Intensity ou augmentez Shadow Softness
- **Reflets trop faibles**: Augmentez Specular Intensity et ajustez Specular Power
- **Brouillard trop dense**: Réduisez Fog Density ou ajustez la couleur pour qu'elle soit plus proche de celle de votre scène

## Limitations

- Cette implémentation est basée sur le ray marching, qui est moins précis que le ray tracing matériel
- URP ne prend pas en charge nativement le ray tracing matériel (RTX)
- Les réflexions sont limitées à un seul rebond
- Les performances peuvent être limitées sur les appareils mobiles ou de faible puissance

---

Pour toute question ou problème, référez-vous à la documentation Unity sur les Render Features URP et les Shaders. 