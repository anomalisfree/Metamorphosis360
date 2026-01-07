Shader "Mapbox/MapboxStyles"
{
	Properties
	{
		_BaseColor ("BaseColor", Color) = (1,1,1,1)
		_DetailColor1 ("DetailColor1", Color) = (1,1,1,1)
		_DetailColor2 ("DetailColor2", Color) = (1,1,1,1)

		_BaseTex ("Base", 2D) = "white" {}
		_DetailTex1 ("Detail_1", 2D) = "white" {}
		_DetailTex2 ("Detail_2", 2D) = "white" {}

		_Emission ("Emission", Range(0.0, 1.0)) = 0.1
	}

	SubShader
	{
		Tags 
		{ 
			"RenderType" = "Opaque" 
			"RenderPipeline" = "UniversalPipeline"
			"Queue" = "Geometry"
		}
		LOD 200

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float4 _BaseColor;
				float4 _DetailColor1;
				float4 _DetailColor2;
				float4 _BaseTex_ST;
				float4 _DetailTex1_ST;
				float4 _DetailTex2_ST;
				float _Emission;
			CBUFFER_END

			TEXTURE2D(_BaseTex);
			SAMPLER(sampler_BaseTex);
			TEXTURE2D(_DetailTex1);
			SAMPLER(sampler_DetailTex1);
			TEXTURE2D(_DetailTex2);
			SAMPLER(sampler_DetailTex2);

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				float3 normalOS : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float fogCoord : TEXCOORD2;
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
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.fogCoord = ComputeFogFactor(output.positionCS.z);

				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

				float2 uv_BaseTex = TRANSFORM_TEX(input.uv, _BaseTex);
				float2 uv_DetailTex1 = TRANSFORM_TEX(input.uv, _DetailTex1);
				float2 uv_DetailTex2 = TRANSFORM_TEX(input.uv, _DetailTex2);

				half4 baseTexture = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, uv_BaseTex);
				half4 detailTexture1 = SAMPLE_TEXTURE2D(_DetailTex1, sampler_DetailTex1, uv_DetailTex1);
				half4 detailTexture2 = SAMPLE_TEXTURE2D(_DetailTex2, sampler_DetailTex2, uv_DetailTex2);

				half4 baseDetail1_Result = lerp(_BaseColor, _DetailColor1, detailTexture1.a);
				half4 detail1Detail2_Result = lerp(baseDetail1_Result, _DetailColor2, detailTexture2.a);

				half4 color = baseTexture * detail1Detail2_Result;
				half3 emission = color.rgb * _Emission;

				// Simple lighting
				Light mainLight = GetMainLight();
				half3 normalWS = normalize(input.normalWS);
				half NdotL = saturate(dot(normalWS, mainLight.direction));
				half3 lighting = mainLight.color * NdotL + unity_AmbientSky.rgb;

				half3 finalColor = color.rgb * lighting + emission;
				finalColor = MixFog(finalColor, input.fogCoord);

				return half4(finalColor, 1.0);
			}
			ENDHLSL
		}

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
			ENDHLSL
		}
	}

	FallBack "Universal Render Pipeline/Lit"
}
