using UnityEngine;

/// <summary>
/// the ambient occlusion tutorial asset.
/// </summary>
[CreateAssetMenu(fileName = "AmbientOcclusionTutorialAsset", menuName = "Rendering/AmbientOcclusionTutorialAsset")]
public class AmbientOcclusionTutorialAsset : RayTracingTutorialAsset
{
  /// <summary>
  /// the blit material.
  /// </summary>
  public Material blitMaterial;

  /// <summary>
  /// the denoiser shader.
  /// </summary>
  public ComputeShader denoiserShader;

  /// <summary>
  /// the blue noise texture.
  /// </summary>
  public Texture2D blueNoiseTexture;

  /// <summary>
  /// create tutorial.
  /// </summary>
  /// <returns>the tutorial.</returns>
  public override RayTracingTutorial CreateTutorial()
  {
    return new AmbientOcclusionTutorial(this);
  }
}
