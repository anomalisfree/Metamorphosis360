Shader "Mapbox/Raster With Transparency" 
{
	Properties 
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "blue" {}
		_Alpha ("Alpha", Float) = 1
	}

	SubShader 
	{
		Tags 
		{ 
			"Queue" = "Transparent" 
			"RenderType" = "Transparent" 
			"RenderPipeline" = "UniversalPipeline"
			"IgnoreProjector" = "True"
		}
		LOD 100

		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off 
		ZWrite Off

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float4 _Color;
				float _Alpha;
			CBUFFER_END

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
				float fogCoord : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = positionInputs.positionCS;
				output.uv = input.uv;
				output.color = input.color;
				output.fogCoord = ComputeFogFactor(output.positionCS.z);

				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

				float2 uv = TRANSFORM_TEX(input.uv, _MainTex);
				half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
				
				half4 color = input.color * texColor;
				
				// Check how much color used for transparency equals _Color.rgb
				// 1=no match, 0=full match
				half3 delta = texColor.rgb - _Color.rgb;
				float match = dot(delta, delta);
				float threshold = step(match, 0.1);

				color.a = (1.0 - (threshold * (1.0 - _Color.a))) * _Alpha;

				color.rgb = MixFog(color.rgb, input.fogCoord);

				return color;
			}
			ENDHLSL
		}
	} 

	FallBack "Universal Render Pipeline/Unlit"
}