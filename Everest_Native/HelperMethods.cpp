#include "HelperMethods.h"
#include <algorithm>

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
        return Lerp(1.2f, 2.5f, localT);
    }
}