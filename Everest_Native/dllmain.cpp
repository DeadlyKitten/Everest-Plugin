#include "HelperMethods.h"
#include "SharedTypes.h"

typedef void (*CullingFunc)(CullingJobData*, int, int);
static CullingFunc ActiveCullingFunc = ExecuteCulling_Generic;

typedef void (*NametagFunc)(NametagJobData*, int, int);
static NametagFunc ActiveNametagFunc = ExecuteNametag_Generic;

extern "C" __declspec(dllexport)
int InitializeNativePlugin()
{
    if (CheckAVX2())
    {
        ActiveCullingFunc = ExecuteCulling_AVX2;
        ActiveNametagFunc = ExecuteNametag_AVX2;
        return 2;
    }

    if (CheckSSE4_1())
    {
        ActiveCullingFunc = ExecuteCulling_SSE4;
        ActiveNametagFunc = ExecuteNametag_SSE4;
        return 1;
    }

    ActiveCullingFunc = ExecuteCulling_Generic;
    ActiveNametagFunc = ExecuteNametag_Generic;
    return 0;
}

extern "C" __declspec(dllexport)
void ExecuteCullingJob(void* rawData, int begin, int end)
{
    CullingJobData* jobData = static_cast<CullingJobData*>(rawData);
    ActiveCullingFunc(jobData, begin, end);
}

extern "C" __declspec(dllexport)
void ExecuteNametagJob(void* rawData, int begin, int end)
{
    NametagJobData* jobData = static_cast<NametagJobData*>(rawData);
    ActiveNametagFunc(jobData, begin, end);
}
