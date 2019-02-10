using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

public class EdgeRT : MonoBehaviour, IAfterDepthPrePass
{
    public const int k_PerObjectBlurRenderLayerIndex = 9;
    
    private EdgeRTPassImpl edgeRTPass;

    public ScriptableRenderPass GetPassToEnqueue(RenderTextureDescriptor baseDescriptor, RenderTargetHandle depthAttachmentHandle)
    {
         if (edgeRTPass == null) edgeRTPass = new EdgeRTPassImpl(baseDescriptor);
        return edgeRTPass;
    }
}

public class EdgeRTPassImpl : ScriptableRenderPass 
{
    private const string k_PerObjectBloomTag = "Per Object Bloom";

    private Material m_brightnessMaskMaterial;

    private RenderTextureDescriptor m_baseDescriptor;
    private RenderTargetHandle m_PerObjectRenderTextureHandle;
    private FilterRenderersSettings m_PerObjectFilterSettings;

    public EdgeRTPassImpl(RenderTextureDescriptor baseDescriptor)
    {
        // All shaders with this lightmode will be in this pass
        RegisterShaderPassName("LightweightForward");

        m_baseDescriptor = baseDescriptor;

        // This just writes black values for anything that is rendered
        m_brightnessMaskMaterial = CoreUtils.CreateEngineMaterial("Hidden/Internal-DepthNormalsTexture");

        // Setup a target RT handle (it just wraps the int id)
        m_PerObjectRenderTextureHandle = new RenderTargetHandle();
        m_PerObjectRenderTextureHandle.Init(k_PerObjectBloomTag);

        m_PerObjectFilterSettings = new FilterRenderersSettings(true)
        {
            // Render all opaque objects
            renderQueueRange = RenderQueueRange.opaque,
            // Filter further by any renderer tagged as per-object blur
            layerMask = 1<<EdgeRT.k_PerObjectBlurRenderLayerIndex
        };
    }

    public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get(k_PerObjectBloomTag);
        using (new ProfilingSample(cmd, k_PerObjectBloomTag))
        {
            var desc = m_baseDescriptor;
            desc.colorFormat = RenderTextureFormat.ARGB32;

            cmd.GetTemporaryRT(m_PerObjectRenderTextureHandle.id, desc);
            SetRenderTarget(
                cmd,
                m_PerObjectRenderTextureHandle.Identifier(),
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.DontCare,
                ClearFlag.All,
                Color.white, // Clear to white, the stencil writes black values
                m_baseDescriptor.dimension // Create a buffer the same size as the color buffer
                );

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var camera = renderingData.cameraData.camera;

            // We want the same rendering result as the main opaque render
            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;

            // Setup render data from camera
            var drawSettings = CreateDrawRendererSettings(camera, sortFlags, RendererConfiguration.None, 
                renderingData.supportsDynamicBatching);

            // Everything gets drawn with the stencil shader
            drawSettings.SetOverrideMaterial(m_brightnessMaskMaterial, 0);

            context.DrawRenderers(renderingData.cullResults.visibleRenderers, ref drawSettings, m_PerObjectFilterSettings);

            // Set a global texture id so we can access this later on
            cmd.SetGlobalTexture("_EdgeRTDepth", m_PerObjectRenderTextureHandle.id);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        base.FrameCleanup(cmd);

        // When rendering is done, clean up our temp RT
        cmd.ReleaseTemporaryRT(m_PerObjectRenderTextureHandle.id);
    }
}
