using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Set material helper.
/// </summary>
public static class SetMaterialToAO
{
  /// <summary>
  /// set material to ambient occlusion.
  /// </summary>
  [MenuItem("AO/Set Material")]
  public static void SetMaterial()
  {
    var aoMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/AmbientOcclusion.mat");

    WalkThroughTransformTree(Selection.activeTransform, tr =>
    {
      var r = tr.GetComponent<Renderer>();
      if (!r) return;
      var mats = new Material[r.sharedMaterials.Length];
      for (var i = 0; i < r.sharedMaterials.Length; ++i)
      {
        mats[i] = aoMat;
      }
      r.sharedMaterials = mats;
    });
  }

  /// <summary>
  /// walk through the transform tree.
  /// </summary>
  /// <param name="parentTr">the parent transform.</param>
  /// <param name="action">the action.</param>
  private static void WalkThroughTransformTree(Transform parentTr, System.Action<Transform> action)
  {
    if (!parentTr.gameObject.activeInHierarchy)
      return;

    foreach (Transform childTr in parentTr)
    {
      WalkThroughTransformTree(childTr, action);
    }

    action(parentTr);
  }
}
