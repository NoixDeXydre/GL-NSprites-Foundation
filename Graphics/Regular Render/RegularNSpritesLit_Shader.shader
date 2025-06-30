// To access properties data shader uses StructuredBuffer<T> down below

// If you use EachUpdate mode then just access _bufferName[instanceID] (instanceID may be named differently)

// If you use Reactive / Static mode then for such properties you firstly need to obtain instance provided by NSprites system
// to do that you need to access _propertyPointers[instanceID] and use it like it is your actual instance id, so
// any Reactive / Static properties should be accessed like:
// int pointer = _propertyPointers[instanceID];
// float propertyValue _propertyNameBuffer[pointer]; // float type is here just for example

// NOTE: some graphics API have problems with how NSprites updates Reactive / Static properties, if you encountered such situation,
// then try to use only EachUpdate mode and access buffers like it described in first section

Shader "Universal Render Pipeline/2D/NSpritesShaderLit"
{
    Properties
    {
        _MainTex("_MainTex", 2D) = "white" {}
        _Color("Color Tint", Color) = (1,1,1,1)
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // for lighting functions

    CBUFFER_START(UnityPerMaterial)
    float4 _Color;
    CBUFFER_END
    ENDHLSL

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue" = "Transparent" "RenderType" = "Transparent" }
            ZTest LEqual
            ZWrite Off // Transparent usually no zwrite
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float3 normalWS : NORMAL; // world space normal for lighting
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
            StructuredBuffer<int> _propertyPointers;
            StructuredBuffer<float4> _uvTilingAndOffsetBuffer;
            StructuredBuffer<float4> _uvAtlasBuffer;
            StructuredBuffer<int2> _sortingDataBuffer;
            StructuredBuffer<float4x4> _positionBuffer;
            StructuredBuffer<float2> _pivotBuffer;
            StructuredBuffer<float2> _heightWidthBuffer;
            StructuredBuffer<int2> _flipBuffer;
#endif

            float4 _sortingGlobalData;

            float4x4 offset_matrix(const float2 input, const float2 scale)
            {
                return float4x4(
                    scale.x,0,0,scale.x * -input.x,
                    0,scale.y,0,scale.y * -input.y,
                    0,0,1,0,
                    0,0,0,1
                );
            }

            void setup()
            {
#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                int propertyIndex = _propertyPointers[unity_InstanceID];
                float4x4 transform = _positionBuffer[propertyIndex];
                float2 pivot = _pivotBuffer[propertyIndex];
                float2 scale = _heightWidthBuffer[propertyIndex];
                unity_ObjectToWorld = mul(transform, offset_matrix(pivot, scale));
#endif
            }

            Varyings Vert(Attributes attributes, uint instanceID : SV_InstanceID)
            {
                Varyings varyings = (Varyings)0;

#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                int propertyIndex = _propertyPointers[instanceID];
                float4 uvTilingAndOffset = _uvTilingAndOffsetBuffer[propertyIndex];
                int2 sortingData = _sortingDataBuffer[propertyIndex];
                int2 flipValue = _flipBuffer[propertyIndex];
#else
                float4 uvTilingAndOffset = float4(1, 1, 0, 0);
                int2 sortingData = int2(0, 0);
                int2 flipValue = int2(0, 0);
#endif

                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_TRANSFER_INSTANCE_ID(attributes, varyings);

                // Flip UVs
                attributes.uv.x = flipValue.x >= 0 ? attributes.uv.x : (1.0 - attributes.uv.x);
                attributes.uv.y = flipValue.y >= 0 ? attributes.uv.y : (1.0 - attributes.uv.y);

                varyings.positionCS = TransformObjectToHClip(attributes.positionOS);

                varyings.positionCS.z =
                    sortingData.x * _sortingGlobalData.x
                    + sortingData.y * _sortingGlobalData.y
                    + _sortingGlobalData.y * saturate((varyings.positionCS.y / varyings.positionCS.w + 1) * 0.5);

                // Tiling and offset UV
                varyings.uv = attributes.uv * uvTilingAndOffset.xy + uvTilingAndOffset.zw;

                // Normal in world space, sprite faces camera on XY plane, normal is forward Z
                float3 normalOS = float3(0, 0, -1);
                varyings.normalWS = normalize(mul((float3x3)unity_ObjectToWorld, normalOS));

                return varyings;
            }

            float4 Frag(Varyings varyings, uint instanceID : SV_InstanceID) : SV_Target
            {
#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                int propertyIndex = _propertyPointers[instanceID];
                float4 uvAtlas = _uvAtlasBuffer[propertyIndex];
#else
                float4 uvAtlas = float4(1, 1, 0, 0);
#endif

                varyings.uv = frac(varyings.uv) * uvAtlas.xy + uvAtlas.zw;

                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, varyings.uv);

                clip(texColor.a - 0.5);

                // --- Simple URP lighting ---

                // Get main directional light data
                Light mainLight = GetMainLight();
                float3 lightDir = -mainLight.direction; // light direction to surface
                float3 normal = normalize(varyings.normalWS);

                // Lambert diffuse term
                float NdotL = saturate(dot(normal, lightDir));
                float3 diffuseLighting = mainLight.color.rgb * NdotL;

                // Modulate texColor by lighting and _Color tint
                float3 finalColor = texColor.rgb * diffuseLighting * _Color.rgb;
                float alpha = texColor.a * _Color.a;

                return float4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}