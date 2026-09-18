using UnityEngine;

namespace Fusion.XR.Shared.Core.Interaction
{
    public interface IWrist
    {
        Transform WristTransform { get; }
    }

    public interface IDetailedWrist : IWrist
    {
        Transform WristBottomTransform { get; }
    }

    public interface IWristTracker {
        public void RegisterWrist(IWrist wrist);
        public void UnregisterWrist(IWrist wrist);
    }
}

