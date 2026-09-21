namespace Fusion.Addons.Inspector.Editor
{
	using System;
	using UnityEngine;

	internal static class ObjectUtility
	{
		public static string GetObjectId(UnityEngine.Object unityObject)
		{
#if UNITY_6000_5_OR_NEWER
			return unityObject.GetEntityId().ToString();
#else
			return unityObject.GetInstanceID().ToString();
#endif
		}

		public static UnityEngine.Object[] FindObjectsByType(Type type, FindObjectsInactive findObjectsInactive)
		{
#if UNITY_6000_5_OR_NEWER
			return UnityEngine.Object.FindObjectsByType(type, findObjectsInactive);
#else
			return UnityEngine.Object.FindObjectsByType(type, findObjectsInactive, FindObjectsSortMode.InstanceID);
#endif
		}
	}
}
