using UnityEngine;

public struct OrientedBounds
{
    Bounds bounds;
    Quaternion rotation;
    Matrix4x4 referenceTransformMatrix;
    Vector3 initialExtends;

    public Vector3 Center => referenceTransformMatrix.MultiplyPoint(bounds.center);
    public Quaternion Rotation => rotation;
    public Vector3 Extends => bounds.extents;

    bool matrixInitialized;

    public OrientedBounds(Vector3 initialCenter, Quaternion rotation, Vector3 initialExtends)
    {
        this.rotation = rotation;
        this.initialExtends = initialExtends;
        referenceTransformMatrix = Matrix4x4.TRS(initialCenter, rotation, Vector3.one);

        bounds = new Bounds(Vector3.zero, initialExtends);
        matrixInitialized = true;
    }

    public OrientedBounds(Quaternion rotation, Vector3 initialExtends)
    {
        this.rotation = rotation;
        this.initialExtends = initialExtends;
        bounds = new Bounds(Vector3.zero, initialExtends);
        referenceTransformMatrix = default;
        matrixInitialized = false;
    }

    public void Encapsulate(Vector3 point, bool ignoreXAxis = false, bool ignoreYAxis = false, bool ignoreZAxis = false)
    {
        if (matrixInitialized == false)
        {
            referenceTransformMatrix = Matrix4x4.TRS(point, rotation, Vector3.one);
            matrixInitialized = true;
        }
        var offset = referenceTransformMatrix.inverse.MultiplyPoint(point);

        if (ignoreXAxis) offset.x = 0;
        if (ignoreYAxis) offset.y = 0;
        if (ignoreZAxis) offset.z = 0;
        bounds.Encapsulate(offset);
    }

    public void Expand(float amount)
    {
        bounds.Expand(amount);
    }

    public void Expand(Vector3 sideIncreaze)
    {
        bounds.extents = new Vector3(bounds.extents.x + sideIncreaze.x, bounds.extents.y + sideIncreaze.y, bounds.extents.z + sideIncreaze.z);
    }

    /// <summary>
    /// Move a transform to Center/Rotation, and changes its scale to Extents * 2
    /// </summary>
    public void ApplyToTransform(Transform transform, bool ignoreParentScale = true)
    {
        if (transform == null) return;
        var centerPosition = transform.position;
        if(matrixInitialized)
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
            var localScale = bounds.extents * 2;
            transform.localScale = new Vector3(localScale.x / parentLossyScale.x, localScale.y / parentLossyScale.y, localScale.z / parentLossyScale.z);
        }
    }
}
