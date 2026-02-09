#pragma once
#include "HelperMethods.h"
#include "SharedTypes.h"
#include <algorithm>

static __forceinline void ExecuteCulling_Kernel(CullingJobData* jobData, int begin, int end)
{
    float cameraX = jobData->cameraPosition.x;
    float cameraY = jobData->cameraPosition.y;
    float cameraZ = jobData->cameraPosition.z;
    float squaredDrawDistance = jobData->squaredDrawDistance;

    const float* __restrict skeletonsX = jobData->skeletonsX;
    const float* __restrict skeletonsY = jobData->skeletonsY;
    const float* __restrict skeletonsZ = jobData->skeletonsZ;

    const float MAX_VALUE = 3.402823466e+38F;

    DistanceCullingResult* __restrict outResults = jobData->results;

    for (int i = begin; i < end; ++i)
    {
        float dx = cameraX - skeletonsX[i];
        float dy = cameraY - skeletonsY[i];
        float dz = cameraZ - skeletonsZ[i];

        float squareDistance = (dx * dx) + (dy * dy) + (dz * dz);

        float finalDistance = (squareDistance > squaredDrawDistance) ? MAX_VALUE : squareDistance;

        outResults[i].index = i;
        outResults[i].distance = finalDistance;
    }
}
