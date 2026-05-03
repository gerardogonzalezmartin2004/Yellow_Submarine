using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Renderer Feature de efecto submarino para Unity 6 URP.
/// Requiere Compatibility Mode activo en el URP Asset.
/// </summary>
public class Underwater : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material material;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Color color = new Color(0f, 0.4f, 0.6f, 1f);

        [Tooltip("Cuanto mas alto, mas oscuro/azul se ve el fondo")]
        public float FogDensity = 5f;

        [Range(0f, 1f)]
        [Tooltip("Transparencia base del color del agua")]
        public float alpha = 0.3f;

        [Tooltip("Intensidad del efecto de olas en pantalla")]
        public float refraction = 0.5f;

        public Texture normalmap;
        public Vector4 UV = new Vector4(1f, 1f, 0.2f, 0.1f);

        [Tooltip("Y por debajo del cual la camara se considera bajo el agua")]
        public float waterLevel = 0f;
    }

    public Settings settings = new Settings();

    // -------------------------------------------------------------------------
    class Pass : ScriptableRenderPass
    {
        public Settings settings;
        private RTHandle source;
        private RTHandle tempTexture;
        private readonly string profilerTag;

        public Pass(string profilerTag)
        {
            this.profilerTag = profilerTag;
        }

        public void Setup(RTHandle sourceHandle)
        {
            source = sourceHandle;
        }

#pragma warning disable CS0618, CS0672
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0; // Textura temporal solo de color
            RenderingUtils.ReAllocateIfNeeded(
                ref tempTexture, desc,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_TempUnderwaterTex"
            );
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings.material == null || source == null) return;

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            cmd.Clear();

            // Enviamos todos los parametros al shader
            settings.material.SetFloat("_FogDensity", settings.FogDensity);
            settings.material.SetFloat("_alpha", settings.alpha);
            settings.material.SetColor("_color", settings.color);
            settings.material.SetFloat("_refraction", settings.refraction);
            settings.material.SetVector("_normalUV", settings.UV);

            if (settings.normalmap != null)
                settings.material.SetTexture("_NormalMap", settings.normalmap);

            // Blitter es la forma correcta de hacer blit en Unity 6 URP
            // Paso 1: copia la camara a la textura temporal
            Blitter.BlitCameraTexture(cmd, source, tempTexture);
            // Paso 2: aplica el material y copia de vuelta a la camara
            Blitter.BlitCameraTexture(cmd, tempTexture, source, settings.material, 0);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
#pragma warning restore CS0618, CS0672

        public void Dispose()
        {
            tempTexture?.Release();
        }
    }

    // -------------------------------------------------------------------------
    Pass pass;

    public override void Create()
    {
        pass = new Pass("Underwater Effects")
        {
            settings = settings,
            renderPassEvent = settings.renderPassEvent
        };
    }

#pragma warning disable CS0618, CS0672
    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        // Le pasamos el handle del color de camara al pass
        pass.Setup(renderer.cameraColorTargetHandle);
    }
#pragma warning restore CS0618, CS0672

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Ignoramos camaras que no sean la principal del juego (evita problemas con Cinemachine)
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        if (settings.material == null)
        {
            Debug.LogWarning("[Underwater] No hay material asignado en el Renderer Feature.");
            return;
        }

        // Solo activamos el efecto si la camara esta por debajo del nivel del agua
        Camera cam = renderingData.cameraData.camera;
        if (cam != null && cam.transform.position.y < settings.waterLevel)
        {
            renderer.EnqueuePass(pass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
    }
}