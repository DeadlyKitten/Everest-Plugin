#include "CullingLogic.h"
#include "NametagLogic.h"

void ExecuteCulling_SSE4(CullingJobData* jobData, int begin, int end)
{
    ExecuteCulling_Kernel(jobData, begin, end);
}

void ExecuteNametag_SSE4(NametagJobData* jobData, int begin, int end)
{
    ExecuteNametag_Kernel(jobData, begin, end);
}