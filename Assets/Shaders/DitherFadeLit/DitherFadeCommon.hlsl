#ifndef PROJECTKM_DITHER_FADE_COMMON_INCLUDED
#define PROJECTKM_DITHER_FADE_COMMON_INCLUDED

inline half ProjectKM_Dither4x4(float2 positionCS)
{
    uint x = (uint)positionCS.x & 3u;
    uint y = (uint)positionCS.y & 3u;
    uint index = x + (y << 2u);

    const half threshold[16] =
    {
        0.0h / 16.0h,  8.0h / 16.0h,  2.0h / 16.0h, 10.0h / 16.0h,
        12.0h / 16.0h, 4.0h / 16.0h, 14.0h / 16.0h,  6.0h / 16.0h,
        3.0h / 16.0h, 11.0h / 16.0h,  1.0h / 16.0h,  9.0h / 16.0h,
        15.0h / 16.0h, 7.0h / 16.0h, 13.0h / 16.0h,  5.0h / 16.0h
    };

    return threshold[index];
}

inline void ProjectKM_ApplyDitherFadeClip(float4 positionCS)
{
    half fade = saturate(pow(_CameraFade, _FadePower));
    if (fade >= _FullFadeThreshold)
    {
        clip(-1.0h);
    }

    half ditherFade = (_FullFadeThreshold > 0.0001h) ? saturate(fade / _FullFadeThreshold) : 1.0h;
    clip((1.0h - ditherFade) - ProjectKM_Dither4x4(positionCS.xy) - 0.0001h);
}

#endif
