Shader "Custom/VR_Portal"
{
	Properties{
	[IntRange] _StencilID("Stencil ID", Range(0,255)) = 0
	}

	SubShader
	{

		Tags {
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
			"Queue" = "Geometry"
		}

		Pass {

				ColorMask 0
				ZWrite Off
				ZTest LEqual


		Stencil
		{
			Ref[_StencilID]
			Comp Always
			Pass Replace

}
	}
	}
}