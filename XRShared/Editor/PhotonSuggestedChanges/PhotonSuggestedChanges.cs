#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Photon.Tools
{
    public interface ISuggestedChangeDescriptor
    {
        public string Description { get; }
        public bool IsAlreadyApplied { get; }
        public bool IsApplicable => true;
        public string Tooltip => "";
        // Image can be either a guid, or a filename (without extension - if 2 files have the same name in the project, a collision could occur)
        public string Image => null;
        public string CategoryName => null;
        // Lower values appear first
        public int Weight => 1_000;
        // Should not be replaced in implementing struct (or that would require to implement a new logic in UI)
        public bool ShouldDisplay => IsApplicable && IsAlreadyApplied == false;

    }

    public interface ISuggestedApplicableChangeDescriptor : ISuggestedChangeDescriptor
    {
        public bool CanApply => ShouldDisplay;

        // If a list of apply action descriptions is provided, you can select the default one (will be the first otherwise). Set it to null to disable this suggestion in auto fix (or implement CanBeIncludedInAutofixAll)
        public string DefaultApplyDescription => AlternativeApplyDescriptions.Length > 0 ? AlternativeApplyDescriptions[0] : "Apply";
        public bool CanBeIncludedInAutofixAll => DefaultApplyDescription != null;
        public string[] AlternativeApplyDescriptions => new string[] { "Apply" };
        public void Apply(string selectedApplyDescription);
    }

    [InitializeOnLoad]
    public class PhotonSuggestedChangesManager
    {
        public static List<ISuggestedChangeDescriptor> SuggestedChangeDescriptors = new List<ISuggestedChangeDescriptor>();
        static List<ISuggestedChangeDescriptor> _displayableSuggestedChangeDescriptors = new List<ISuggestedChangeDescriptor>();
        static List<ISuggestedChangeDescriptor> _appliedSuggestedChangeDescriptors = new List<ISuggestedChangeDescriptor>();
        static List<ISuggestedChangeDescriptor> _nonApplicableSuggestedChangeDescriptors = new List<ISuggestedChangeDescriptor>();
        public static List<ISuggestedChangeDescriptor> DisplayableSuggestedChangeDescriptors
        {
            get
            {
                return _displayableSuggestedChangeDescriptors;
            }
        }

        public static List<ISuggestedChangeDescriptor> AppliedSuggestedChangeDescriptors
        {
            get
            {
                return _appliedSuggestedChangeDescriptors;
            }
        }
        public static List<ISuggestedChangeDescriptor> NonApplicableSuggestedChangeDescriptors
        {
            get
            {
                return _nonApplicableSuggestedChangeDescriptors;
            }
        }

        public static void UpdateFilteredDescriptorLists()
        {
            _displayableSuggestedChangeDescriptors.Clear();
            _appliedSuggestedChangeDescriptors.Clear();
            _nonApplicableSuggestedChangeDescriptors.Clear();
            foreach (var d in SuggestedChangeDescriptors)
            {
                if (d.ShouldDisplay)
                {
                    _displayableSuggestedChangeDescriptors.Add(d);
                } 
                else if(d.IsApplicable == false)
                {
                    _nonApplicableSuggestedChangeDescriptors.Add(d);
                } 
                else if(d.IsAlreadyApplied)
                {
                    _appliedSuggestedChangeDescriptors.Add(d);
                } 
                else
                {
                    UnityEngine.Debug.LogError("Not displayble type of suggestion: applicable, not already applied, and yet not displayed. Either fix the suggestion or add a new section to the suggestion UI");
                }
            }
        }

        static PhotonSuggestedChangesManager()
        {
        }
    }

    [InitializeOnLoad]
    public class PhotonSuggestedChangesSettings
    {
        public const string SettingsProviderPath = "Project/Photon/Suggested changes";
        [SettingsProvider]
        public static SettingsProvider CreateSuggestedChangesProvider()
        {
            return new SettingsProvider(SettingsProviderPath, SettingsScope.Project)
            {
                label = "Suggested changes",
                activateHandler = (_, root) =>
                {
                    try
                    {
                        CreateSuggestedChangesView(root);
                    }
                    catch(System.Exception e)
                    {
                        UnityEngine.Debug.LogError("Unable to create view: "+e.Message+"\n"+e.StackTrace);
                    }
                },
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "Photon", "Suggested changes" })
            };
        }

        static void CreateSuggestedChangesView(VisualElement root)
        {
            var suggestionView = LoadAsset<VisualTreeAsset>("SuggestionView");
            var suggestionEntryTemplate = LoadAsset<VisualTreeAsset>("SuggestionEntry");
            var suggestionAppliedEntryTemplate = LoadAsset<VisualTreeAsset>("SuggestionAppliedEntry");
            var suggestionNonapplicableEntryTemplate = LoadAsset<VisualTreeAsset>("SuggestionNonapplicableEntry");

            // Instantiate UXML
            VisualElement contentFromUXML = suggestionView.Instantiate();

            // Apply all button
            var applyAllButton = contentFromUXML.Q<Button>("apply-all-button");
            applyAllButton.clicked += () => {
                ApplyAll();
            };

            // List
            PhotonSuggestedChangesManager.UpdateFilteredDescriptorLists();
            var applicableSuggestionListView = contentFromUXML.Q<ListView>("suggestion-list");
            ConfigureSuggestionList(applicableSuggestionListView, PhotonSuggestedChangesManager.DisplayableSuggestedChangeDescriptors, suggestionEntryTemplate, displayApplyButtons: true, fixedItemHeight: 91);
            var apppliedSuggestionLiveview = contentFromUXML.Q<ListView>("applied-suggestion-list");
            ConfigureSuggestionList(apppliedSuggestionLiveview, PhotonSuggestedChangesManager.AppliedSuggestedChangeDescriptors, suggestionAppliedEntryTemplate, displayApplyButtons: false, fixedItemHeight: 41, defaultImageHandling: DefaultImageHandling.Preserve);
            var nonapplicableSuggestionLiveview = contentFromUXML.Q<ListView>("nonapplicable-suggestion-list");
            ConfigureSuggestionList(nonapplicableSuggestionLiveview, PhotonSuggestedChangesManager.NonApplicableSuggestedChangeDescriptors, suggestionNonapplicableEntryTemplate, displayApplyButtons: false, fixedItemHeight: 41, defaultImageHandling: DefaultImageHandling.Hide);


            root.Add(contentFromUXML);
        }

        enum DefaultImageHandling
        {
            Hide,
            Preserve,
            ShowAndReplaceWhenOverriden
        }

        static void ConfigureSuggestionList(ListView listView, List<ISuggestedChangeDescriptor> filteredSuggestions, VisualTreeAsset suggestionEntryTemplate, bool displayApplyButtons = true, float fixedItemHeight = 90, DefaultImageHandling defaultImageHandling = DefaultImageHandling.Hide)
        {
            listView.makeItem = () => {
                var listEntry = suggestionEntryTemplate.Instantiate();
                return listEntry;
            };
            listView.bindItem = (listEntry, index) => {
                var suggestionDescriptor = filteredSuggestions[index];

                var suggestionDescriptionLabel = listEntry.Q<Label>("suggestion-description");
                var suggestionImage = listEntry.Q<VisualElement>("suggestion-image");
                var suggestionImageContainer = listEntry.Q<VisualElement>("suggestion-image-container");
                var description = suggestionDescriptor.Description;

                suggestionDescriptionLabel.text = description;
                if (string.IsNullOrEmpty(suggestionDescriptor.Tooltip) == false)
                {
                    suggestionDescriptionLabel.tooltip = suggestionDescriptor.Tooltip;
                    suggestionImage.tooltip = suggestionDescriptor.Tooltip;
                }
                //suggestionDescriptionLabel.selection.isSelectable = true;
                if (defaultImageHandling == DefaultImageHandling.Hide)
                {
                    suggestionImage.style.backgroundImage = null;
                    suggestionImageContainer.style.display = DisplayStyle.None;
                }
                var imageName = suggestionDescriptor.Image;
                if (defaultImageHandling != DefaultImageHandling.Preserve && string.IsNullOrEmpty(imageName) == false)
                {
                    try
                    {
                        suggestionImage.style.backgroundImage = LoadAsset<Texture2D>(imageName);
                        suggestionImageContainer.style.display = DisplayStyle.Flex;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Unable to find editor image " + imageName + "\n" + e);
                    }
                }

                var buttonsContainer = listEntry.Q<VisualElement>("apply-buttons-container");
                if(buttonsContainer != null)
                {
                    if (displayApplyButtons)
                    {
                        var templateButton = listEntry.Q<Button>("apply-button");
                        buttonsContainer.Remove(templateButton);

                        if (suggestionDescriptor is ISuggestedApplicableChangeDescriptor applicableChangeDescriptor && applicableChangeDescriptor.CanApply)
                        {
                            foreach (var applyDescription in applicableChangeDescriptor.AlternativeApplyDescriptions)
                            {
                                var applyButton = new Button(() =>
                                {
                                    applicableChangeDescriptor.Apply(applyDescription);
                                });
                                applyButton.text = applyDescription;
                                buttonsContainer.Add(applyButton);
                            }
                        }
                    }
                    else
                    {
                        buttonsContainer.style.display = DisplayStyle.None;
                    }
                }
            };
            listView.fixedItemHeight = fixedItemHeight;
            listView.selectionChanged += (IEnumerable<object> selectedItems) => {
                foreach (var i in selectedItems)
                {

                }
                listView.ClearSelection();
            };
            listView.itemsSource = filteredSuggestions;
        }
        static void ApplyAll()
        {
            foreach (var suggestedChangeDescriptor in PhotonSuggestedChangesManager.SuggestedChangeDescriptors)
            {
                if (suggestedChangeDescriptor is ISuggestedApplicableChangeDescriptor applicableChangeDescriptor && applicableChangeDescriptor.CanApply)
                {
                    if (applicableChangeDescriptor.CanBeIncludedInAutofixAll)
                    {
                        applicableChangeDescriptor.Apply(applicableChangeDescriptor.DefaultApplyDescription);
                    }
                }
            }
        }

        /// <summary>
        /// Load an asset based on its name. Exact name and asset type are checked.
        /// Note: 
        /// - if 2 files in the project have the same name, they can't be differentiated. Use guid lookup to avoid this instead (see XRShared's AssetLookup.TryFindAssetByGuid)
        /// - in the context of XRshared, can be replaced by AssetLookup.TryFindAsset
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="Exception">Raise an exception if the file is not found</exception>
        public static T LoadAsset<T>(string name) where T : UnityEngine.Object
        {
            // If name is a guid, we check first
            string assetPath = null;
            if (TryFindAssetPathByGuid<T>(name, out assetPath))
            {
                // A file with name as its guid has been found
            }
            else if (TryFindAssetPathByName<T>(name, out assetPath))
            {
                // A file with this exact name has been found
            }

            if (string.IsNullOrEmpty(assetPath) == false)
            { 
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset == null)
                {
                    Debug.LogError("Unable to load asset "+ name);
                    throw new Exception($"Asset at {assetPath} cannot be loaded. Detected type: " + AssetDatabase.GetMainAssetTypeAtPath(assetPath));
                }
                return asset;
            }
            else
            {
                Debug.LogError("Unable to load asset " + name);
                throw new Exception("Unable to find asset " + name);
            }
        }

        public static bool TryFindAssetPathByName<T>(string assetName, out string path)
        {
            path = null;
            var assets = AssetDatabase.FindAssets(assetName);
            foreach (var guid in assets)
            {
                if (TryFindAssetPathByGuid<T>(guid, out path, expectedAssetName: assetName))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if it manages to find an asset path for a given guid
        /// Validate the asset type, and the filename (without extension) if given
        /// </summary>
        public static bool TryFindAssetPathByGuid<T>(string guid, out string path, string expectedAssetName = null)
        {
            path = null;
            var potentialPath = AssetDatabase.GUIDToAssetPath(guid);

            bool validCandidate = string.IsNullOrEmpty(potentialPath) == false;
            if (validCandidate && string.IsNullOrEmpty(expectedAssetName) == false)
            {
                var filename = Path.GetFileNameWithoutExtension(potentialPath);
                validCandidate = filename == expectedAssetName;
            }

            if (validCandidate && AssetDatabase.GetMainAssetTypeAtPath(potentialPath) == typeof(T))
            {
                path = potentialPath;
                return true;
            }
            return false;
        }
    }
}

#endif
