using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Struct to have a Bound not aligned with scene axis, but with an additional rotation
/// Under the hood, manipulate a regular Bound, that is relative to the referential create by the initialCenter and rotation.
/// </summary>
public struct OrientedBounds
{
    Bounds bounds;
    Quaternion rotation;
    Matrix4x4 referenceTransformMatrix;
    Vector3 initialSize;

    /// <summary>
    /// Center of the oriented bound
    /// </summary>
    public Vector3 Center => referenceTransformMatrix.MultiplyPoint(bounds.center);

    /// <summary>
    /// Rotation of the oriented bound
    /// </summary>
    public Quaternion Rotation => _rotationInitialized ? rotation : Quaternion.identity;

    /// <summary>
    /// The extents of the Bounding Box.This is always half of the size of the Bounds.
    /// </summary>
    public Vector3 Extents => bounds.extents;

    /// <summary>
    /// Size of the bound (twice the extents)
    /// </summary>
    public Vector3 Size => bounds.extents * 2f;

    bool _matrixInitialized;
    bool _rotationInitialized;

    /// <summary>
    /// Create an oriented bound with base center, rotation, and starting size (aka twice the extent stored)
    /// </summary>
    public OrientedBounds(Vector3 initialCenter, Quaternion rotation, Vector3 initialSize)
    {
        this.rotation = rotation;
        this.initialSize = initialSize;
        referenceTransformMatrix = Matrix4x4.TRS(initialCenter, rotation, Vector3.one);

        bounds = new Bounds(Vector3.zero, initialSize);
        _matrixInitialized = true;
        _rotationInitialized = true;
    }

    /// <summary>
    /// Create an oriented bound with base center, rotation, and default starting size (aka twice the extent stored) of 0.05f * Vector3.one 
    /// </summary>
    public OrientedBounds(Quaternion rotation) : this(rotation, 0.05f * Vector3.one) {}

    /// <summary>
    /// Create an oriented bound with Vector3.zero center, rotation, and default starting size (aka twice the extent stored) 
    /// </summary>
    public OrientedBounds(Quaternion rotation, Vector3 initialSize = default)
    {
        this.rotation = rotation;
        this.initialSize = initialSize;
        bounds = new Bounds(Vector3.zero, initialSize);
        referenceTransformMatrix = default;
        _matrixInitialized = false;
        _rotationInitialized = true;
    }

    /// <summary>
    /// Add a point to be included in the oriented bound
    /// Optionally, can ignore the contribution on this point on a local axis of the resulting bound
    /// </summary>
    public void Encapsulate(Vector3 point, bool ignoreXAxis = false, bool ignoreYAxis = false, bool ignoreZAxis = false)
    {
        if (_matrixInitialized == false)
        {
            referenceTransformMatrix = Matrix4x4.TRS(point, Rotation, Vector3.one);
            _matrixInitialized = true;
        }
        var offset = referenceTransformMatrix.inverse.MultiplyPoint(point);

        if (ignoreXAxis) offset.x = 0;
        if (ignoreYAxis) offset.y = 0;
        if (ignoreZAxis) offset.z = 0;
        bounds.Encapsulate(offset);
    }

    /// <summary>
    /// Expand the bounds by increasing its size by amount along each side.
    /// </summary>
    public void Expand(float amount)
    {
        bounds.Expand(amount);
    }

    /// <summary>
    /// Expand the bounds by increasing its extents (half of the size)
    /// </summary>
    public void IncreaseExtents(Vector3 extentsIncrease)
    {
        bounds.extents = new Vector3(bounds.extents.x + extentsIncrease.x, bounds.extents.y + extentsIncrease.y, bounds.extents.z + extentsIncrease.z);
    }

    /// <summary>
    /// Move a transform to Center/Rotation, and changes its scale to Extents * 2
    /// </summary>
    public void ApplyToTransform(Transform transform, bool ignoreParentScale = true)
    {
        if (transform == null) return;
        var centerPosition = transform.position;
        if(_matrixInitialized)
        {
            centerPosition = Center;
        }
        transform.position = centerPosition;
        transform.rotation = Rotation;
        if (ignoreParentScale || transform.parent == null)
        {
            transform.localScale = bounds.extents * 2;
        }
        else
        {
            var parentLossyScale = transform.parent.lossyScale;
            var worldScale = bounds.extents * 2;
            transform.localScale = new Vector3(worldScale.x / parentLossyScale.x, worldScale.y / parentLossyScale.y, worldScale.z / parentLossyScale.z);
        }
    }
}
