Shader "Universal Render Pipeline/Lit with Waves"
{
    Properties
    {
        // Original Lit Shader Properties
        _WorkflowMode("WorkflowMode", Float) = 1.0
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}
        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecGlossMap("Specular", 2D) = "white" {}
        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0
        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}
        _Parallax("Scale", Range(0.005, 0.08)) = 0.005
        _ParallaxMap("Height Map", 2D) = "black" {}
        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        _DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [Header(Wave Settings)]
        [Toggle(_ENABLE_WAVES)] _EnableWaves("Enable Waves", Float) = 1.0
        
        [Header(Wave 1)]
        _Wave1Amplitude("Wave 1 Amplitude", Range(0.0, 5.0)) = 0.5
        _Wave1Wavelength("Wave 1 Wavelength", Range(0.1, 100.0)) = 10.0
        _Wave1Speed("Wave 1 Speed", Range(0.0, 10.0)) = 1.0
        _Wave1Steepness("Wave 1 Steepness", Range(0.0, 1.0)) = 0.5
        _Wave1Direction("Wave 1 Direction", Vector) = (1, 0, 0, 0)
        
        [Header(Wave 2)]
        _Wave2Amplitude("Wave 2 Amplitude", Range(0.0, 5.0)) = 0.3
        _Wave2Wavelength("Wave 2 Wavelength", Range(0.1, 100.0)) = 15.0
        _Wave2Speed("Wave 2 Speed", Range(0.0, 10.0)) = 1.2
        _Wave2Steepness("Wave 2 Steepness", Range(0.0, 1.0)) = 0.4
        _Wave2Direction("Wave 2 Direction", Vector) = (0, 1, 0, 0)
        
        [Header(Wave 3)]
        _Wave3Amplitude("Wave 3 Amplitude", Range(0.0, 5.0)) = 0.2
        _Wave3Wavelength("Wave 3 Wavelength", Range(0.1, 100.0)) = 8.0
        _Wave3Speed("Wave 3 Speed", Range(0.0, 10.0)) = 0.8
        _Wave3Steepness("Wave 3 Steepness", Range(0.0, 1.0)) = 0.3
        _Wave3Direction("Wave 3 Direction", Vector) = (1, 1, 0, 0)
        
        [Header(Wave 4)]
        _Wave4Amplitude("Wave 4 Amplitude", Range(0.0, 5.0)) = 0.15
        _Wave4Wavelength("Wave 4 Wavelength", Range(0.1, 100.0)) = 12.0
        _Wave4Speed("Wave 4 Speed", Range(0.0, 10.0)) = 1.5
        _Wave4Steepness("Wave 4 Steepness", Range(0.0, 1.0)) = 0.35
        _Wave4Direction("Wave 4 Direction", Vector) = (-1, 0.5, 0, 0)

        // Blending state
        _Surface("__surface", Float) = 0.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0
        [HideInInspector] _AddPrecomputedVelocity("_AddPrecomputedVelocity", Float) = 0.0
        [HideInInspector] _XRMotionVectorsPass("_XRMotionVectorsPass", Float) = 1.0
        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        _QueueOffset("Queue offset", Float) = 0.0
        
        [HideInInspector] _ClearCoatMask("_ClearCoatMask", Float) = 0.0
        [HideInInspector] _ClearCoatSmoothness("_ClearCoatSmoothness", Float) = 0.0
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex LitPassVertexWaves
            #pragma fragment LitPassFragment

            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            
            // Wave Feature
            #pragma shader_feature_local _ENABLE_WAVES

            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            
            //Wave parameters
            float _Wave1Amplitude, _Wave1Wavelength, _Wave1Speed, _Wave1Steepness;
            float4 _Wave1Direction; //.xyz = origin, .w = inner radius (safe zone)
            float _Wave2Amplitude, _Wave2Wavelength, _Wave2Speed, _Wave2Steepness;
            float4 _Wave2Direction;
            float _Wave3Amplitude, _Wave3Wavelength, _Wave3Speed, _Wave3Steepness;
            float4 _Wave3Direction;
            float _Wave4Amplitude, _Wave4Wavelength, _Wave4Speed, _Wave4Steepness;
            float4 _Wave4Direction;

            //Circular/Radial Wave with inner safe radius - like audio waves
            void CircularWave(float3 position, float3 origin, float amplitude, float wavelength, float speed, float steepness, float time, float innerRadius,
                            inout float3 offset, inout float3 normal)
            {
                if (amplitude < 0.001) return;
                
                //Calculate distance from origin in XZ plane (ignore Y)
                float2 toPoint = position.xz - origin.xz;
                float distance = length(toPoint);
                
                //SAFE ZONE: No deformation inside inner radius
                if (distance < innerRadius)
                {
                    return; //No wave displacement in safe zone
                }
                
                //Calculate effective distance (distance from edge of safe zone)
                float effectiveDistance = distance - innerRadius;
                
                //Normalized direction from origin to current point (radial)
                float2 direction = toPoint / distance;
                
                //Wave parameters
                float k = 2.0 * 3.14159265 / max(wavelength, 0.1);
                float c = sqrt(9.8 / k); //Phase velocity
                
                //Phase calculation: wave starts at inner radius and expands outward
                float phase = k * effectiveDistance - k * c * speed * time;
                
                //Smooth fade-in at the edge of safe zone (prevents harsh edge)
                float fadeInDistance = wavelength * 0.5; //Fade over half wavelength
                float fadeIn = smoothstep(0, fadeInDistance, effectiveDistance);
                
                //Steepness factor (controls horizontal displacement)
                float Q = steepness / k;
                
                //Wave shape
                float cosPhase = cos(phase);
                float sinPhase = sin(phase);
                
                //Apply fade-in to amplitude
                float effectiveAmplitude = amplitude * fadeIn;
                
                //Vertical displacement (main wave height)
                offset.y += effectiveAmplitude * sinPhase;
                
                //Horizontal displacement (creates the characteristic peak shape)
                float horizontalDisp = Q * effectiveAmplitude * cosPhase;
                offset.x += direction.x * horizontalDisp;
                offset.z += direction.y * horizontalDisp;
                
                //Calculate normal for proper lighting
                float wa = k * effectiveAmplitude;
                float normalFactor = Q * wa * cosPhase;
                
                normal.x -= direction.x * normalFactor;
                normal.z -= direction.y * normalFactor;
                normal.y -= Q * wa * sinPhase;
            }

            //Gerstner Wave (directional) - for ambient waves
            void GerstnerWave(float3 position, float2 direction, float amplitude, float wavelength, float speed, float steepness, float time,
                            inout float3 offset, inout float3 normal)
            {
                if (amplitude < 0.001) return;
                
                float k = 2.0 * 3.14159265 / max(wavelength, 0.1);
                float c = sqrt(9.8 / k);
                float2 d = normalize(direction + float2(0.001, 0.001));
                float f = k * (dot(d, position.xz) - c * speed * time);
                float a = steepness / k;
                
                //Position offset
                offset.x += d.x * a * amplitude * cos(f);
                offset.y += amplitude * sin(f);
                offset.z += d.y * a * amplitude * cos(f);
                
                //Normal contribution
                float wa = k * amplitude;
                float Csf = cos(f);
                float Snf = sin(f);
                
                normal.x -= d.x * wa * Csf;
                normal.z -= d.y * wa * Csf;
                normal.y -= steepness * wa * Snf;
            }

            //Include to get structures
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            
            //Replace vertex shader
            Varyings LitPassVertexWaves(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                //Start with original position
                float3 positionOS = input.positionOS.xyz;
                float3 normalOS = input.normalOS;
                
                //Transform to world space FIRST
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(normalOS);
                
                //Apply waves in world space
                float time = _Time.y;
                float3 waveOffset = float3(0, 0, 0);
                float3 waveNormal = float3(0, 1, 0);
                
                //_Wave1Direction.w contains the inner radius (safe zone)
                CircularWave(positionWS, _Wave1Direction.xyz, _Wave1Amplitude, _Wave1Wavelength, _Wave1Speed, _Wave1Steepness, time, _Wave1Direction.w, waveOffset, waveNormal);
                CircularWave(positionWS, _Wave2Direction.xyz, _Wave2Amplitude, _Wave2Wavelength, _Wave2Speed, _Wave2Steepness, time, _Wave2Direction.w, waveOffset, waveNormal);
                CircularWave(positionWS, _Wave3Direction.xyz, _Wave3Amplitude, _Wave3Wavelength, _Wave3Speed, _Wave3Steepness, time, _Wave3Direction.w, waveOffset, waveNormal);
                CircularWave(positionWS, _Wave4Direction.xyz, _Wave4Amplitude, _Wave4Wavelength, _Wave4Speed, _Wave4Steepness, time, _Wave4Direction.w, waveOffset, waveNormal);
                
                //Apply wave offset
                positionWS += waveOffset;
                
                //Normalize wave normal
                waveNormal = normalize(waveNormal);
                
                //Use wave normal
                normalWS = waveNormal;
                
                //Calculate final positions
                float4 positionCS = TransformWorldToHClip(positionWS);
                
                //Setup tangent
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                normalInput.normalWS = normalWS;

                //Lighting calculations
                half3 vertexLight = half3(0, 0, 0);
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    uint lightsCount = GetAdditionalLightsCount();
                    for (uint lightIndex = 0u; lightIndex < lightsCount; ++lightIndex)
                    {
                        Light light = GetAdditionalLight(lightIndex, positionWS);
                        half3 attenuatedLightColor = light.color * light.distanceAttenuation;
                        vertexLight += LightingLambert(attenuatedLightColor, light.direction, normalWS);
                    }
                #endif

                //Output
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.normalWS = normalWS;
                output.positionWS = positionWS;
                output.positionCS = positionCS;

                #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
                    real sign = input.tangentOS.w * GetOddNegativeScale();
                    float3 tangent = normalize(cross(float3(0, 0, 1), normalWS));
                    output.tangentWS = half4(tangent, sign);
                #endif

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                #ifdef DYNAMICLIGHTMAP_ON
                    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                
                OUTPUT_SH(normalWS, output.vertexSH);

                half fogFactor = ComputeFogFactor(positionCS.z);
                #ifdef _ADDITIONAL_LIGHTS_VERTEX
                    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
                #else
                    output.fogFactor = fogFactor;
                #endif

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    VertexPositionInputs vertexInput;
                    vertexInput.positionWS = positionWS;
                    vertexInput.positionCS = positionCS;
                    output.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                return output;
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "GBuffer"
            Tags { "LightMode" = "UniversalGBuffer" }
            ZWrite[_ZWrite]
            ZTest LEqual
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma exclude_renderers gles3 glcore
            #pragma vertex LitGBufferPassVertexWaves
            #pragma fragment LitGBufferPassFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            
            //Wave parameters for GBuffer
            float _Wave1Amplitude, _Wave1Wavelength, _Wave1Speed, _Wave1Steepness;
            float4 _Wave1Direction;
            float _Wave2Amplitude, _Wave2Wavelength, _Wave2Speed, _Wave2Steepness;
            float4 _Wave2Direction;
            float _Wave3Amplitude, _Wave3Wavelength, _Wave3Speed, _Wave3Steepness;
            float4 _Wave3Direction;
            float _Wave4Amplitude, _Wave4Wavelength, _Wave4Speed, _Wave4Steepness;
            float4 _Wave4Direction;

            void CircularWaveGBuffer(float3 position, float3 origin, float amplitude, float wavelength, float speed, float steepness, float time, float innerRadius,
                            inout float3 offset, inout float3 normal)
            {
                if (amplitude < 0.001) return;
                
                float2 toPoint = position.xz - origin.xz;
                float distance = length(toPoint);
                
                if (distance < innerRadius) return;
                
                float effectiveDistance = distance - innerRadius;
                float2 direction = toPoint / distance;
                float k = 2.0 * 3.14159265 / max(wavelength, 0.1);
                float c = sqrt(9.8 / k);
                float phase = k * effectiveDistance - k * c * speed * time;
                
                float fadeInDistance = wavelength * 0.5;
                float fadeIn = smoothstep(0, fadeInDistance, effectiveDistance);
                
                float Q = steepness / k;
                float cosPhase = cos(phase);
                float sinPhase = sin(phase);
                
                float effectiveAmplitude = amplitude * fadeIn;
                
                offset.y += effectiveAmplitude * sinPhase;
                
                float horizontalDisp = Q * effectiveAmplitude * cosPhase;
                offset.x += direction.x * horizontalDisp;
                offset.z += direction.y * horizontalDisp;
                
                float wa = k * effectiveAmplitude;
                float normalFactor = Q * wa * cosPhase;
                
                normal.x -= direction.x * normalFactor;
                normal.z -= direction.y * normalFactor;
                normal.y -= Q * wa * sinPhase;
            }
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitGBufferPass.hlsl"
            
            //Custom GBuffer vertex shader with waves
            Varyings LitGBufferPassVertexWaves(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = input.positionOS.xyz;
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                //Apply waves
                float time = _Time.y;
                float3 waveOffset = float3(0, 0, 0);
                float3 waveNormal = float3(0, 1, 0);
                
                CircularWaveGBuffer(positionWS, _Wave1Direction.xyz, _Wave1Amplitude, _Wave1Wavelength, _Wave1Speed, _Wave1Steepness, time, _Wave1Direction.w, waveOffset, waveNormal);
                CircularWaveGBuffer(positionWS, _Wave2Direction.xyz, _Wave2Amplitude, _Wave2Wavelength, _Wave2Speed, _Wave2Steepness, time, _Wave2Direction.w, waveOffset, waveNormal);
                CircularWaveGBuffer(positionWS, _Wave3Direction.xyz, _Wave3Amplitude, _Wave3Wavelength, _Wave3Speed, _Wave3Steepness, time, _Wave3Direction.w, waveOffset, waveNormal);
                CircularWaveGBuffer(positionWS, _Wave4Direction.xyz, _Wave4Amplitude, _Wave4Wavelength, _Wave4Speed, _Wave4Steepness, time, _Wave4Direction.w, waveOffset, waveNormal);
                
                positionWS += waveOffset;
                waveNormal = normalize(waveNormal);
                normalWS = waveNormal;
                
                float4 positionCS = TransformWorldToHClip(positionWS);
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                normalInput.normalWS = normalWS;

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionCS = positionCS;
                output.positionWS = positionWS;
                output.normalWS = normalWS;

                #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
                    real sign = input.tangentOS.w * GetOddNegativeScale();
                    float3 tangent = normalize(cross(float3(0, 0, 1), normalWS));
                    output.tangentWS = half4(tangent, sign);
                #endif

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                #ifdef DYNAMICLIGHTMAP_ON
                    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                #endif
                
                OUTPUT_SH(normalWS, output.vertexSH);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    VertexPositionInputs vertexInput;
                    vertexInput.positionWS = positionWS;
                    vertexInput.positionCS = positionCS;
                    output.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                return output;
            }
            
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SPECGLOSSMAP
            #pragma shader_feature EDITOR_VISUALIZATION
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Universal2D.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}