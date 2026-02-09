#include "NametagLogic.h"
#include <immintrin.h>

void ExecuteCulling_AVX2(CullingJobData* jobData, int begin, int end)
{
    __m256 vCamX = _mm256_set1_ps(jobData->cameraPosition.x);
    __m256 vCamY = _mm256_set1_ps(jobData->cameraPosition.y);
    __m256 vCamZ = _mm256_set1_ps(jobData->cameraPosition.z);
    __m256 vMaxDistSq = _mm256_set1_ps(jobData->squaredDrawDistance);
    __m256 vMaxValue = _mm256_set1_ps(3.402823466e+38F);

    __m256i vIndices = _mm256_setr_epi32(
        begin, begin + 1, begin + 2, begin + 3,
        begin + 4, begin + 5, begin + 6, begin + 7
    );

    __m256i vEight = _mm256_set1_epi32(8);

    const float* __restrict sX = jobData->skeletonsX;
    const float* __restrict sY = jobData->skeletonsY;
    const float* __restrict sZ = jobData->skeletonsZ;
    DistanceCullingResult* __restrict outResults = jobData->results;

    int i = begin;

    for (; i <= end - 8; i += 8)
    {
        __m256 vx = _mm256_loadu_ps(sX + i);
        __m256 vy = _mm256_loadu_ps(sY + i);
        __m256 vz = _mm256_loadu_ps(sZ + i);

        __m256 dx = _mm256_sub_ps(vCamX, vx);
        __m256 dy = _mm256_sub_ps(vCamY, vy);
        __m256 dz = _mm256_sub_ps(vCamZ, vz);

        __m256 distSq = _mm256_mul_ps(dx, dx);
        distSq = _mm256_add_ps(distSq, _mm256_mul_ps(dy, dy));
        distSq = _mm256_add_ps(distSq, _mm256_mul_ps(dz, dz));

        __m256 mask = _mm256_cmp_ps(distSq, vMaxDistSq, _CMP_GT_OQ);

        __m256 finalDist = _mm256_blendv_ps(distSq, vMaxValue, mask);

        __m256 vIndicesFloat = _mm256_castsi256_ps(vIndices);

        __m256 row1 = _mm256_unpacklo_ps(vIndicesFloat, finalDist);
        __m256 row2 = _mm256_unpackhi_ps(vIndicesFloat, finalDist);

        __m256 outA = _mm256_permute2f128_ps(row1, row2, 0x20);
        __m256 outB = _mm256_permute2f128_ps(row1, row2, 0x31);

        _mm256_storeu_ps((float*)(outResults + i), outA);
        _mm256_storeu_ps((float*)(outResults + i + 4), outB);

        vIndices = _mm256_add_epi32(vIndices, vEight);
    }

    float scalarMax = 3.402823466e+38F;
    float scalarSqDist = jobData->squaredDrawDistance;
    float cx = jobData->cameraPosition.x;
    float cy = jobData->cameraPosition.y;
    float cz = jobData->cameraPosition.z;

    for (; i < end; ++i)
    {
        float dx = cx - sX[i];
        float dy = cy - sY[i];
        float dz = cz - sZ[i];
        float dSq = (dx * dx) + (dy * dy) + (dz * dz);

        outResults[i].index = i;
        outResults[i].distance = (dSq > scalarSqDist) ? scalarMax : dSq;
    }
}

void ExecuteNametag_AVX2(NametagJobData* jobData, int begin, int end)
{
    ExecuteNametag_Kernel(jobData, begin, end);
}