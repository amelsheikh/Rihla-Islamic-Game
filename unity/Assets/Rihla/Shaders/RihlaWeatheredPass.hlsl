// The forward pass of "Rihla/Weathered Stone": URP's Lit forward pass, with the stone's colour weathered in world
// space before lighting, so no two stretches of wall or paving look the same:
//  - large and small patches of lighter and darker stone (breaks up the 1K texture's repeat),
//  - a slow change of hue from area to area (warmer ochre, cooler grey),
//  - faint dark streaks running down the walls where rain has run for years,
//  - grime and damp at the foot of walls, within about 1.5 m of the ground.
// Only the colour (and a little smoothness) changes; lighting, shadows, fog and SSAO are URP's own.
#ifndef RIHLA_WEATHERED_PASS_INCLUDED
#define RIHLA_WEATHERED_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

TEXTURE2D(_RihlaNoise); SAMPLER(sampler_RihlaNoise);

half4 RihlaNoise(float2 p) { return SAMPLE_TEXTURE2D(_RihlaNoise, sampler_RihlaNoise, p); }

void RihlaWeather(inout SurfaceData s, float3 pw, half3 nw)
{
#if defined(_RIHLA_GROUND)
    float2 p = pw.xz;
    // Two reads only (integrated graphics): large patches, and medium patches with fine grain.
    half big = RihlaNoise(p * 0.017).r; half4 m = RihlaNoise(p * 0.093 + 0.31); half mid = m.g;
    s.albedo *= 1.0 + (big - 0.5) * 0.34 + (mid - 0.5) * 0.2 + (m.a - 0.5) * 0.08;
    // Worn, darker and browner patches where feet and carts have passed for centuries.
    half worn = saturate((mid - 0.56) * 3.0) * saturate((big - 0.35) * 2.0);
    s.albedo = lerp(s.albedo, s.albedo * half3(0.86, 0.8, 0.72), worn);
    s.smoothness = saturate(s.smoothness + worn * 0.12);
#else
    // Along the wall: whichever horizontal axis the wall runs along; roof tops use the ground plane.
    float along = abs(nw.x) > abs(nw.z) ? pw.z : pw.x;
    float2 p = abs(nw.y) > 0.7 ? pw.xz : float2(along, pw.y);
    // Jerusalem limestone is pale cream, paler and less orange than the photographed sandstone textures.
    half luma = dot(s.albedo, half3(0.3, 0.59, 0.11));
    s.albedo = lerp(luma * half3(1.08, 1.0, 0.87), s.albedo, 0.68) * 1.12;
    // Three reads at most (integrated graphics): large patches and hue, medium patches, rain streaks.
    half4 b4 = RihlaNoise(p * float2(0.031, 0.047)); half big = b4.r, mid = RihlaNoise(p * 0.12 + 0.17).g;
    s.albedo *= 1.0 + (big - 0.5) * 0.3 + (mid - 0.5) * 0.16;
    // Hue changes from wall to wall: honey-coloured stone here, greyer stone there.
    half hue = b4.b;
    s.albedo *= lerp(half3(1.05, 0.99, 0.9), half3(0.94, 0.96, 0.99), smoothstep(0.3, 0.7, hue));
    if (abs(nw.y) < 0.5)
    {
        // Rain streaks: noise stretched down the wall, stronger near the top where water runs off.
        half streak = RihlaNoise(float2(along * 0.55, pw.y * 0.018)).b;
        s.albedo *= 1.0 - saturate((streak - 0.56) * 3.0) * 0.2;
        // Grime and damp at the foot of the wall, with a ragged upper edge.
        half foot = saturate(1.0 - (pw.y - 0.05) / (1.0 + mid * 0.9));
        foot *= foot;
        s.albedo = lerp(s.albedo, s.albedo * half3(0.6, 0.54, 0.46), foot * 0.65);
        s.smoothness *= 1.0 - foot * 0.5;
    }
#endif
}

void RihlaWeatheredFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);
    RihlaWeather(surfaceData, input.positionWS, normalize(input.normalWS));

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

#if defined(_DBUFFER)
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

    InitializeBakedGIData(input, inputData);

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent());

    outColor = color;

#ifdef _WRITE_RENDERING_LAYERS
    outRenderingLayers = EncodeMeshRenderingLayer();
#endif
}

#endif
