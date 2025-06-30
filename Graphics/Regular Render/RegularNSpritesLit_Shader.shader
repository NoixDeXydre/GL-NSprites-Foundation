Shader "Universal Render Pipeline/2D/NSpritesShaderLit"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);

#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    StructuredBuffer<int> _propertyPointers;
    StructuredBuffer<float4> _uvTilingAndOffsetBuffer;
    StructuredBuffer<float4> _uvAtlasBuffer;
    StructuredBuffer<int2> _flipBuffer;
    StructuredBuffer<float4x4> _positionBuffer;
    StructuredBuffer<float2> _pivotBuffer;
    StructuredBuffer<float2> _heightWidthBuffer;
#endif

    float4x4 offset_matrix(const float2 input, const float2 scale)
    {
        return float4x4(
            scale.x, 0, 0, scale.x * -input.x,
            0, scale.y, 0, scale.y * -input.y,
            0, 0, 1, 0,
            0, 0, 0, 1
        );
    }

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
        float3 normalWS : NORMAL;
        float3 positionWS : TEXCOORD1;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    float _Smoothness;
    float _Metallic;

    Varyings VertexLit(Attributes IN, uint instanceID : SV_InstanceID)
    {
        Varyings OUT = (Varyings)0;

        UNITY_SETUP_INSTANCE_ID(IN);
        UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

        int propertyIndex = _propertyPointers[instanceID];
        float4x4 transform = _positionBuffer[propertyIndex];
        float2 pivot = _pivotBuffer[propertyIndex];
        float2 scale = _heightWidthBuffer[propertyIndex];
        int2 flipValue = _flipBuffer[propertyIndex];

        float4x4 localToWorld = mul(transform, offset_matrix(pivot, scale));

        // Calcul position monde et clip
        float4 posWS = mul(localToWorld, float4(IN.positionOS, 1.0));
        OUT.positionCS = TransformWorldToHClip(posWS.xyz);
        OUT.positionWS = posWS.xyz;

        // Normale fixe face caméra
        float3 normalOS = float3(0, 0, -1);
        OUT.normalWS = normalize(mul((float3x3)localToWorld, normalOS));

        // UV avec flip et tiling
        float4 uvTilingAndOffset = _uvTilingAndOffsetBuffer[propertyIndex];
        float2 uv = IN.uv;
        uv.x = flipValue.x >= 0 ? uv.x : (1.0 - uv.x);
        uv.y = flipValue.y >= 0 ? uv.y : (1.0 - uv.y);
        OUT.uv = uv * uvTilingAndOffset.xy + uvTilingAndOffset.zw;

        return OUT;
    }

    float4 FragmentLit(Varyings IN, uint instanceID : SV_InstanceID) : SV_Target
    {
        int propertyIndex = _propertyPointers[instanceID];
        float4 uvAtlas = _uvAtlasBuffer[propertyIndex];

        // UV atlas
        float2 uv = frac(IN.uv);
        uv = uv * uvAtlas.xy + uvAtlas.zw;

        float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
        clip(albedo.a - 0.5);

        // Récupère la lumière principale URP
        Light mainLight = GetMainLight();

        // Calcul simple éclairage direct (diffuse)
        float3 N = normalize(IN.normalWS);
        float3 L = normalize(mainLight.direction);
        float NdotL = saturate(dot(N, -L));

        // Calcul PBR simplifié (albedo * NdotL)
        float3 diffuse = albedo.rgb * mainLight.color.rgb * NdotL;

        // Ici on simplifie, pas de speculaire ou GI pour le moment
        return float4(diffuse, albedo.a);
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

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex VertexLit
            #pragma fragment FragmentLit
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
