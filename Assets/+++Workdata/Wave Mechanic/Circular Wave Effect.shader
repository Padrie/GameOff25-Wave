Shader "Custom/NewUnlitUniversalRenderPipelineShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        _WaveOriginWS("Wave Origin (World)", Vector) = (0,0,0,0)
        _WaveLifetime("Wave Lifetime (s)", Float) = 1.0
        [HideInInspector]_WaveStartTime("Wave Start Time (s)", Float) = 0.0

        _PropagationSpeed("Propagation Speed (m/s)", Float) = 6.0
        _RingWidth("Ring Width (m)", Float) = 1.0     
        _RingIntensity("Ring Intensity", Range(0,2)) = 1.0

        _FalloffStart("Falloff Start (m)", Float) = 5.0
        _FalloffEnd("Falloff End (m)", Float) = 20.0

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
                float  ring        : TEXCOORD1;
                float  falloff     : TEXCOORD2; 
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                float4 _BaseMap_ST;

                float4 _WaveOriginWS;

                float  _WaveLifetime;  
                float  _WaveStartTime;  

                float  _PropagationSpeed; 
                float  _RingWidth;      
                float  _RingIntensity;

                float  _FalloffStart;
                float  _FalloffEnd;

                float  _DispAmplitude;
                float  _DisplaceAlongUp;
            CBUFFER_END

            float RingProfile(float r, float r0, float width)
            {
                float halfW = max(width * 0.5, 1e-5);
                float inner = smoothstep(r0 - halfW, r0, r);
                float outer = 1.0 - smoothstep(r0, r0 + halfW, r);
                return saturate(inner * outer);
            }

            void ComputeShock(float3 posWS, out float ring, out float falloff)
            {
                float2 p = posWS.xz;
                float2 o = _WaveOriginWS.xz;
                float  r = length(p - o);

                float age = max(0.0, _Time.y - _WaveStartTime);

                float r0 = age * _PropagationSpeed;

                ring = RingProfile(r, r0, _RingWidth) * _RingIntensity;

                float spatial = 1.0 - smoothstep(_FalloffStart, _FalloffEnd, r);

                float life  = max(_WaveLifetime, 1e-5);
                float alive = saturate(1.0 - age / life);

                falloff = spatial * alive;

                if (alive <= 0.0) ring = 0.0;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);

                float ring, falloff;
                ComputeShock(posWS, ring, falloff);

                float disp = (ring * 2.0 - 1.0) * 0.5 * _DispAmplitude * falloff;

                float3 dirWS = (_DisplaceAlongUp > 0.5) ?
                               float3(0,1,0) :
                               normalize(TransformObjectToWorldNormal(IN.normalOS));

                posWS += dirWS * disp;

                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.ring        = ring;
                OUT.falloff     = falloff;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                float brightness = 1.0 + IN.ring * IN.falloff;
                float3 rgb = baseCol.rgb * brightness;

                float alpha = baseCol.a * IN.falloff;

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
