# Guide d'utilisation du système de projectiles

## Introduction
Ce système permet d'ajouter des projectiles (balles) à votre arme dans le jeu. Lorsque vous tirez, des projectiles sont instantanément créés et se déplacent dans la direction de l'arme.

## Configuration de l'arme
1. Sélectionnez votre arme dans la hiérarchie de la scène
2. Dans l'inspecteur, trouvez le composant `GunfireController`
3. Assignez le prefab `Bullet` au champ `Projectile Prefab`
4. Assurez-vous que `Muzzle Position` est configuré avec un GameObject vide placé à l'extrémité du canon de l'arme

## Personnalisation des projectiles
Vous pouvez modifier les propriétés des projectiles de deux façons :

### Option 1: Modifier le prefab Bullet
1. Ouvrez le dossier `Assets/BigRookGames/_AssetPacks/Stylized Weapon Pack/M4 Scoped Assault Rifle/Prefabs`
2. Sélectionnez le prefab `Bullet`
3. Dans l'inspecteur, ajustez les propriétés du composant `ProjectileController` :
   - `Projectile Speed` : Vitesse du projectile (unités par seconde)
   - `Lifetime` : Durée de vie avant destruction automatique (secondes)
   - `Damage` : Dégâts causés par le projectile
   - `Impact Effect` : Effet visuel à instancier lors de l'impact (optionnel)

### Option 2: Créer votre propre prefab de projectile
1. Créez un nouveau GameObject
2. Ajoutez un Mesh (comme une sphère ou une capsule)
3. Ajoutez un Collider (comme un SphereCollider ou CapsuleCollider)
4. Ajoutez le script `ProjectileController`
5. Configurez les propriétés selon vos besoins
6. Créez un prefab à partir de ce GameObject
7. Assignez votre nouveau prefab au champ `Projectile Prefab` du `GunfireController`

## Fonctionnalités avancées
Le système de projectiles prend en charge :
- La détection de collision avec d'autres objets
- L'instanciation d'effets d'impact lors des collisions
- La possibilité d'appliquer des dégâts (nécessite l'implémentation d'un système de santé)

## Dépannage
Si les projectiles ne sont pas visibles ou ne fonctionnent pas correctement :
1. Vérifiez que le prefab de projectile est correctement assigné
2. Assurez-vous que `Muzzle Position` est configuré et placé correctement
3. Vérifiez que le projectile a un Mesh Renderer visible et un Collider approprié
4. Assurez-vous que la vitesse du projectile n'est pas trop élevée ou trop basse