#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif
using UnityEngine;

namespace Fusion.XR.Shared.Automatization
{
    public static class AssetLookup
    {
        /// <summary>
        /// Criteria to find an asset based on its name
        /// - requiredPathElements: Optional required string in the found asset path
        /// - extension: will add a check on the file extension
        /// - startWithGuidLookupFirst: will first check if a file with this name as its guid is present (less collision risks when using guids)        /// 
        /// </summary>
        public struct AssetLookupCriteria
        {
            public string name;
            public bool requirePerfectNameMatch;
            public string[] requiredPathElements;
            public string extension;
            public bool startWithGuidLookupFirst;

            public AssetLookupCriteria(string name, string extension = null, string[] requiredPathElements = null, bool requirePerfectNameMatch = true, bool startWithGuidLookupFirst = true)
            {
                this.name = name;
                this.requiredPathElements = requiredPathElements;
                this.requirePerfectNameMatch = requirePerfectNameMatch;
                this.extension = extension;
                this.startWithGuidLookupFirst = startWithGuidLookupFirst;
            }

            public AssetLookupCriteria(string name, string extension = null, string requiredPathElement = null, bool requirePerfectNameMatch = true, bool startWithGuidLookupFirst = true)
            {
                this.name = name;
                this.requiredPathElements = new string[] { requiredPathElement };
                this.requirePerfectNameMatch = requirePerfectNameMatch;
                this.extension = extension;
                this.startWithGuidLookupFirst = startWithGuidLookupFirst;
            }
        }

        /// <summary>
        /// Try to find asset based on its name
        /// </summary>
        /// <param name="requiredPathElements">Optional required string in the found asset path</param>
        /// <returns>True if a matching asset has been found</returns>
        public static bool TryFindAsset<T>(AssetLookupCriteria criteria, out T asset) where T : UnityEngine.Object
        {
            asset = default;

            if (criteria.startWithGuidLookupFirst && TryFindAssetByGuid(criteria.name, out asset))
            {
                // A file with name as its guid has been found
                return true;
            }

            var lookupString = criteria.name;
            if (string.IsNullOrEmpty(criteria.extension) == false)
            {
                lookupString += " t:"+ criteria.extension;
            }
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets(lookupString);
            T bestCandidateAsset = null;
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath) == false)
                {
                    bool validCandidate = true;
                    // Check the requiredPathElements
                    if (IsCompatiblePath(assetPath, criteria.requiredPathElements) == false)
                    {
                        Debug.LogError(1);
                        validCandidate = false;
                    }

                    if (validCandidate && TryFindAssetByGuid(guid, out T candidateAsset))
                    {
                        var filename = Path.GetFileNameWithoutExtension(assetPath);
                        if (criteria.name == filename)
                        {
                            // Perfect match
                            asset = candidateAsset;
                            return true;
                        }
                        Debug.LogError(2);
                        if (criteria.requirePerfectNameMatch == false)
                        {
                            bestCandidateAsset = candidateAsset;
                        }
                    }
                }
                if (bestCandidateAsset != null)
                {
                    asset = bestCandidateAsset;
                    return true;
                }
            }
#endif
            Debug.LogError($"Asset not found \"{criteria.name}\" with selected filter");
            return false;
        }

        public static bool IsCompatiblePath(string assetPath, string[] requiredPathElements = null)
        {
            if (requiredPathElements != null)
            {
                foreach (var requiredPathElement in requiredPathElements)
                {
                    if (string.IsNullOrEmpty(requiredPathElement) == false && assetPath.Contains(requiredPathElement) == false)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool TryFindAsset<T>(string name, out T asset, string extension = null, string requiredPathElement = null) where T : UnityEngine.Object
        {
            return TryFindAsset<T>(new AssetLookupCriteria(name: name, extension: extension, requiredPathElement: requiredPathElement), out asset);
        }

        /// <summary>
        /// Try to find asset based on its guid. Validate its type (note, for prefabs, it will always return a GameObject type, so for included component, look for GameObject then search for the component in the result)
        /// </summary>
        /// <param name="requiredPathElements">Optional required string in the found asset path</param>
        /// <returns>True if a matching asset has been found</returns>
        public static bool TryFindAssetByGuid<T>(string guid, out T asset) where T : UnityEngine.Object
        {
            asset = default;
#if UNITY_EDITOR
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }
            if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(T))
            {
                return false;
            }

            asset = (T)AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                return false;
            }
            return true;
#else
            return false;
#endif
        }
    }
}

