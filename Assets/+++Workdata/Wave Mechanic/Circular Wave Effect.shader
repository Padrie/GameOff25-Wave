Shader "Custom/NewUnlitUniversalRenderPipelineShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        //Ripple controls
        _WaveOriginWS("Wave Origin (World)", Vector) = (0,0,0,0)
        _Amplitude("Amplitude (brightness)", Range(0,1)) = 0.5
        _Frequency("Frequency", Float) = 1.0
        _Speed("Speed", Float) = 1.0

        //Falloff
        _FalloffStart("Falloff Start (m)", Float) = 5.0
        _FalloffEnd("Falloff End (m)", Float) = 20.0

        //Vertex displacement
        _DispAmplitude("Displacement Amplitude (m)", Float) = 0.25
        [Toggle] _DisplaceAlongUp("Displace Along World Up (off = normal)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"    ="Transparent"
            "Queue"         ="Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float  wave01      : TEXCOORD1;
                float  falloff     : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                float4 _BaseMap_ST;

                float4 _WaveOriginWS;
                float  _Amplitude;
                float  _Frequency;
                float  _Speed;

                float  _FalloffStart;
                float  _FalloffEnd;

                float  _DispAmplitude;
                float  _DisplaceAlongUp; 
            CBUFFER_END

            void ComputeRipple(float3 posWS, out float wave01, out float falloff)
            {
                float2 p = posWS.xz;
                float2 o = _WaveOriginWS.xz;
                float  r = length(p - o);

                float t = _Time.y * _Speed;
                wave01  = 0.5 + 0.5 * sin(r * _Frequency - t);

                falloff = 1.0 - smoothstep(_FalloffStart, _FalloffEnd, r);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);

                float wave01, falloff;
                ComputeRipple(posWS, wave01, falloff);

                float disp = (wave01 * 2.0 - 1.0) * _DispAmplitude * falloff;

                float3 dirWS = (_DisplaceAlongUp > 0.5) ?
                               float3(0,1,0) :
                               normalize(TransformObjectToWorldNormal(IN.normalOS));

                posWS += dirWS * disp;

                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.wave01      = wave01;
                OUT.falloff     = falloff;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                //brightness mod from ripple
                float brightness = (1.0 - _Amplitude) + (2.0 * _Amplitude) * IN.wave01;
                float3 rgb = baseCol.rgb * lerp(1.0, brightness, IN.falloff);

                //fade out with distance
                float alpha = baseCol.a * IN.falloff;

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
