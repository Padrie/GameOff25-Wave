Shader "TextMeshPro/Glimmer"
{
    Properties
    {
        [MainTexture] _MainTex("Font Atlas", 2D) = "white" {}
        [HDR] _FaceColor("Face Color", Color) = (1,1,1,1)
        
        // Glimmer Properties
        [HDR] _GlimmerColor("Glimmer Color", Color) = (1,1,1,1)
        _GlimmerSpeed("Glimmer Speed", Range(0, 5)) = 1
        _GlimmerWidth("Glimmer Width", Range(0.01, 1)) = 0.2
        _GlimmerAngle("Glimmer Angle", Range(-180, 180)) = 45
        _GlimmerSharpness("Glimmer Sharpness", Range(1, 20)) = 5
        _GlimmerIntensity("Glimmer Intensity", Range(0, 2)) = 1
        _GlimmerOffset("Glimmer Offset", Range(-2, 2)) = 0
        
        // Standard TMP Properties
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Width", Range(0, 1)) = 0
        
        _UnderlayColor("Underlay Color", Color) = (0,0,0,0.5)
        _UnderlayOffsetX("Underlay OffsetX", Range(-1,1)) = 0
        _UnderlayOffsetY("Underlay OffsetY", Range(-1,1)) = 0
        _UnderlayDilate("Underlay Dilate", Range(-1,1)) = 0
        _UnderlaySoftness("Underlay Softness", Range(0,1)) = 0

        _WeightNormal("Weight Normal", float) = 0
        _WeightBold("Weight Bold", float) = 0.5

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255

        _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest[unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]

        Pass
        {
            Name "TextMeshProGlimmer"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 screenPos : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _FaceColor;
                float4 _GlimmerColor;
                float4 _OutlineColor;
                float _OutlineWidth;
                float _GlimmerSpeed;
                float _GlimmerWidth;
                float _GlimmerAngle;
                float _GlimmerSharpness;
                float _GlimmerIntensity;
                float _GlimmerOffset;
                float _WeightNormal;
                float _WeightBold;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.color = v.color;
                o.texcoord0 = TRANSFORM_TEX(v.texcoord0, _MainTex);
                
                // Calculate screen position for glimmer effect
                float4 screenPos = ComputeScreenPos(o.vertex);
                o.screenPos = screenPos.xy / screenPos.w;
                
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Sample the font atlas
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord0);
                
                // Extract distance field value
                float distance = texColor.a;
                
                // Calculate base text alpha
                float width = _WeightNormal * 0.5;
                float alpha = saturate((distance - (0.5 - width)) / fwidth(distance) + 0.5);
                
                // Base text color
                float4 baseColor = _FaceColor * i.color;
                baseColor.a *= alpha;
                
                // Calculate glimmer effect
                float angleRad = radians(_GlimmerAngle);
                float2 direction = float2(cos(angleRad), sin(angleRad));
                
                // Rotate UV coordinates
                float2 glimmerUV = i.screenPos;
                float glimmerPos = dot(glimmerUV, direction);
                
                // Animate the glimmer
                float time = _Time.y * _GlimmerSpeed + _GlimmerOffset;
                float glimmerMask = glimmerPos - time;
                
                // Create sharp glimmer edge
                glimmerMask = frac(glimmerMask);
                glimmerMask = smoothstep(0, _GlimmerWidth, glimmerMask) * 
                              smoothstep(_GlimmerWidth * 2, _GlimmerWidth, glimmerMask);
                
                // Apply sharpness
                glimmerMask = pow(glimmerMask, 1.0 / _GlimmerSharpness);
                
                // Only apply glimmer where text exists
                glimmerMask *= alpha;
                
                // Blend glimmer with base color
                float4 finalColor = baseColor;
                finalColor.rgb += _GlimmerColor.rgb * glimmerMask * _GlimmerIntensity;
                
                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "TextMeshPro/Mobile/Distance Field"
}