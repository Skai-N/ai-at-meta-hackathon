using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Meta.XR;
using Random = UnityEngine.Random;

using Meta.XR.BuildingBlocks.AIBlocks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Meta.XR;
using System;

/// <summary>
/// Spawns exactly one arrow aligned to the ground, pointing along a given 2D (XZ) direction.
/// Every call overwrites the previous arrow (destroy & respawn).
/// </summary>
public class Arrow : MonoBehaviour
{
    [Header("Ground & Placement")]
    public Vector3 groundNormal = Vector3.up;
    public float heightOffset = 0.02f;       // Small lift to avoid z-fighting

    [Header("Arrow")]
    public GameObject arrowPrefab;           // Prefab with +Z pointing to the tip

    private GameObject _arrowInstance;

    private Camera _cam;

    private void Awake()
    {   
        _cam = Camera.main;
    }

    /// <summary>
    /// Draw a new arrow that points along direction2D (x=right, y=forward) in world XZ.
    /// Destroys any previously spawned arrow instance.
    /// </summary>
    public void Draw(Vector2 direction2D)
    {
        if (!arrowPrefab || !_cam) return;

        Debug.LogWarning(direction2D.ToString());

        // Destroy old arrow
        if (_arrowInstance)
        {
            Destroy(_arrowInstance);
            _arrowInstance = null;
        }

        // 2D -> 3D world-space vector (XZ)
        Vector3 dirWorld = new Vector3(direction2D.x, 0f, direction2D.y);

        // Project onto the ground plane and normalize
        Vector3 planar = Vector3.ProjectOnPlane(dirWorld, groundNormal);

        if (planar.sqrMagnitude < 1e-8f)
            planar = Vector3.forward;
        planar.Normalize();

        var front = _cam.transform.position + _cam.transform.forward * 2;

        // Rotation aligned to surface
        Quaternion rot = Quaternion.LookRotation(planar, groundNormal);

        // Position slightly above ground
        Vector3 pos = front + groundNormal * heightOffset;

        // Spawn fresh arrow
        _arrowInstance = Instantiate(arrowPrefab, pos, rot);
    }

    private void OnDestroy()
    {
        if (_arrowInstance) Destroy(_arrowInstance);
    }
}
