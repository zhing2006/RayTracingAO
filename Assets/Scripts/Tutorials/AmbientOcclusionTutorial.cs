using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

/// <summary>
/// the ambient occlusion tutorial.
/// </summary>
public class AmbientOcclusionTutorial : RayTracingTutorial
{
  private readonly int _NoiseTextureShaderId = Shader.PropertyToID("_NoiseTexture");
  private readonly int _GBufferShaderId = Shader.PropertyToID("_GBuffer");
  private readonly int _DenoiseInputShaderId = Shader.PropertyToID("_DenoiseInput");
  private readonly int _DenoiseOutputRWShaderId = Shader.PropertyToID("_DenoiseOutputRW");
  private readonly int _DenoiserFilterRadiusShaderId = Shader.PropertyToID("_DenoiserFilterRadius");
  private readonly int _ViewForwardDirShaderId = Shader.PropertyToID("_ViewForwardDir");

  /// <summary>
  /// the G buffer[normal, depth]
  /// </summary>
  private RTHandle _GBuffer;
  /// <summary>
  /// the intermediate buffer.
  /// </summary>
  private RTHandle _intermediateBuffer;
  /// <summary>
  /// the ambient occlusion texture.
  /// </summary>
  private RTHandle _AOTexture;

  private int _BilateralFilterKernelId;
  private int _GatherKernelId;

  /// <summary>
  /// the noise texture.
  /// </summary>
  private Texture2D _noiseTexture;
  /// <summary>
  /// the final blit material.
  /// </summary>
  private Material _finalBlitMat;

  /// <summary>
  /// constructor.
  /// </summary>
  /// <param name="asset">the tutorial asset.</param>
  public AmbientOcclusionTutorial(RayTracingTutorialAsset asset) : base(asset)
  {
    _noiseTexture = (_asset as AmbientOcclusionTutorialAsset).blueNoiseTexture;
    _finalBlitMat = (_asset as AmbientOcclusionTutorialAsset).blitMaterial;
  }

  /// <summary>
  /// dispose.
  /// </summary>
  /// <param name="disposing">whether is disposing.</param>
  public override void Dispose(bool disposing)
  {
    base.Dispose(disposing);

    if (null != _GBuffer)
    {
      RTHandles.Release(_GBuffer);
      _GBuffer = null;
    }

    if (null != _intermediateBuffer)
    {
      RTHandles.Release(_intermediateBuffer);
      _intermediateBuffer = null;
    }

    if (null != _AOTexture)
    {
      RTHandles.Release(_AOTexture);
      _AOTexture = null;
    }
  }

  /// <summary>
  /// render.
  /// </summary>
  /// <param name="context">the render context.</param>
  /// <param name="camera">the camera.</param>
  public override void Render(ScriptableRenderContext context, Camera camera)
  {
    // Only render game view.
    if (camera.cameraType != CameraType.Game)
      return;

    base.Render(context, camera);

    var projectionMatrix = Matrix4x4.Perspective(camera.fieldOfView, camera.aspect,
      camera.nearClipPlane, camera.farClipPlane);

    var outputTarget = RequireOutputTarget(camera, GraphicsFormat.R16_SFloat);
    var outputDepth = RequireOutputDepth(camera);
    var outputTargetSize = RequireOutputTargetSize(camera);
    var gBuffer = RequireGBuffer(camera);
    var intermediateBuffer = RequireIntermediateBuffer(camera);
    var AOTexture = RequireAOTexture(camera);
    var denoiser = (_asset as AmbientOcclusionTutorialAsset).denoiserShader;
    _BilateralFilterKernelId = denoiser.FindKernel("BilateralFilter");
    _GatherKernelId = denoiser.FindKernel("Gather");

    var accelerationStructure = _pipeline.RequestAccelerationStructure();

    var cmd = CommandBufferPool.Get(typeof(AmbientOcclusionTutorial).Name);
    try
    {
      using (new ProfilingSample(cmd, "GBuffer"))
      {
        cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, projectionMatrix);
        cmd.SetRenderTarget(gBuffer, outputDepth);
        cmd.ClearRenderTarget(true, false, Color.black);
        foreach (var renderer in SceneManager.Instance.renderers)
        {
          for (var subMeshIndex = 0; subMeshIndex < renderer.sharedMaterials.Length; ++subMeshIndex)
            cmd.DrawRenderer(renderer, renderer.sharedMaterial, subMeshIndex, renderer.sharedMaterial.FindPass("GBuffer"));
        }
      }

      context.ExecuteCommandBuffer(cmd);
      cmd.Clear();

      using (new ProfilingSample(cmd, "RayTracing"))
      {
        cmd.SetRayTracingShaderPass(_shader, "RayTracing");
        cmd.SetRayTracingAccelerationStructure(_shader, _pipeline.accelerationStructureShaderId, accelerationStructure);
        cmd.SetRayTracingTextureParam(_shader, _NoiseTextureShaderId, _noiseTexture);
        cmd.SetRayTracingTextureParam(_shader, _GBufferShaderId, gBuffer);
        cmd.SetRayTracingTextureParam(_shader, _outputTargetShaderId, outputTarget);
        cmd.SetRayTracingVectorParam(_shader, _outputTargetSizeShaderId, outputTargetSize);
        cmd.DispatchRays(_shader, "AmbientOcclusionGenShader", (uint) outputTarget.rt.width,
          (uint) outputTarget.rt.height, 1, camera);
      }

      context.ExecuteCommandBuffer(cmd);
      cmd.Clear();

      using (new ProfilingSample(cmd, "Denoise"))
      {
        const int areaTileSize = 8;
        var numTilesX = (intermediateBuffer.rt.width + (areaTileSize - 1)) / areaTileSize;
        var numTilesY = (intermediateBuffer.rt.height + (areaTileSize - 1)) / areaTileSize;
        cmd.SetComputeTextureParam(denoiser, _BilateralFilterKernelId, _NoiseTextureShaderId, _noiseTexture);
        cmd.SetComputeTextureParam(denoiser, _BilateralFilterKernelId, _GBufferShaderId, gBuffer);
        cmd.SetComputeTextureParam(denoiser, _BilateralFilterKernelId, _DenoiseInputShaderId, outputTarget);
        cmd.SetComputeVectorParam(denoiser, _outputTargetSizeShaderId, outputTargetSize);
        cmd.SetComputeTextureParam(denoiser, _BilateralFilterKernelId, _DenoiseOutputRWShaderId, intermediateBuffer);
        cmd.SetComputeFloatParam(denoiser, _DenoiserFilterRadiusShaderId, 0.2f);
        cmd.DispatchCompute(denoiser, _BilateralFilterKernelId, numTilesX, numTilesY, 1);

        numTilesX = (AOTexture.rt.width + (areaTileSize - 1)) / areaTileSize;
        numTilesY = (AOTexture.rt.height + (areaTileSize - 1)) / areaTileSize;
        cmd.SetComputeTextureParam(denoiser, _GatherKernelId, _DenoiseInputShaderId, intermediateBuffer);
        cmd.SetComputeTextureParam(denoiser, _GatherKernelId, _DenoiseOutputRWShaderId, AOTexture);
        cmd.DispatchCompute(denoiser, _GatherKernelId, numTilesX, numTilesY, 1);
      }

      context.ExecuteCommandBuffer(cmd);
      cmd.Clear();

      using (new ProfilingSample(cmd, "FinalBlit"))
      {
        cmd.SetGlobalTexture(_GBufferShaderId, gBuffer);
        cmd.Blit(AOTexture, BuiltinRenderTextureType.CameraTarget, _finalBlitMat);
        //cmd.Blit(AOTexture, BuiltinRenderTextureType.CameraTarget);
      }

      context.ExecuteCommandBuffer(cmd);
    }
    finally
    {
      CommandBufferPool.Release(cmd);
    }
  }

  /// <summary>
  /// require G buffer.
  /// </summary>
  /// <param name="camera">the camera.</param>
  /// <returns>the G buffer.</returns>
  private RTHandle RequireGBuffer(Camera camera)
  {
    if (null != _GBuffer)
      return _GBuffer;

    _GBuffer = RTHandles.Alloc(
      camera.pixelWidth,
      camera.pixelHeight,
      1,
      DepthBits.None,
      GraphicsFormat.R32G32B32A32_SFloat,
      FilterMode.Point,
      TextureWrapMode.Clamp,
      TextureDimension.Tex2D,
      false,
      false,
      false,
      false,
      1,
      0f,
      MSAASamples.None,
      false,
      false,
      RenderTextureMemoryless.None,
      $"NormalDepthBuffer_{camera.name}");

    return _GBuffer;
  }

  /// <summary>
  /// require ambient occlusion texture.
  /// </summary>
  /// <param name="camera">the camera.</param>
  /// <returns>the ambient occlusion texture.</returns>
  private RTHandle RequireAOTexture(Camera camera)
  {
    if (null != _AOTexture)
      return _AOTexture;

    _AOTexture = RTHandles.Alloc(
      camera.pixelWidth / 2,
      camera.pixelHeight / 2,
      1,
      DepthBits.None,
      GraphicsFormat.R16_SFloat,
      FilterMode.Bilinear,
      TextureWrapMode.Clamp,
      TextureDimension.Tex2D,
      true,
      false,
      false,
      false,
      1,
      0f,
      MSAASamples.None,
      false,
      false,
      RenderTextureMemoryless.None,
      $"AOTexture_{camera.name}");

    return _AOTexture;
  }

  /// <summary>
  /// require intermediate buffer.
  /// </summary>
  /// <param name="camera">the camera.</param>
  /// <returns>the intermediate buffer.</returns>
  private RTHandle RequireIntermediateBuffer(Camera camera)
  {
    if (null != _intermediateBuffer)
      return _intermediateBuffer;

    _intermediateBuffer = RTHandles.Alloc(
      camera.pixelWidth,
      camera.pixelHeight,
      1,
      DepthBits.None,
      GraphicsFormat.R16_SFloat,
      FilterMode.Bilinear,
      TextureWrapMode.Clamp,
      TextureDimension.Tex2D,
      true,
      false,
      false,
      false,
      1,
      0f,
      MSAASamples.None,
      false,
      false,
      RenderTextureMemoryless.None,
      $"IntermediateBuffer_{camera.name}");

    return _intermediateBuffer;
  }
}
