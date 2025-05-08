Shader "Custom/SimpleRayTracing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaxDistance ("Distance Maximum", Float) = 100
        _MaxSteps ("Étapes Maximum", Int) = 100
        _HitDistance ("Distance de Contact", Float) = 0.01
        _ReflectionStrength ("Force de Réflexion", Range(0, 1)) = 0.5
        _SphereRadius ("Rayon des Sphères", Float) = 1.0
        _SpherePos1 ("Position Sphère 1", Vector) = (0, 1, 0, 0)
        _SpherePos2 ("Position Sphère 2", Vector) = (2, 1, 2, 0)
        _SphereColor1 ("Couleur Sphère 1", Color) = (1, 0, 0, 1)
        _SphereColor2 ("Couleur Sphère 2", Color) = (0, 0, 1, 1)
        
        // Paramètres d'éclairage
        _ShadowIntensity ("Intensité des Ombres", Range(0, 1)) = 0.75
        _ShadowSoftness ("Douceur des Ombres", Range(1, 64)) = 16
        _FogDensity ("Densité du Brouillard", Range(0, 0.1)) = 0.01
        _FogColor ("Couleur du Brouillard", Color) = (0.75, 0.85, 1.0, 1)
        _SpecularPower ("Puissance Spéculaire", Range(1, 128)) = 32
        _SpecularIntensity ("Intensité Spéculaire", Range(0, 1)) = 0.5
        _LightColor ("Couleur Lumière", Color) = (1, 1, 1, 1)
        _LightDirection ("Direction Lumière", Vector) = (1, 1, 1, 0)
        _AmbientColor ("Couleur Ambiante", Color) = (0.2, 0.2, 0.3, 1)
        
        // Textures
        [Toggle] _UseTextures ("Utiliser des Textures", Float) = 0
        _GroundTexture ("Texture du Sol", 2D) = "white" {}
        _Sphere1Texture ("Texture Sphère 1", 2D) = "white" {}
        _Sphere2Texture ("Texture Sphère 2", 2D) = "white" {}
        _TextureTiling ("Répétition Texture", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _USE_TEXTURES
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewVectorWS : TEXCOORD1;
                float3 cameraPosWS : TEXCOORD2;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _MaxDistance;
            int _MaxSteps;
            float _HitDistance;
            float _ReflectionStrength;
            float _SphereRadius;
            float4 _SpherePos1;
            float4 _SpherePos2;
            float4 _SphereColor1;
            float4 _SphereColor2;
            
            // Paramètres d'éclairage
            float _ShadowIntensity;
            float _ShadowSoftness;
            float _FogDensity;
            float4 _FogColor;
            float _SpecularPower;
            float _SpecularIntensity;
            float4 _LightColor;
            float4 _LightDirection;
            float4 _AmbientColor;
            
            // Textures
            float _UseTextures;
            TEXTURE2D(_GroundTexture);
            SAMPLER(sampler_GroundTexture);
            TEXTURE2D(_Sphere1Texture);
            SAMPLER(sampler_Sphere1Texture);
            TEXTURE2D(_Sphere2Texture);
            SAMPLER(sampler_Sphere2Texture);
            float _TextureTiling;
            
            // Structure pour stocker les informations d'intersection
            struct HitInfo {
                bool hit;
                float3 position;
                float3 normal;
                float3 color;
                bool isReflective;
                float smoothness;
                float2 uv;
                int materialID; // 0 = sol, 1 = sphère 1, 2 = sphère 2, etc.
            };
            
            // SDF pour une sphère
            float SDFSphere(float3 p, float3 center, float radius)
            {
                return length(p - center) - radius;
            }
            
            // SDF pour un plan
            float SDFPlane(float3 p, float3 normal, float height)
            {
                return dot(p, normal) + height;
            }
            
            // Fonction pour calculer les coordonnées UV
            float2 CalculateUV(float3 p, float3 normal, int materialID)
            {
                float2 uv = float2(0, 0);
                
                if (materialID == 0) // Sol
                {
                    // Coordonnées UV planaires pour le sol
                    uv = float2(p.x, p.z) * _TextureTiling;
                }
                else if (materialID == 1 || materialID == 2) // Sphères
                {
                    // Coordonnées UV sphériques
                    float3 center = (materialID == 1) ? _SpherePos1.xyz : _SpherePos2.xyz;
                    float3 dir = normalize(p - center);
                    
                    // Mapping sphérique simple
                    float u = 0.5 + atan2(dir.z, dir.x) / (2.0 * 3.14159);
                    float v = 0.5 - asin(dir.y) / 3.14159;
                    
                    uv = float2(u, v) * _TextureTiling;
                }
                
                return uv;
            }
            
            // Calcul SDF pour toute la scène
            float SceneDistance(float3 position, out float3 color, out bool isReflective, out float smoothness, out int materialID)
            {
                // Sphères
                float sphere1 = SDFSphere(position, _SpherePos1.xyz, _SphereRadius);
                float sphere2 = SDFSphere(position, _SpherePos2.xyz, _SphereRadius);
                
                // Plan / sol
                float plane = SDFPlane(position, float3(0, 1, 0), 0);
                
                // Déterminer quel objet est le plus proche
                float minDist = min(min(sphere1, sphere2), plane);
                
                // Définir couleur et propriétés de réflexion en fonction de l'objet le plus proche
                smoothness = 0.5; // valeur par défaut
                materialID = -1;
                
                if (minDist == sphere1) {
                    color = _SphereColor1.rgb;
                    isReflective = true;
                    smoothness = 0.8;
                    materialID = 1;
                }
                else if (minDist == sphere2) {
                    color = _SphereColor2.rgb;
                    isReflective = true;
                    smoothness = 0.9;
                    materialID = 2;
                }
                else {
                    // Damier pour le sol
                    float checkerSize = 1.0;
                    float3 p = position;
                    float check = frac(floor(p.x / checkerSize) + floor(p.z / checkerSize)) * 2;
                    color = lerp(float3(0.8, 0.8, 0.8), float3(0.2, 0.2, 0.2), check);
                    isReflective = true;
                    smoothness = 0.3;
                    materialID = 0;
                }
                
                return minDist;
            }
            
            // Calcul de la normale
            float3 CalculateNormal(float3 p)
            {
                float2 e = float2(0.001, 0);
                float3 dummyColor;
                bool dummyReflective;
                float dummySmoothness;
                int dummyMatID;
                
                float3 normal = normalize(float3(
                    SceneDistance(p + e.xyy, dummyColor, dummyReflective, dummySmoothness, dummyMatID) - SceneDistance(p - e.xyy, dummyColor, dummyReflective, dummySmoothness, dummyMatID),
                    SceneDistance(p + e.yxy, dummyColor, dummyReflective, dummySmoothness, dummyMatID) - SceneDistance(p - e.yxy, dummyColor, dummyReflective, dummySmoothness, dummyMatID),
                    SceneDistance(p + e.yyx, dummyColor, dummyReflective, dummySmoothness, dummyMatID) - SceneDistance(p - e.yyx, dummyColor, dummyReflective, dummySmoothness, dummyMatID)
                ));
                
                return normal;
            }
            
            // Calcul des ombres douces
            float CalculateSoftShadow(float3 ro, float3 rd, float mint, float maxt, float k)
            {
                float result = 1.0;
                float t = mint;
                float ph = 1e10; // big, such that y = 0 on the first iteration
                
                for (int i = 0; i < 32 && t < maxt; i++)
                {
                    float3 dummyColor;
                    bool dummyReflective;
                    float dummySmoothness;
                    int dummyMatID;
                    
                    float h = SceneDistance(ro + rd * t, dummyColor, dummyReflective, dummySmoothness, dummyMatID);
                    
                    // Utiliser la technique d'ombrage doux
                    float y = h * h / (2.0 * ph);
                    float d = sqrt(h * h - y * y);
                    result = min(result, k * d / max(0.0, t - y));
                    ph = h;
                    
                    t += h;
                    
                    // Si on est très proche d'un objet, on arrête
                    if (h < 0.001) break;
                }
                
                return clamp(result, 0.0, 1.0);
            }
            
            // Fonction Ray Marching principale
            HitInfo RayMarch(float3 ro, float3 rd)
            {
                HitInfo result;
                result.hit = false;
                result.isReflective = false;
                result.smoothness = 0.5;
                result.materialID = -1;
                
                float totalDistance = 0;
                
                for (int i = 0; i < _MaxSteps; i++)
                {
                    float3 p = ro + rd * totalDistance;
                    float ds = SceneDistance(p, result.color, result.isReflective, result.smoothness, result.materialID);
                    
                    totalDistance += ds;
                    
                    if (ds < _HitDistance)
                    {
                        result.hit = true;
                        result.position = p;
                        result.normal = CalculateNormal(p);
                        
                        // Calculer les coordonnées UV
                        result.uv = CalculateUV(p, result.normal, result.materialID);
                        
                        break;
                    }
                    
                    if (totalDistance > _MaxDistance)
                    {
                        break;
                    }
                }
                
                result.position = ro + rd * totalDistance;
                
                return result;
            }
            
            // Échantillonnage de texture selon l'ID du matériau
            float3 SampleTexture(float2 uv, int materialID)
            {
                if (materialID == 0) // Sol
                {
                    return SAMPLE_TEXTURE2D(_GroundTexture, sampler_GroundTexture, uv).rgb;
                }
                else if (materialID == 1) // Sphère 1
                {
                    return SAMPLE_TEXTURE2D(_Sphere1Texture, sampler_Sphere1Texture, uv).rgb;
                }
                else if (materialID == 2) // Sphère 2
                {
                    return SAMPLE_TEXTURE2D(_Sphere2Texture, sampler_Sphere2Texture, uv).rgb;
                }
                
                return float3(1, 1, 1);
            }
            
            // Calcul de brouillard atmosphérique
            float3 ApplyFog(float3 color, float distance)
            {
                // Facteur d'atténuation exponentielle
                float fogFactor = 1.0 - exp(-distance * _FogDensity);
                return lerp(color, _FogColor.rgb, fogFactor);
            }
            
            // Calcul avancé d'éclairage avec ombres et spéculaire
            float3 Lighting(float3 p, float3 normal, float3 baseColor, float3 viewDir, float smoothness)
            {
                // Direction de lumière directionnelle
                float3 lightDir = normalize(_LightDirection.xyz);
                
                // Calcul d'ombre
                float shadow = CalculateSoftShadow(p + normal * 0.01, lightDir, 0.1, 20.0, _ShadowSoftness);
                shadow = lerp(1.0, shadow, _ShadowIntensity);
                
                // Composante diffuse
                float diffuse = max(dot(normal, lightDir), 0.0) * shadow;
                
                // Composante spéculaire (Blinn-Phong)
                float3 halfwayDir = normalize(lightDir + viewDir);
                float specular = pow(max(dot(normal, halfwayDir), 0.0), _SpecularPower * smoothness);
                specular *= _SpecularIntensity * shadow * smoothness;
                
                // Composante ambiante
                float3 ambient = _AmbientColor.rgb;
                
                // Couleur finale
                float3 finalColor = baseColor * (ambient + diffuse * _LightColor.rgb) + specular * _LightColor.rgb;
                
                return finalColor;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                
                // Calcul du vecteur de vue pour le ray marching
                float4 clipPos = float4(output.uv * 2.0 - 1.0, 0, 1);
                float4 viewPos = mul(unity_CameraInvProjection, clipPos);
                viewPos /= viewPos.w;
                output.viewVectorWS = mul(unity_CameraToWorld, float4(viewPos.xyz, 0)).xyz;
                
                // Position de la caméra
                output.cameraPosWS = _WorldSpaceCameraPos;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float4 originalColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Configuration du ray
                float3 ro = input.cameraPosWS;
                float3 rd = normalize(input.viewVectorWS);
                
                // Premier ray marching
                HitInfo hit = RayMarch(ro, rd);
                
                if (hit.hit)
                {
                    // Appliquer la texture si activée
                    #ifdef _USE_TEXTURES
                    if (hit.materialID >= 0)
                    {
                        float3 textureColor = SampleTexture(hit.uv, hit.materialID);
                        hit.color *= textureColor;
                    }
                    #endif
                    
                    // Calcul de l'éclairage de base
                    float3 color = Lighting(hit.position, hit.normal, hit.color, -rd, hit.smoothness);
                    
                    // Appliquer le brouillard
                    float dist = length(hit.position - ro);
                    color = ApplyFog(color, dist);
                    
                    // Si la surface est réfléchissante, lancer un rayon réfléchi
                    if (hit.isReflective)
                    {
                        // Calcul de la direction réfléchie
                        float3 reflectDir = reflect(rd, hit.normal);
                        
                        // Second ray marching pour la réflexion
                        HitInfo reflectHit = RayMarch(hit.position + hit.normal * 0.01, reflectDir);
                        
                        if (reflectHit.hit)
                        {
                            #ifdef _USE_TEXTURES
                            if (reflectHit.materialID >= 0)
                            {
                                float3 textureColor = SampleTexture(reflectHit.uv, reflectHit.materialID);
                                reflectHit.color *= textureColor;
                            }
                            #endif
                            
                            // Éclairage pour la réflexion
                            float3 reflectColor = Lighting(reflectHit.position, reflectHit.normal, reflectHit.color, -reflectDir, reflectHit.smoothness);
                            
                            // Appliquer le brouillard à la réflexion
                            float reflectDist = length(reflectHit.position - hit.position);
                            reflectColor = ApplyFog(reflectColor, reflectDist);
                            
                            // Mixer la couleur de base avec la couleur réfléchie
                            color = lerp(color, reflectColor, _ReflectionStrength * hit.smoothness);
                        }
                        else
                        {
                            // Si aucun objet n'est touché, utiliser une couleur de "ciel"
                            float3 skyColor = lerp(float3(0.6, 0.8, 0.9), _FogColor.rgb, 0.5);
                            color = lerp(color, skyColor, _ReflectionStrength * hit.smoothness);
                        }
                    }
                    
                    return float4(color, 1);
                }
                else
                {
                    // Couleur du ciel si aucun objet n'est touché
                    float3 skyColor = lerp(float3(0.6, 0.8, 0.9), _FogColor.rgb, 0.3 + 0.7 * rd.y);
                    return float4(skyColor, 1);
                }
            }
            ENDHLSL
        }
    }
} 