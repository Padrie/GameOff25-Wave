Shader "URP/Lit Ripple (Safe Opaque)"
{
    Properties
    {
        // Core PBR
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]  _BaseColor("Base Color", Color) = (1,1,1,1)

        _MetallicGlossMap("MetallicGloss Map", 2D) = "black" {}
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Smoothness("Smoothness", Range(0,1)) = 0.5

        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0,2)) = 1.0

        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1.0

        _EmissionColor("Emission Color", Color) = (0,0,0)
        _EmissionMap("Emission Map", 2D) = "black" {}

        // Render state (locked to opaque & backface to match default URP Lit)
        [HideInInspector]_Surface("Surface", Float) = 0.0
        [HideInInspector]_AlphaClip("Alpha Clipping", Float) = 0.0
        [HideInInspector]_Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        _Cull("Cull", Float) = 2.0

        // Ripple controls (additive feature)
        _RippleEnabled("Ripple Enabled", Float) = 1.0
        _RippleCenter("Ripple Center (World)", Vector) = (0,0,0,0)
        _RippleAmplitude("Ripple Amplitude", Float) = 0.1
        _RippleFrequency("Ripple Frequency", Float) = 6.283185
        _RippleSpeed("Ripple Speed", Float) = 1.25
        _RippleRadius("Ripple Max Radius", Float) = 6.0
        _RippleVertical("Ripple Axis (0=X,1=Y,2=Z)", Float) = 1.0

        _WaveTime("Wave Time", Float) = 0.0
        _WaveDecay("Wave Decay", Range(0,10)) = 2.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "UniversalMaterialType"="Lit" }
        LOD 300
        Cull [_Cull]

        HLSLINCLUDE
        #pragma target 3.5

        // Ensure texture ST vectors are declared so all passes (including ShadowCaster/DepthOnly)
        // that compile separately can reference TRANSFORM_TEX and _ST variables.
        float4 _BaseMap_ST;
        float4 _MetallicGlossMap_ST;
        float4 _BumpMap_ST;
        float4 _OcclusionMap_ST;
        float4 _EmissionMap_ST;

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

        // Keep all stock textures from SurfaceInput.hlsl. Only our params go here.
        CBUFFER_START(RippleParams)
            half    _RippleEnabled;
            float3  _RippleCenter;
            half    _RippleAmplitude;
            half    _RippleFrequency;
            half    _RippleSpeed;
            half    _RippleRadius;
            half    _RippleVertical;
            half    _WaveTime;
            half    _WaveDecay;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float4 tangentOS  : TANGENT;
            float2 uv0        : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS   : TEXCOORD1;
            float4 tangentWS  : TEXCOORD2;
            float2 uv0        : TEXCOORD3;
            float3 viewDirWS  : TEXCOORD4;
            half   fogCoord   : TEXCOORD5;
            float4 shadowCoord: TEXCOORD6;
        };

        // Minimal ripple (object-space in/out, world-space radial on XZ plane)
        float3 ApplyRipple(float3 positionOS)
        {
            if (_RippleEnabled <= 0.5) return positionOS;

            float3 positionWS = TransformObjectToWorld(positionOS);
            float  r = length((positionWS - _RippleCenter).xz);
            if (_RippleRadius > 1e-4)
            {
                float falloff = saturate(1.0 - r / _RippleRadius);
                float t = (_Time.y + _WaveTime);
                float phase = _RippleFrequency * r - _RippleSpeed * t * _RippleFrequency;
                float amp = _RippleAmplitude * falloff * exp(-_WaveDecay * (1.0 - falloff));
                float offset = amp * sin(phase);

                if (_RippleVertical < 0.5)      positionWS.x += offset;
                else if (_RippleVertical < 1.5) positionWS.y += offset;
                else                             positionWS.z += offset;

                positionOS = TransformWorldToObject(positionWS);
            }
            return positionOS;
        }

        inline half3 SampleWSNormal(float2 uv, float3 nWS, float3 tWS, float3 bWS)
        {
        #ifdef _NORMALMAP
            half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
            float3x3 TBN = float3x3(tWS, bWS, nWS);
            return normalize(mul(nTS, TBN));
        #else
            return normalize(nWS);
        #endif
        }

        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            float3 displacedOS = ApplyRipple(IN.positionOS.xyz);

            VertexPositionInputs posInputs = GetVertexPositionInputs(displacedOS);
            VertexNormalInputs   normInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

            OUT.positionCS = posInputs.positionCS;
            OUT.positionWS = posInputs.positionWS;
            OUT.normalWS   = NormalizeNormalPerPixel(normInputs.normalWS);
            OUT.tangentWS  = float4(normInputs.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
            OUT.uv0        = TRANSFORM_TEX(IN.uv0, _BaseMap);
            OUT.viewDirWS  = GetWorldSpaceViewDir(posInputs.positionWS);
            OUT.fogCoord   = ComputeFogFactor(posInputs.positionCS.z);
        #if defined(MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
            OUT.shadowCoord = TransformWorldToShadowCoord(posInputs.positionWS);
        #endif
            return OUT;
        }

        half4 frag(Varyings IN) : SV_Target
        {
            // Albedo
            half4 albedo = _BaseColor * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv0);

            // Build TBN & Normal
            float3 t = normalize(IN.tangentWS.xyz);
            float3 n = normalize(IN.normalWS);
            float3 b = normalize(cross(n, t)) * IN.tangentWS.w;
            half3 normalWS = SampleWSNormal(IN.uv0, n, t, b);

            // Metallic/Smoothness
            half metallic = _Metallic;
            half smooth   = _Smoothness;
        #if defined(_METALLICSPECGLOSSMAP)
            half4 mg = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, IN.uv0);
            metallic = saturate(mg.r);
            smooth   = saturate(mg.a * _Smoothness);
        #endif

            // Occlusion
            half occlusion = 1.0;
        #if defined(_OCCLUSIONMAP)
            occlusion = lerp(1.0, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, IN.uv0).g, _OcclusionStrength);
        #endif

            // Emission
            half3 emission = _EmissionColor;
        #if defined(_EMISSION)
            emission *= SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv0).rgb;
        #endif

            SurfaceData surfaceData;
            surfaceData.albedo = albedo.rgb;
            surfaceData.metallic = metallic;
            surfaceData.specular = 0;
            surfaceData.smoothness = smooth;
            surfaceData.normalTS = half3(0,0,1);
            surfaceData.occlusion = occlusion;
            surfaceData.emission = emission;
            surfaceData.alpha = 1; // force opaque to match default Lit visibility
            surfaceData.clearCoatMask = 0;
            surfaceData.clearCoatSmoothness = 0;

            InputData lightingInput;
            lightingInput.positionWS = IN.positionWS;
            lightingInput.normalWS   = normalWS;
            lightingInput.viewDirectionWS = normalize(IN.viewDirWS);
        #if defined(MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
            lightingInput.shadowCoord = IN.shadowCoord;
        #else
            lightingInput.shadowCoord = float4(0,0,0,0);
        #endif
            lightingInput.fogCoord = IN.fogCoord;
            lightingInput.vertexLighting = half3(0,0,0);
            lightingInput.bakedGI = SampleSH(normalize(normalWS));
            lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
            lightingInput.shadowMask = 1;

            half4 color = UniversalFragmentPBR(lightingInput, surfaceData);
            color.rgb = MixFog(color.rgb, lightingInput.fogCoord);
            return color;
        }
        ENDHLSL

        // Forward Lit (Opaque, default URP states)
        Pass
        {
            Name "ForwardLit"
            Tags{ "LightMode"="UniversalForward" }
            Blend One Zero
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
                #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
                #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
                #pragma multi_compile _ _SHADOWS_SOFT
                #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
                #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
                #pragma multi_compile _ _LIGHT_COOKIES
                #pragma multi_compile _ DIRLIGHTMAP_COMBINED _ LIGHTMAP_ON
                #pragma multi_compile _ DYNAMICLIGHTMAP_ON
                #pragma multi_compile _ _REFLECTION_PROBE_BLENDING _REFLECTION_PROBE_BOX_PROJECTION
                #pragma multi_compile _ _DEFERRED_REFLECTIONS
                #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION

                #pragma multi_compile _ _NORMALMAP
                #pragma multi_compile _ _METALLICSPECGLOSSMAP
                #pragma multi_compile _ _OCCLUSIONMAP
                #pragma multi_compile _ _EMISSION

                #pragma multi_compile_fog
                #pragma instancing_options procedural:setup unity_instancing_enabled
            ENDHLSL
        }

        // ShadowCaster (displaced)
        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
                #pragma vertex ShadowPassVertex
                #pragma fragment ShadowPassFragment
                #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

                float3 ApplyRipple(float3 positionOS);

                VertexOutput ShadowPassVertex(Attributes input)
                {
                    float3 posOS = ApplyRipple(input.positionOS.xyz);
                    VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                    VertexOutput o;
                    o.positionCS = posInputs.positionCS;
                    return o;
                }
            ENDHLSL
        }

        // DepthOnly (displaced)
        Pass
        {
            Name "DepthOnly"
            Tags{ "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
                #pragma vertex DepthOnlyVertex
                #pragma fragment DepthOnlyFragment
                #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

                float3 ApplyRipple(float3 positionOS);

                VaryingsDepthOnly DepthOnlyVertex(Attributes input)
                {
                    VaryingsDepthOnly o;
                    float3 posOS = ApplyRipple(input.positionOS.xyz);
                    VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                    o.positionCS = posInputs.positionCS;
                    return o;
                }
            ENDHLSL
        }
    }
    FallBack Off
}