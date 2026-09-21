namespace Fusion.Addons.Inspector.Editor
{
	using UnityEngine;
	using UnityEditor;
	using UnityEditor.Overlays;
	using UnityEditor.Toolbars;

	[Overlay(typeof(SceneView), "Fusion Inspector")]
	public class FusionInspectorToolbar : ToolbarOverlay
	{
		FusionInspectorToolbar() : base(OpenInspectorButton.ID)
		{
		}
	}

	[EditorToolbarElement(ID, typeof(SceneView))]
	public class OpenInspectorButton : EditorToolbarButton
	{
		public const string ID = nameof(FusionInspectorToolbar) + "." + nameof(OpenInspectorButton);

		public OpenInspectorButton()
		{
			Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Photon/FusionAddons/Inspector/Editor/EditorResources/PhotonCircle.png");
			if (iconTexture == null)
			{
				string[] guids = AssetDatabase.FindAssets("PhotonCircle t:Texture2D");
				if (guids.Length > 0)
				{
					iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
				}
			}

			icon    = iconTexture;
			tooltip = "Open Fusion Inspector.";

			clicked -= OnClick;
			clicked += OnClick;
		}

		private void OnClick()
		{
			FusionInspector.Create();
		}
	}

}
