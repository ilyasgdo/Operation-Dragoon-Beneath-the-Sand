using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

public class RayTracingRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class RayTracingSettings
    {
        public Material rayTracingMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public RayTracingSettings settings = new RayTracingSettings();
    private RayTracingRenderPass rayTracingPass;

    class RayTracingRenderPass : ScriptableRenderPass
    {
        private Material rayTracingMaterial;
        private RTHandle colorTarget;
        private int tempTextureId;
        private RTHandle tempTexture;
        private string profilerTag;
        private RenderTextureDescriptor tempDescriptor;

        public RayTracingRenderPass(string tag)
        {
            profilerTag = tag;
            tempTextureId = Shader.PropertyToID("_TempRayTracingTexture");
        }

        public void Setup(Material material, RTHandle target)
        {
            rayTracingMaterial = material;
            colorTarget = target;
        }

        [Obsolete("Utiliser Configure à la place.")]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            tempDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            tempDescriptor.depthBufferBits = 0;
            
            // Création de la texture temporaire
            RenderingUtils.ReAllocateIfNeeded(ref tempTexture, tempDescriptor, name: "_TempRayTracingTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (rayTracingMaterial == null)
            {
                Debug.LogError("Ray Tracing material est manquant!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            // Effectue le ray tracing dans une texture temporaire
            Blitter.BlitCameraTexture(cmd, colorTarget, tempTexture, rayTracingMaterial, 0);
            
            // Puis restitue le résultat dans la cible originale
            Blitter.BlitCameraTexture(cmd, tempTexture, colorTarget, rayTracingMaterial, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            tempTexture?.Release();
        }
    }

    public override void Create()
    {
        rayTracingPass = new RayTracingRenderPass("Ray Tracing Pass");
        rayTracingPass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.rayTracingMaterial == null)
        {
            Debug.LogWarning("Le matériau de Ray Tracing est manquant. Assurez-vous de l'assigner dans l'inspecteur.");
            return;
        }

        // Récupérer la cible de rendu correctement avec la nouvelle API
        rayTracingPass.Setup(settings.rayTracingMaterial, renderer.cameraColorTargetHandle);
        renderer.EnqueuePass(rayTracingPass);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // Nettoyage des ressources allouées
    }
} 