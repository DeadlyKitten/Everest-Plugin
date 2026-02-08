#pragma once
#include <algorithm>
#include <array>
#include <intrin.h>

inline bool CheckAVX2()
{
    std::array<int, 4> cpui;
    __cpuid(cpui.data(), 0);
    int nIds = cpui[0];
    if (nIds < 7) return false;
    __cpuid(cpui.data(), 7);
    return (cpui[1] & (1 << 5)) != 0;
}

inline bool CheckSSE4_1()
{
    std::array<int, 4> cpui;
    __cpuid(cpui.data(), 1);
    return (cpui[2] & (1 << 19)) != 0;
}

inline float Lerp(float a, float b, float t) {
    return a + (b - a) * t;
}

inline float EvaluateScaleCurve(float t) {
    t = std::clamp(t, 0.0f, 1.0f);
    if (t < 0.2f) {
        float localT = t / 0.2f;
        return Lerp(0.8f, 1.2f, localT);
    }
    else {
        float localT = (t - 0.2f) / 0.8f;
        return Lerp(1.2f, 2.0f, localT);
    }
}