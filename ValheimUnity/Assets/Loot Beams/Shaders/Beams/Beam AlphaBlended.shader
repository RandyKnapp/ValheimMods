// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Loot Beams/Beam AlphaBlended"
{
	Properties
	{
		[Header(Core Options)]_TintColor("Color", Color) = (1,1,1,1)
		_Glow("Glow", Float) = 1
		_GlobalColor("Global Color", Range( 0 , 1)) = 1
		[NoScaleOffset]_MainTex("Main Texture", 2D) = "white" {}
		_BotFadeRange("Bot Fade Range", Range( 0 , 1)) = 0
		_BotFadeSmooth("Bot Fade Smooth", Range( 0 , 1)) = 0
		[Header(Particle Options)][Toggle]_ForceParticle("Force Particle", Float) = 0
		[Toggle]_ForceUVOffset("Force UV Offset", Float) = 0
		[Header(Mask Options)][Toggle]_BlendMask("Blend Mask", Float) = 0
		[NoScaleOffset]_MaskTex("Mask Texture", 2D) = "white" {}
		_MaskValue("Mask Value", Float) = 1
		_MaskScale("Mask Scale", Float) = 1
		_MaskScroll("Mask Scroll", Float) = 1
		[Header(Ripple Options)][Toggle]_UVDistortion("UV Distortion", Float) = 0
		[NoScaleOffset]_RippleTexture("Ripple Texture", 2D) = "white" {}
		_RippleValue("Ripple Value", Float) = 0.1
		_RippleScale("Ripple Scale", Float) = 1
		_RippleScroll("Ripple Scroll", Float) = 1

	}


	Category 
	{
		SubShader
		{
		LOD 0

			Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMask RGB
			Cull Off
			Lighting Off 
			ZWrite Off
			ZTest LEqual
			
			Pass {
			
				CGPROGRAM
				
				#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
				#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
				#endif
				
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				#pragma multi_compile_instancing
				#pragma multi_compile_particles
				#pragma multi_compile_fog
				#include "UnityShaderVariables.cginc"
				#define ASE_NEEDS_FRAG_COLOR


				#include "UnityCG.cginc"

				struct appdata_t 
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					
				};

				struct v2f 
				{
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_FOG_COORDS(1)
					#ifdef SOFTPARTICLES_ON
					float4 projPos : TEXCOORD2;
					#endif
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
					
				};
				
				
				#if UNITY_VERSION >= 560
				UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
				#else
				uniform sampler2D_float _CameraDepthTexture;
				#endif

				//Don't delete this comment
				// uniform sampler2D_float _CameraDepthTexture;

				uniform sampler2D _MainTex;
				uniform fixed4 _TintColor;
				uniform float4 _MainTex_ST;
				uniform float _InvFade;
				uniform float _ForceUVOffset;
				uniform float _UVDistortion;
				uniform sampler2D _RippleTexture;
				SamplerState sampler_RippleTexture;
				uniform float _RippleScale;
				uniform float _RippleScroll;
				uniform float _RippleValue;
				SamplerState sampler_MainTex;
				uniform float _BlendMask;
				uniform sampler2D _MaskTex;
				SamplerState sampler_MaskTex;
				uniform float _MaskScale;
				uniform float _MaskScroll;
				uniform float _MaskValue;
				uniform float _ForceParticle;
				uniform float _BotFadeRange;
				uniform float _BotFadeSmooth;
				uniform float _GlobalColor;
				uniform float _Glow;


				v2f vert ( appdata_t v  )
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					

					v.vertex.xyz +=  float3( 0, 0, 0 ) ;
					o.vertex = UnityObjectToClipPos(v.vertex);
					#ifdef SOFTPARTICLES_ON
						o.projPos = ComputeScreenPos (o.vertex);
						COMPUTE_EYEDEPTH(o.projPos.z);
					#endif
					o.color = v.color;
					o.texcoord = v.texcoord;
					UNITY_TRANSFER_FOG(o,o.vertex);
					return o;
				}

				fixed4 frag ( v2f i  ) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( i );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( i );

					#ifdef SOFTPARTICLES_OFF
						float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
						float partZ = i.projPos.z;
						float fade = saturate (_InvFade * (sceneZ-partZ));
						i.color.a *= fade;
					#endif

					float4 texCoord134 = i.texcoord;
					texCoord134.xy = i.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float4 UV96 = texCoord134;
					float2 appendResult39 = (float2(_RippleScroll , 0.0));
					float4 temp_cast_2 = (tex2D( _RippleTexture, ( (UV96*_RippleScale + 0.0) + float4( ( appendResult39 * _Time.y ), 0.0 , 0.0 ) ).xy ).r).xxxx;
					float4 lerpResult31 = lerp( UV96 , temp_cast_2 , _RippleValue);
					float4 UVDistortion124 = lerpResult31;
					float2 appendResult136 = (float2(texCoord134.x , ( texCoord134.y - texCoord134.z )));
					float2 UVParticle138 = appendResult136;
					float4 tex2DNode3 = tex2D( _MainTex, (( _ForceUVOffset )?( float4( UVParticle138, 0.0 , 0.0 ) ):( (( _UVDistortion )?( UVDistortion124 ):( UV96 )) )).xy );
					float2 appendResult68 = (float2(_MaskScroll , 0.0));
					float BlendMask100 = pow( tex2D( _MaskTex, ( (UV96*_MaskScale + 0.0) + float4( ( appendResult68 * _Time.y ), 0.0 , 0.0 ) ).xy ).r , _MaskValue );
					float GradientOpacity128 = ( ( ( 1.0 - UV96.x ) - ( ( _BotFadeRange * ( _BotFadeSmooth + 1.0 ) ) - _BotFadeSmooth ) ) * ( 1.0 / _BotFadeSmooth ) );
					float clampResult26 = clamp( (( _ForceParticle )?( 1.0 ):( GradientOpacity128 )) , 0.0 , 1.0 );
					

					fixed4 col = ( ( _TintColor * _TintColor.a * tex2DNode3 * i.color ) * ( ( ( tex2DNode3.a * i.color.a * _TintColor.a * (( _BlendMask )?( BlendMask100 ):( 1.0 )) ) * clampResult26 ) * _GlobalColor ) * _Glow );
					UNITY_APPLY_FOG(i.fogCoord, col);
					return col;
				}
				ENDCG 
			}
		}	
	}
	
	
	
}


/*ASEBEGIN
Version=18400
212;213;1920;777;5138.646;953.2684;3.977162;True;False
Node;AmplifyShaderEditor.TextureCoordinatesNode;134;-4748.617,-534.2709;Inherit;False;0;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;116;-4085.453,-803.4966;Inherit;False;293;165;UV Setup;1;96;;1,0.8416586,0.1921569,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;96;-4035.452,-753.4969;Inherit;False;UV;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;40;-4937.462,267.0324;Float;False;Property;_RippleScroll;Ripple Scroll;17;0;Create;True;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;120;-4863.919,-48.45546;Inherit;False;258;165;Add UV;1;121;;1,0.8416586,0.1921569,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;121;-4813.919,1.544723;Inherit;False;96;UV;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;49;-4758.602,133.8554;Float;False;Property;_RippleScale;Ripple Scale;16;0;Create;True;0;0;False;0;False;1;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;38;-4676.682,436.6652;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;39;-4641.731,272.1134;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-5154.946,1522.831;Float;False;Constant;_Float;Float;4;0;Create;True;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;118;-3218.585,-969.4289;Inherit;False;258;165;Add UV;1;119;;1,0.8416586,0.1921569,1;0;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;48;-4496.568,33.13488;Inherit;True;3;0;FLOAT4;0,0,0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;36;-4393.031,271.9571;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;60;-3360.803,-653.3893;Float;False;Property;_MaskScroll;Mask Scroll;12;0;Create;True;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;122;-4264.224,836.0823;Inherit;False;258;165;Add UV;1;123;;1,0.8416586,0.1921569,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-4898.945,1410.83;Float;False;Property;_BotFadeSmooth;Bot Fade Smooth;5;0;Create;True;0;0;False;0;False;0;0.06;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;68;-3145.788,-648.3175;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;33;-4079.919,33.84533;Inherit;True;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;61;-3102.942,-782.9276;Float;False;Property;_MaskScale;Mask Scale;11;0;Create;True;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;67;-3112.901,-400.709;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;16;-4578.946,1186.83;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;25;-4642.946,1074.83;Float;False;Property;_BotFadeRange;Bot Fade Range;4;0;Create;True;0;0;False;0;False;0;0.06;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;119;-3168.585,-919.4288;Inherit;False;96;UV;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;123;-4214.224,886.0826;Inherit;False;96;UV;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.BreakToComponentsNode;114;-3971.783,890.0696;Inherit;False;FLOAT4;1;0;FLOAT4;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;-4194.946,1154.83;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;62;-2884.997,-914.0279;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.WireNode;133;-4032.149,1393.88;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;32;-3773.421,6.270464;Inherit;True;Property;_RippleTexture;Ripple Texture;14;1;[NoScaleOffset];Create;True;0;0;False;0;False;-1;None;940942cbd112b074ea5147810de1f6b6;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;42;-3653.505,236.5968;Float;False;Property;_RippleValue;Ripple Value;15;0;Create;True;0;0;False;0;False;0.1;0.03;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;63;-2820.876,-649.4923;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;14;-3810.945,1154.83;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;64;-2551.641,-913.4097;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;31;-3335.475,11.29842;Inherit;True;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;135;-4411.058,-487.383;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;125;-2989.649,-44.23381;Inherit;False;293;165;Add Ripple;1;124;;0.1921569,1,0.5140226,1;0;0
Node;AmplifyShaderEditor.OneMinusNode;27;-3635.89,890.6094;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;12;-4578.946,1522.831;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;136;-4154.564,-510.2319;Inherit;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;132;-3363.683,1393.86;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;127;-2561.541,517.8858;Inherit;False;285;165;Add Ripple;1;126;;0.1921569,1,0.5140226,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;117;-2539.552,178.9851;Inherit;False;258;165;Add UV;1;99;;1,0.8416586,0.1921569,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;66;-2205.498,-718.4229;Float;False;Property;_MaskValue;Mask Value;10;0;Create;True;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;124;-2938.117,5.766363;Inherit;False;UVDistortion;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CommentaryNode;139;-3897.485,-565.1216;Inherit;False;293;165;Particle UV;1;138;;1,0.1921569,0.7217064,1;0;0
Node;AmplifyShaderEditor.SamplerNode;65;-2335.108,-940.7314;Inherit;True;Property;_MaskTex;Mask Texture;9;1;[NoScaleOffset];Create;False;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleSubtractOpNode;11;-3425.731,890.6512;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;141;-2184.232,517.6453;Inherit;False;270;165;Add UV Particle;1;140;;1,0.1921569,0.7217064,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;99;-2492.151,227.6852;Inherit;False;96;UV;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;138;-3847.485,-515.1213;Inherit;False;UVParticle;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;126;-2511.542,567.8859;Inherit;False;124;UVDistortion;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.PowerNode;70;-1890.974,-912.1984;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;103;-1598.49,-967.5717;Inherit;False;293;165;Opacity Mask Setup;1;100;;0.1933962,0.7241934,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;129;-2850.171,835.6827;Inherit;False;308;165;Gradient Opacity Setup;1;128;;1,0.3074892,0.1921569,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-3167.15,890.3057;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;140;-2134.231,567.6452;Inherit;False;138;UVParticle;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;104;-2918.76,348.2028;Inherit;False;272;165;Add Opacity Mask;1;102;;0.1933962,0.7241934,1,1;0;0
Node;AmplifyShaderEditor.ToggleSwitchNode;80;-2119.51,230.9171;Inherit;False;Property;_UVDistortion;UV Distortion;13;0;Create;True;0;0;False;1;Header(Ripple Options);False;0;2;0;FLOAT4;11,1,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CommentaryNode;131;-1721.338,641.9276;Inherit;False;308;165;Add Gradient Opacity;1;130;;1,0.3074892,0.1921569,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;128;-2800.171,886.9479;Inherit;False;GradientOpacity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;100;-1548.49,-917.5717;Inherit;False;BlendMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode;137;-1846.993,230.8098;Inherit;False;Property;_ForceUVOffset;Force UV Offset;7;0;Create;True;0;0;False;0;False;0;2;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;130;-1671.338,691.9276;Inherit;False;128;GradientOpacity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;102;-2868.76,398.2028;Inherit;False;100;BlendMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;3;-1552.4,207.2888;Inherit;True;Property;_MainTex;Main Texture;3;1;[NoScaleOffset];Create;False;0;0;False;0;False;-1;None;22f0f59668faf9c4b82a93cfbbe402bf;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;143;-1350.723,692.719;Inherit;False;Property;_ForceParticle;Force Particle;6;0;Create;True;0;0;False;1;Header(Particle Options);False;0;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;2;-1467.49,-0.4162838;Float;False;Property;_TintColor;Color;0;0;Create;False;0;0;False;1;Header(Core Options);False;1,1,1,1;0,0.6398292,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ToggleSwitchNode;73;-2490.793,374.4976;Inherit;False;Property;_BlendMask;Blend Mask;8;0;Create;True;0;0;False;1;Header(Mask Options);False;0;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;4;-1436.254,438.2246;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;26;-1022.866,699.437;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5;-1083.342,306.58;Inherit;True;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;30;-765.6334,553.6699;Float;False;Property;_GlobalColor;Global Color;2;0;Create;True;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;1;-1081.484,3.187324;Inherit;True;4;4;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-702.9325,305.4364;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-384.9608,550.404;Float;False;Property;_Glow;Glow;1;0;Create;True;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;95;-525.7389,173.3899;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;29;-444.7932,305.5958;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;59;-125.3905,284.6355;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;58;103.5567,283.8154;Float;False;True;-1;2;;0;7;Loot Beams/Beam AlphaBlended;0b6a9f8b4f707c74ca64c0be8e590de0;True;SubShader 0 Pass 0;0;0;SubShader 0 Pass 0;2;True;2;5;False;-1;10;False;-1;0;1;False;-1;1;False;-1;False;False;False;False;False;False;False;False;True;2;False;-1;True;True;True;True;False;0;False;-1;False;False;False;False;True;2;False;-1;True;3;False;-1;False;True;4;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;0;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;96;0;134;0
WireConnection;39;0;40;0
WireConnection;48;0;121;0
WireConnection;48;1;49;0
WireConnection;36;0;39;0
WireConnection;36;1;38;0
WireConnection;68;0;60;0
WireConnection;33;0;48;0
WireConnection;33;1;36;0
WireConnection;16;0;18;0
WireConnection;16;1;17;0
WireConnection;114;0;123;0
WireConnection;15;0;25;0
WireConnection;15;1;16;0
WireConnection;62;0;119;0
WireConnection;62;1;61;0
WireConnection;133;0;18;0
WireConnection;32;1;33;0
WireConnection;63;0;68;0
WireConnection;63;1;67;0
WireConnection;14;0;15;0
WireConnection;14;1;133;0
WireConnection;64;0;62;0
WireConnection;64;1;63;0
WireConnection;31;0;121;0
WireConnection;31;1;32;1
WireConnection;31;2;42;0
WireConnection;135;0;134;2
WireConnection;135;1;134;3
WireConnection;27;0;114;0
WireConnection;12;0;17;0
WireConnection;12;1;18;0
WireConnection;136;0;134;1
WireConnection;136;1;135;0
WireConnection;132;0;12;0
WireConnection;124;0;31;0
WireConnection;65;1;64;0
WireConnection;11;0;27;0
WireConnection;11;1;14;0
WireConnection;138;0;136;0
WireConnection;70;0;65;1
WireConnection;70;1;66;0
WireConnection;9;0;11;0
WireConnection;9;1;132;0
WireConnection;80;0;99;0
WireConnection;80;1;126;0
WireConnection;128;0;9;0
WireConnection;100;0;70;0
WireConnection;137;0;80;0
WireConnection;137;1;140;0
WireConnection;3;1;137;0
WireConnection;143;0;130;0
WireConnection;73;1;102;0
WireConnection;26;0;143;0
WireConnection;5;0;3;4
WireConnection;5;1;4;4
WireConnection;5;2;2;4
WireConnection;5;3;73;0
WireConnection;1;0;2;0
WireConnection;1;1;2;4
WireConnection;1;2;3;0
WireConnection;1;3;4;0
WireConnection;8;0;5;0
WireConnection;8;1;26;0
WireConnection;95;0;1;0
WireConnection;29;0;8;0
WireConnection;29;1;30;0
WireConnection;59;0;95;0
WireConnection;59;1;29;0
WireConnection;59;2;6;0
WireConnection;58;0;59;0
ASEEND*/
//CHKSM=03FBC8700965A7B9C6787B0DE3D69F10133DE518