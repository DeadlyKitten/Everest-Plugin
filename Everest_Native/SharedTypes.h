#pragma once

struct DistanceCullingResult
{
    int index;
    float distance;
};

struct float3
{
    float x, y, z;
};

struct CullingJobData
{
    float3 cameraPosition;
    float squaredDrawDistance;

    float* skeletonsX;
    float* skeletonsY;
    float* skeletonsZ;

    DistanceCullingResult* results;
};

struct Matrix4x4
{
    float matrix[16];
};

struct NametagResult
{
    float screenX;
    float screenY;
    float alpha;
    float scale;
    int isVisible;
};

struct NametagJobData
{
    float3* skeletons;

    NametagResult* results;

    Matrix4x4 viewProjectionMatrix;
    float3 cameraPosition;
    float3 cameraFoward;
    float screenWidth;
    float screenHeight;

    float maxDistanceSquared;
    float minDistanceSquared;
    float maxViewAngleCos;
    float textVerticalOffset;
    float minTextScale;
    float maxTextScale;
};

void ExecuteCulling_AVX2(CullingJobData* jobData, int begin, int end);
void ExecuteCulling_SSE4(CullingJobData* jobData, int begin, int end);
void ExecuteCulling_Generic(CullingJobData* jobData, int begin, int end);

void ExecuteNametag_AVX2(NametagJobData* jobData, int begin, int end);
void ExecuteNametag_SSE4(NametagJobData* jobData, int begin, int end);
void ExecuteNametag_Generic(NametagJobData* jobData, int begin, int end);