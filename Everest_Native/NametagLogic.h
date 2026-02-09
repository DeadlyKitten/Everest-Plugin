#pragma once
#include "HelperMethods.h"
#include "SharedTypes.h"
#include <algorithm>

static __forceinline void ExecuteNametag_Kernel(NametagJobData* jobData, int begin, int end)
{
    const float3* __restrict skeletons = jobData->skeletons;
    NametagResult* __restrict results = jobData->results;

    float cameraX = jobData->cameraPosition.x;
    float cameraY = jobData->cameraPosition.y;
    float cameraZ = jobData->cameraPosition.z;

    float forwardX = jobData->cameraFoward.x;
    float forwardY = jobData->cameraFoward.y;
    float forwardZ = jobData->cameraFoward.z;

    const float* projectionMatrix = jobData->viewProjectionMatrix.matrix;

    for (int i = begin; i < end; ++i)
    {
        results[i].isVisible = 1;

        float3 position = skeletons[i];

        float x = position.x;
        float y = position.y + jobData->textVerticalOffset;
        float z = position.z;

        float dx = x - cameraX;
        float dy = (position.y - cameraY);
        float dz = z - cameraZ;
        float distanceSquared = dx * dx + dy * dy + dz * dz;

        if (distanceSquared > jobData->maxDistanceSquared) {
            results[i].isVisible = 0;
            continue;
        }

        float inverseDistance = 1.0f / sqrtf(distanceSquared);
        float dirX = dx * inverseDistance;
        float dirY = dy * inverseDistance;
        float dirZ = dz * inverseDistance;

        float dot = (forwardX * dirX) + (forwardY * dirY) + (forwardZ * dirZ);

        if (dot < jobData->maxViewAngleCos) {
            results[i].isVisible = 0;
            continue;
        }

        float clipX = (x * projectionMatrix[0]) + (y * projectionMatrix[4]) + (z * projectionMatrix[8]) + projectionMatrix[12];
        float clipY = (x * projectionMatrix[1]) + (y * projectionMatrix[5]) + (z * projectionMatrix[9]) + projectionMatrix[13];
        float clipW = (x * projectionMatrix[3]) + (y * projectionMatrix[7]) + (z * projectionMatrix[11]) + projectionMatrix[15];

        if (clipW <= 0.0f) {
            results[i].isVisible = 0;
            continue;
        }

        float inverseW = 1.0f / clipW;
        float normalizedX = clipX * inverseW;
        float normalizedY = clipY * inverseW;

        float screenX = (normalizedX + 1.0f) * 0.5f * jobData->screenWidth;
        float screenY = (normalizedY + 1.0f) * 0.5f * jobData->screenHeight;

        float distanceScoreRaw = (distanceSquared - jobData->minDistanceSquared) / (jobData->maxDistanceSquared - jobData->minDistanceSquared);
        float distanceScore = 1.0f - std::clamp(distanceScoreRaw, 0.0f, 1.0f);

        float range = 1.0f - jobData->maxViewAngleCos;
        float val = dot - jobData->maxViewAngleCos;

        float angleScoreRaw = val / range;
        float angleScore = std::clamp(angleScoreRaw, 0.0f, 1.0f);

        float finalAlpha = distanceScore * angleScore;
        float targetScale = EvaluateScaleCurve(distanceScoreRaw);
        float finalScale = std::clamp(targetScale, jobData->minTextScale, jobData->maxTextScale);

        results[i].screenX = screenX;
        results[i].screenY = screenY;
        results[i].alpha = finalAlpha;
        results[i].scale = finalScale;
    }
}