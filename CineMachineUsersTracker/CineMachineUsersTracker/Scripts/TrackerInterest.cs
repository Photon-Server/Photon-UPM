using System.Collections.Generic;
using UnityEngine;

public class TrackerInterest : MonoBehaviour
{
#if FUSION_WEAVER
    public float radiusForScaleOne = 1f;
    bool started = false;

    public bool adaptRadiusToScale = false;

    public bool unregisterWhenDisabled = true;
    public bool unregisterWhenRenderersDisabled = false;
    List<Renderer> renderers = new List<Renderer>();
    bool registered = false;

    float lastAppliedRadius = -1;
    float lastScaleWhenRadiusApplied = -1;
    UsersTracker[] trackers = null;
    private void Awake()
    {
        renderers = new List<Renderer>(GetComponentsInChildren<Renderer>(true));
    }
    private void Start()
    {
        started = true;
        Register();
    }

    private void Update()
    {
        if (unregisterWhenRenderersDisabled)
        {
            bool shouldTrack = false;
            foreach(var r in renderers)
            {
                if (r.enabled)
                {
                    shouldTrack = true;
                    break;
                }
            }
            if(shouldTrack != registered)
            {
                if (shouldTrack)
                    Register();
                else
                    Unregister();
            }
        }
        if (adaptRadiusToScale && registered && trackers != null && trackers.Length > 0)
        {
            var scale = ReferenceScale;
            if (Mathf.Abs(scale - lastScaleWhenRadiusApplied) > 0.01f)
            {
                UpdateRegistration();
            }
        }
    }

    protected virtual float ReferenceScale => transform.lossyScale.x;

    void UpdateRegistration()
    {
        var scale = ReferenceScale;
        var adaptedRadius = radiusForScaleOne * scale;
        foreach (var trackers in trackers)
        {
            trackers.UpdateTrackedObject(gameObject, radius: adaptedRadius);
        }
        lastAppliedRadius = adaptedRadius;
        lastScaleWhenRadiusApplied = scale;
    }

    void Register() {
        registered = true;
        var scale = ReferenceScale;
        var adaptedRadius = radiusForScaleOne;
        if (adaptRadiusToScale)
        {
            adaptedRadius = radiusForScaleOne * scale;
        }
        trackers = FindObjectsByType<UsersTracker>(FindObjectsSortMode.None);
        foreach(var tracker in trackers)
        {
            tracker.AddTrackedObject(gameObject, radius: adaptedRadius);
        }
        lastScaleWhenRadiusApplied = scale;
        lastAppliedRadius = adaptedRadius;
    }

    private void OnEnable()
    {
        if (started && unregisterWhenDisabled)
        {
            Register();
        }
    }

    private void OnDisable()
    {
        Unregister();
    }

    private void OnDestroy()
    {
        Unregister();
    }

    void Unregister() {
        registered = false;
        trackers = FindObjectsByType<UsersTracker>(FindObjectsSortMode.None);
        foreach (var tracker in trackers)
        {
            tracker.RemoveTrackObject(gameObject);
        }
    }
#endif
}
