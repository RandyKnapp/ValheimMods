Shader "JVLmock_Custom/Gradient Mapped Particle (Unlit)" {
	Properties {
		[NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 0
		[Enum(Red,0,Green,1,Blue,2,Alpha,3)] _GradientChannel ("Gradient Channel", Float) = 0
		[Toggle] _GradientAsAlpha ("Use Gradient Channel as Alpha", Float) = 1
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
		[Toggle] _SoftParticles ("Soft Particles", Float) = 0
		_SoftFadeFactor ("Fade Factor", Float) = 1
		_SoftNearFade ("Near Soft", Float) = 0.5
		_CameraFadeFactor ("Camera Fade Factor", Float) = 1
		[Toggle] _FejdFog ("Enable Fog", Float) = 0
		[MaterialToggle] _SkyMask ("SkyMask", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
}