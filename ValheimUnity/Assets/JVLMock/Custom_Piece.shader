Shader "JVLmock_Custom/Piece" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_MetallicTex ("Metallic", 2D) = "white" {}
		_Metallic ("Metallic", Range(0, 1)) = 0
		_MetallicAlphaGloss ("Metal smoothness", Range(0, 1)) = 0
		[HDR] _MetalColor ("Metal color", Vector) = (1,1,1,1)
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal intensity", Float) = 1
		[MaterialToggle] _TriplanarMap ("Use triplanar mapping", Float) = 0
		[MaterialToggle] _TriplanarLocalPos ("Triplanar local pos", Float) = 0
		_TriplanarScale ("Triplanar scale", Float) = 1
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_EmissionMap ("Emissive (RGB)", 2D) = "white" {}
		_EmissionColor ("Emissive", Vector) = (0,0,0,0)
		_RippleDistance ("Noise distance", Float) = 0
		_RippleFreq ("Noise freq", Float) = 1
		_ValueNoise ("ValueNoise", Float) = 0.5
		[MaterialToggle] _ValueNoiseVertex ("Value noise per vertex", Float) = 0
		[MaterialToggle] _TwoSidedNormals ("Twosided normals", Float) = 0
		[Enum(Off,0,Back,2)] _Cull ("Cull", Float) = 2
		[MaterialToggle] _AddRain ("Add Rain", Float) = 1
		[MaterialToggle] _MoveableObject ("Non-Static Object", Float) = 0
		_NoiseTex ("Noise", 2D) = "white" {}
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
}