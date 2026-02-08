#include "CullingLogic.h"
#include "NametagLogic.h"

void ExecuteCulling_Generic(CullingJobData* jobData, int begin, int end)
{
    ExecuteCulling_Kernel(jobData, begin, end);
}

void ExecuteNametag_Generic(NametagJobData* jobData, int begin, int end)
{
    ExecuteNametag_Kernel(jobData, begin, end);
}