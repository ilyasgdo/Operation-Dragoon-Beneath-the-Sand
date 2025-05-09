Shader "Custom/SimpleWaterShaderURP"
{
    Properties
    {
        _Color ("Couleur", Color) = (0.2, 0.5, 0.7, 0.8)
        _MainTex ("Texture principale", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _DistortionScale ("Échelle de distorsion", Range(0, 0.5)) = 0.1
        _WaveSpeed ("Vitesse des vagues", Range(0, 5)) = 1
        _WaveScale ("Échelle des vagues", Range(0, 10)) = 1
        _ReflectionStrength ("Force de réflexion", Range(0, 1)) = 0.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.8
        _DepthFactor ("Facteur de profondeur", Range(0, 5)) = 1
    }
    
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        
        Pass
        {
            Name "Universal Forward"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 viewDir : TEXCOORD4;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NormalMap_ST;
                float4 _Color;
                float _DistortionScale;
                float _WaveSpeed;
                float _WaveScale;
                float _ReflectionStrength;
                float _Smoothness;
                float _DepthFactor;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Positions
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                // Normals
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                
                // UVs
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                // View direction
                output.viewDir = GetWorldSpaceViewDir(output.positionWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Animation des vagues
                float2 uv1 = input.uv * _WaveScale;
                float2 uv2 = input.uv * _WaveScale * 0.5;
                float t = _Time.y * _WaveSpeed;
                
                uv1 += float2(t * 0.1, t * 0.2);
                uv2 += float2(-t * 0.2, t * 0.1);
                
                // Normal maps
                float3 normalTS1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1));
                float3 normalTS2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2));
                float3 normalTS = normalize(normalTS1 + normalTS2);
                
                // Transformer la normale tangent space en world space
                float3 binormalWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tangentToWorld = float3x3(input.tangentWS.xyz, binormalWS, input.normalWS);
                float3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
                
                // Distorsion
                float2 distortion = normalTS.xy * _DistortionScale;
                float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + distortion);
                
                // Calcul du facteur de Fresnel
                float3 viewDir = normalize(input.viewDir);
                float fresnel = pow(1.0 - saturate(dot(normalize(normalWS), viewDir)), 5.0);
                
                // Éclairage
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalize(normalWS);
                lightingInput.viewDirectionWS = viewDir;
                lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                
                // Couleur finale
                float4 finalColor = _Color * albedo;
                finalColor.rgb += fresnel * _ReflectionStrength;
                
                // Propriétés de surface
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalColor.rgb;
                surfaceData.alpha = _Color.a;
                surfaceData.specular = float3(0.0, 0.0, 0.0);
                surfaceData.smoothness = _Smoothness;
                
                // Appliquer l'éclairage
                half4 color = UniversalFragmentPBR(lightingInput, surfaceData);
                color.a = _Color.a;
                
                return color;
            }
            ENDHLSL
        }
        
        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            
            // Déclarations requises pour les mêmes propriétés
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NormalMap_ST;
                float4 _Color;
                float _DistortionScale;
                float _WaveSpeed;
                float _WaveScale;
                float _ReflectionStrength;
                float _Smoothness;
                float _DepthFactor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, 0));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            
            ENDHLSL
        }
        
        // DepthOnly pass
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Déclarations requises pour les mêmes propriétés
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NormalMap_ST;
                float4 _Color;
                float _DistortionScale;
                float _WaveSpeed;
                float _WaveScale;
                float _ReflectionStrength;
                float _Smoothness;
                float _DepthFactor;
            CBUFFER_END

            struct Attributes
            {
                float4 position     : POSITION;
                float2 texcoord     : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            
            ENDHLSL
        }
    }
} 