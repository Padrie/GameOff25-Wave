Shader "UI/SlickButton"
{
    Properties
    {
        [Header(Main)]
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        [Header(Gradient)]
        [Toggle] _UseGradient ("Use Gradient", Float) = 1
        _GradientTop ("Gradient Top", Color) = (1,1,1,1)
        _GradientBottom ("Gradient Bottom", Color) = (0.8,0.8,0.8,1)
        _GradientAngle ("Gradient Angle", Range(0, 360)) = 90
        _GradientOffset ("Gradient Offset", Range(-1, 1)) = 0
        
        [Header(Border)]
        [Toggle] _UseBorder ("Use Border", Float) = 1
        _BorderColor ("Border Color", Color) = (0,0,0,1)
        _BorderWidth ("Border Width", Range(0, 0.5)) = 0.02
        _BorderSmoothness ("Border Smoothness", Range(0, 0.1)) = 0.01
        
        [Header(Inner Glow)]
        [Toggle] _UseInnerGlow ("Use Inner Glow", Float) = 0
        _InnerGlowColor ("Inner Glow Color", Color) = (1,1,1,0.5)
        _InnerGlowWidth ("Inner Glow Width", Range(0, 0.3)) = 0.05
        _InnerGlowSoftness ("Inner Glow Softness", Range(0, 0.2)) = 0.05
        
        [Header(Shadow)]
        [Toggle] _UseShadow ("Use Shadow", Float) = 1
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0.5)
        _ShadowOffset ("Shadow Offset", Vector) = (0.02, -0.02, 0, 0)
        _ShadowBlur ("Shadow Blur", Range(0, 0.1)) = 0.03
        
        [Header(Rounded Corners)]
        _CornerRadius ("Corner Radius", Range(0, 0.5)) = 0.1
        _CornerSmoothness ("Corner Smoothness", Range(0, 0.1)) = 0.01
        
        [Header(Hover Effect)]
        [Toggle] _UseHoverEffect ("Use Hover Effect", Float) = 1
        _HoverBrightness ("Hover Brightness", Range(0, 2)) = 1.2
        _HoverProgress ("Hover Progress", Range(0, 1)) = 0
        
        [Header(Shine Effect)]
        [Toggle] _UseShine ("Use Shine", Float) = 1
        _ShineColor ("Shine Color", Color) = (1,1,1,0.3)
        _ShineAngle ("Shine Angle", Range(-180, 180)) = 45
        _ShineWidth ("Shine Width", Range(0, 1)) = 0.2
        _ShineSpeed ("Shine Speed", Float) = 1
        _ShineOffset ("Shine Offset", Range(-2, 2)) = 0
        
        [Header(Pulse Effect)]
        [Toggle] _UsePulse ("Use Pulse", Float) = 0
        _PulseSpeed ("Pulse Speed", Float) = 2
        _PulseIntensity ("Pulse Intensity", Range(0, 1)) = 0.1
        
        [Header(UI Settings)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            Name "Default"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 screenPos : TEXCOORD1;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _GradientTop;
                float4 _GradientBottom;
                float _GradientAngle;
                float _GradientOffset;
                float4 _BorderColor;
                float _BorderWidth;
                float _BorderSmoothness;
                float4 _InnerGlowColor;
                float _InnerGlowWidth;
                float _InnerGlowSoftness;
                float4 _ShadowColor;
                float4 _ShadowOffset;
                float _ShadowBlur;
                float _CornerRadius;
                float _CornerSmoothness;
                float _HoverBrightness;
                float _HoverProgress;
                float4 _ShineColor;
                float _ShineAngle;
                float _ShineWidth;
                float _ShineSpeed;
                float _ShineOffset;
                float _PulseSpeed;
                float _PulseIntensity;
                float _UseGradient;
                float _UseBorder;
                float _UseInnerGlow;
                float _UseShadow;
                float _UseHoverEffect;
                float _UseShine;
                float _UsePulse;
            CBUFFER_END
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.screenPos = v.uv;
                return o;
            }
            
            // SDF for rounded rectangle
            float sdRoundedBox(float2 p, float2 size, float radius)
            {
                float2 d = abs(p) - size + radius;
                return min(max(d.x, d.y), 0.0) + length(max(d, 0.0)) - radius;
            }
            
            // Rotate UV coordinates
            float2 rotateUV(float2 uv, float angle)
            {
                float rad = radians(angle);
                float s = sin(rad);
                float c = cos(rad);
                float2x2 rotMatrix = float2x2(c, -s, s, c);
                uv -= 0.5;
                uv = mul(rotMatrix, uv);
                uv += 0.5;
                return uv;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.screenPos;
                float2 centered = (uv - 0.5) * 2.0;
                
                // Aspect ratio correction (approximate)
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                centered.x *= aspectRatio;
                
                // Calculate rounded rectangle SDF
                float2 size = float2(aspectRatio, 1.0);
                float dist = sdRoundedBox(centered, size, _CornerRadius * 2.0);
                
                // Main shape mask
                float shapeMask = 1.0 - smoothstep(-_CornerSmoothness, _CornerSmoothness, dist);
                
                // Shadow
                float4 shadowCol = float4(0, 0, 0, 0);
                if (_UseShadow > 0.5)
                {
                    float2 shadowUV = centered - _ShadowOffset.xy * 2.0;
                    float shadowDist = sdRoundedBox(shadowUV, size, _CornerRadius * 2.0);
                    float shadowMask = 1.0 - smoothstep(-_ShadowBlur, _ShadowBlur, shadowDist);
                    shadowCol = _ShadowColor * shadowMask * (1.0 - shapeMask);
                }
                
                // Base color with gradient
                float4 baseColor = _Color * i.color;
                
                if (_UseGradient > 0.5)
                {
                    float2 gradUV = rotateUV(uv, _GradientAngle);
                    float gradientMask = gradUV.y + _GradientOffset;
                    baseColor *= lerp(_GradientBottom, _GradientTop, gradientMask);
                }
                
                // Texture
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                baseColor *= texColor;
                
                // Pulse effect
                if (_UsePulse > 0.5)
                {
                    float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                    baseColor.rgb += pulse * _PulseIntensity;
                }
                
                // Hover effect
                if (_UseHoverEffect > 0.5)
                {
                    float hoverMult = lerp(1.0, _HoverBrightness, _HoverProgress);
                    baseColor.rgb *= hoverMult;
                }
                
                // Inner glow
                if (_UseInnerGlow > 0.5)
                {
                    float innerDist = abs(dist);
                    float innerGlow = smoothstep(_InnerGlowWidth + _InnerGlowSoftness, _InnerGlowWidth - _InnerGlowSoftness, innerDist);
                    innerGlow *= shapeMask;
                    baseColor.rgb = lerp(baseColor.rgb, _InnerGlowColor.rgb, innerGlow * _InnerGlowColor.a);
                }
                
                // Shine effect
                if (_UseShine > 0.5)
                {
                    float2 shineUV = rotateUV(uv, _ShineAngle);
                    float shinePos = _ShineOffset + frac(_Time.y * _ShineSpeed * 0.1) * 2.0 - 1.0;
                    float shine = 1.0 - smoothstep(0.0, _ShineWidth, abs(shineUV.x + shineUV.y - shinePos));
                    baseColor.rgb += _ShineColor.rgb * shine * _ShineColor.a * shapeMask;
                }
                
                // Border
                if (_UseBorder > 0.5)
                {
                    float borderDist = abs(dist);
                    float borderMask = smoothstep(_BorderWidth + _BorderSmoothness, _BorderWidth - _BorderSmoothness, borderDist);
                    borderMask *= shapeMask;
                    baseColor.rgb = lerp(_BorderColor.rgb, baseColor.rgb, borderMask);
                    baseColor.a = lerp(_BorderColor.a, baseColor.a, borderMask);
                }
                
                // Apply shape mask
                baseColor.a *= shapeMask;
                
                // Composite with shadow
                float4 finalColor = lerp(shadowCol, baseColor, baseColor.a);
                finalColor.a = max(shadowCol.a, baseColor.a);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}
