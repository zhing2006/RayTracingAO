using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

/// <summary>
/// the ray tracing render pipeline.
/// </summary>
public class RayTracingRenderPipeline : RenderPipeline
{
  /// <summary>
  /// the render pipeline asset.
  /// </summary>
  private RayTracingRenderPipelineAsset _asset;
  /// <summary>
  /// the ray tracing acceleration structure.
  /// </summary>
  private RayTracingAccelerationStructure _accelerationStructure;

  public readonly int accelerationStructureShaderId = Shader.PropertyToID("_AccelerationStructure");
  public readonly int _SHArShaderId = Shader.PropertyToID("_SHAr");
  public readonly int _SHAgShaderId = Shader.PropertyToID("_SHAg");
  public readonly int _SHAbShaderId = Shader.PropertyToID("_SHAb");
  public readonly int _SHBrShaderId = Shader.PropertyToID("_SHBr");
  public readonly int _SHBgShaderId = Shader.PropertyToID("_SHBg");
  public readonly int _SHBbShaderId = Shader.PropertyToID("_SHBb");
  public readonly int _SHCShaderId  = Shader.PropertyToID("_SHC");
  public readonly int _MainLightDirShaderId = Shader.PropertyToID("_MainLightDir");

  /// <summary>
  /// the tutorial.
  /// </summary>
  private RayTracingTutorial _tutorial;

  /// <summary>
  /// constructor.
  /// </summary>
  /// <param name="asset">the render pipeline asset.</param>
  public RayTracingRenderPipeline(RayTracingRenderPipelineAsset asset)
  {
    _asset = asset;
    _accelerationStructure = new RayTracingAccelerationStructure();

    _tutorial = _asset.tutorialAsset.CreateTutorial();
    if (_tutorial == null)
    {
      Debug.LogError("Can't create tutorial.");
      return;
    }

    if (_tutorial.Init(this) == false)
    {
      _tutorial = null;
      Debug.LogError("Initialize tutorial failed.");
      return;
    }
  }

  /// <summary>
  /// require the ray tracing acceleration structure.
  /// </summary>
  /// <returns>the ray tracing acceleration structure.</returns>
  public RayTracingAccelerationStructure RequestAccelerationStructure()
  {
    return _accelerationStructure;
  }

  /// <summary>
  /// render.
  /// </summary>
  /// <param name="context">the render context.</param>
  /// <param name="cameras">the all cameras.</param>
  protected override void Render(ScriptableRenderContext context, Camera[] cameras)
  {
    if (!SystemInfo.supportsRayTracing)
    {
      Debug.LogError("You system is not support ray tracing. Please check your graphic API is D3D12 and os is Windows 10.");
      return;
    }

    BeginFrameRendering(context, cameras);

    System.Array.Sort(cameras, (lhs, rhs) => (int)(lhs.depth - rhs.depth));

    BuildAccelerationStructure();
    SetupSHCoefficients();
    Shader.SetGlobalVector(_MainLightDirShaderId, -SceneManager.Instance.mainLight.transform.forward);

    foreach (var camera in cameras)
    {
      // Only render game and scene view camera.
      if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)
        continue;

      BeginCameraRendering(context, camera);
      _tutorial?.Render(context, camera);
      context.Submit();
      EndCameraRendering(context, camera);
    }

    EndFrameRendering(context, cameras);
  }

  /// <summary>
  /// setup SH coefficients.
  /// </summary>
  private void SetupSHCoefficients()
  {
    var coefficients = new Vector4[7];
    for (var ch = 0; ch < 3; ++ch)
    {
      coefficients[ch].x = RenderSettings.ambientProbe[ch, 3];
      coefficients[ch].y = RenderSettings.ambientProbe[ch, 1];
      coefficients[ch].z = RenderSettings.ambientProbe[ch, 2];
      coefficients[ch].w = RenderSettings.ambientProbe[ch, 0] - RenderSettings.ambientProbe[ch, 6];
      coefficients[ch + 3].x = RenderSettings.ambientProbe[ch, 4];
      coefficients[ch + 3].y = RenderSettings.ambientProbe[ch, 5];
      coefficients[ch + 3].z = RenderSettings.ambientProbe[ch, 6] * 3.0f;
      coefficients[ch + 3].w = RenderSettings.ambientProbe[ch, 7];
    }

    coefficients[6].x = RenderSettings.ambientProbe[0, 8];
    coefficients[6].y = RenderSettings.ambientProbe[1, 8];
    coefficients[6].z = RenderSettings.ambientProbe[2, 8];
    coefficients[6].w = 1.0f;
    var shaderIds = new[]
    {
      _SHArShaderId,
      _SHAgShaderId,
      _SHAbShaderId,
      _SHBrShaderId,
      _SHBgShaderId,
      _SHBbShaderId,
      _SHCShaderId,
    };
    for (var i = 0; i < shaderIds.Length; ++i)
      Shader.SetGlobalVector(shaderIds[i], coefficients[i]);
  }

  /// <summary>
  /// dispose.
  /// </summary>
  /// <param name="disposing">whether is disposing.</param>
  protected override void Dispose(bool disposing)
  {
    if (null != _tutorial)
    {
      _tutorial.Dispose(disposing);
      _tutorial = null;
    }

    if (null != _accelerationStructure)
    {
      _accelerationStructure.Dispose();
      _accelerationStructure = null;
    }
  }

  /// <summary>
  /// build the ray tracing acceleration structure.
  /// </summary>
  private void BuildAccelerationStructure()
  {
    if (SceneManager.Instance == null || !SceneManager.Instance.isDirty) return;

    _accelerationStructure.Dispose();
    _accelerationStructure = new RayTracingAccelerationStructure();

    SceneManager.Instance.FillAccelerationStructure(ref _accelerationStructure);

    _accelerationStructure.Build();

    SceneManager.Instance.isDirty = false;
  }
}
