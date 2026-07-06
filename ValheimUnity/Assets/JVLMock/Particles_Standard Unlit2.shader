Shader "JVLmock_Particles/Standard Unlit2" {
	Properties {
		_MainTex ("Albedo", 2D) = "white" {}
		_Color ("Color", Vector) = (1,1,1,1)
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		_BumpScale ("Scale", Float) = 1
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_EmissionColor ("Color", Vector) = (0,0,0,1)
		_EmissionMap ("Emission", 2D) = "white" {}
		_DistortionStrength ("Strength", Float) = 1
		_DistortionBlend ("Blend", Range(0, 1)) = 0.5
		_SoftParticlesNearFadeDistance ("Soft Particles Near Fade", Float) = 0
		_SoftParticlesFarFadeDistance ("Soft Particles Far Fade", Float) = 1
		_CameraNearFadeDistance ("Camera Near Fade", Float) = 1
		_CameraFarFadeDistance ("Camera Far Fade", Float) = 2
		[HideInInspector] _Mode ("__mode", Float) = 0
		[HideInInspector] _ColorMode ("__colormode", Float) = 0
		[HideInInspector] _FlipbookMode ("__flipbookmode", Float) = 0
		[HideInInspector] _LightingEnabled ("__lightingenabled", Float) = 0
		[HideInInspector] _DistortionEnabled ("__distortionenabled", Float) = 0
		[HideInInspector] _EmissionEnabled ("__emissionenabled", Float) = 0
		[HideInInspector] _BlendOp ("__blendop", Float) = 0
		[HideInInspector] _SrcBlend ("__src", Float) = 1
		[HideInInspector] _DstBlend ("__dst", Float) = 0
		[HideInInspector] _ZWrite ("__zw", Float) = 1
		[HideInInspector] _Cull ("__cull", Float) = 2
		[HideInInspector] _SoftParticlesEnabled ("__softparticlesenabled", Float) = 0
		[HideInInspector] _CameraFadingEnabled ("__camerafadingenabled", Float) = 0
		[HideInInspector] _SoftParticleFadeParams ("__softparticlefadeparams", Vector) = (0,0,0,0)
		[HideInInspector] _CameraFadeParams ("__camerafadeparams", Vector) = (0,0,0,0)
		[HideInInspector] _ColorAddSubDiff ("__coloraddsubdiff", Vector) = (0,0,0,0)
		[HideInInspector] _DistortionStrengthScaled ("__distortionstrengthscaled", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		fixed4 _Color;
		struct Input
		{
			float2 uv_MainTex;
		};
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	Fallback "VertexLit"
	//CustomEditor "StandardParticlesShaderGUI"
}