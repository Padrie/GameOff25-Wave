Shader "Custom/URP_MultiShockwaves_Array"
{
    Properties
    {
        [MainColor]_BaseColor("Base Color", Color) = (1,1,1,1)
        [MainTexture]_BaseMap("Base Map", 2D) = "white" {}
        _PropagationSpeed("Propagation Speed (m/s)", Float) = 6
        _RingWidth("Ring Width (m)", Float) = 1
        _RingIntensity("Ring Intensity", Range(0,2)) = 1
        _FalloffStart("Falloff Start (m)", Float) = 5
        _FalloffEnd("Falloff End (m)", Float) = 20
        _DispAmplitude("Displacement Amplitude (m)", Float) = 0.25
        [Toggle]_DisplaceAlongUp("Displace Along World Up (off = normal)", Float) = 0
        _DispAttenK("Displacement Attenuation by Distance", Range(0,1)) = 0.25
    }

    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #define MAX_WAVES 16

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; float ringAccumV:TEXCOORD1; float falloffMaxV:TEXCOORD2; float3 posWS:TEXCOORD3; };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float4 _BaseMap_ST;
            float _PropagationSpeed;
            float _RingWidth;
            float _RingIntensity;
            float _FalloffStart;
            float _FalloffEnd;
            float _DispAmplitude;
            float _DisplaceAlongUp;
            float _DispAttenK;
            CBUFFER_END

            int    _GW_WaveCount;
            float4 _GW_WaveData[MAX_WAVES];
            float  _GW_WaveLife[MAX_WAVES];

            float RingNoAA(float r, float r0, float width)
            {
                float h = max(width * 0.5, 1e-5);
                float inner = smoothstep(r0 - h, r0, r);
                float outer = 1.0 - smoothstep(r0, r0 + h, r);
                return saturate(inner * outer);
            }

            float RingAA(float r, float r0, float width)
            {
                float h = max(width * 0.5, 1e-5);
                float pix = fwidth(r);
                h = max(h, pix * 1.5);
                float inner = smoothstep(r0 - h, r0, r);
                float outer = 1.0 - smoothstep(r0, r0 + h, r);
                return saturate(inner * outer);
            }

            void AccumulateV(float3 posWS, out float ringSum, out float falloffMax, out float rStrong)
            {
                ringSum = 0; falloffMax = 0; rStrong = 0;
                [loop] for (int i = 0; i < _GW_WaveCount && i < MAX_WAVES; i++)
                {
                    float3 origin = _GW_WaveData[i].xyz;
                    float  startT = _GW_WaveData[i].w;
                    float  life   = _GW_WaveLife[i];
                    float age = max(0.0, _Time.y - startT);
                    if (life <= 1e-5 || age > life) continue;
                    float2 p = posWS.xz - origin.xz;
                    float  r = length(p);
                    float  r0 = age * _PropagationSpeed;
                    float  ring = RingNoAA(r, r0, _RingWidth) * _RingIntensity;
                    float  spatial = 1 - smoothstep(_FalloffStart, _FalloffEnd, r);
                    float  alive = saturate(1 - age / max(life, 1e-5));
                    float  f = spatial * alive;
                    ringSum += ring * f;
                    if (f > falloffMax) { falloffMax = f; rStrong = r; }
                }
                ringSum = saturate(ringSum);
            }

            void AccumulateF(float3 posWS, out float ringSum, out float falloffMax)
            {
                ringSum = 0; falloffMax = 0;
                [loop] for (int i = 0; i < _GW_WaveCount && i < MAX_WAVES; i++)
                {
                    float3 origin = _GW_WaveData[i].xyz;
                    float  startT = _GW_WaveData[i].w;
                    float  life   = _GW_WaveLife[i];
                    float age = max(0.0, _Time.y - startT);
                    if (life <= 1e-5 || age > life) continue;
                    float2 p = posWS.xz - origin.xz;
                    float  r = length(p);
                    float  r0 = age * _PropagationSpeed;
                    float  ring = RingAA(r, r0, _RingWidth) * _RingIntensity;
                    float  spatial = 1 - smoothstep(_FalloffStart, _FalloffEnd, r);
                    float  alive = saturate(1 - age / max(life, 1e-5));
                    float  f = spatial * alive;
                    ringSum += ring * f;
                    falloffMax = max(falloffMax, f);
                }
                ringSum = saturate(ringSum);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float ringSumV, falloffMaxV, rStrong;
                AccumulateV(posWS, ringSumV, falloffMaxV, rStrong);
                float disp = (ringSumV * 2 - 1) * 0.5 * _DispAmplitude * falloffMaxV;
                float atten = 1.0 / (1.0 + _DispAttenK * rStrong);
                disp *= atten;
                float3 dirWS = (_DisplaceAlongUp > 0.5) ? float3(0,1,0) : normalize(TransformObjectToWorldNormal(IN.normalOS));
                posWS += dirWS * disp;
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.ringAccumV = ringSumV;
                OUT.falloffMaxV = falloffMaxV;
                OUT.posWS = posWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float ringSum, falloffMax;
                AccumulateF(IN.posWS, ringSum, falloffMax);
                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                float  brightness = 1 + ringSum * falloffMax;
                float3 rgb = baseCol.rgb * brightness;
                float  alpha = baseCol.a * falloffMax;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
