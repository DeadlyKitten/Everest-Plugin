using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using static Unity.Jobs.LowLevel.Unsafe.JobsUtility;

namespace Everest.Jobs
{
    public static unsafe class JobExtensions
    {
        public static JobHandle ScheduleNative(this CullingJobNative jobData, int arrayLength, int batchSize, JobHandle dependency = default)
        {
            var scheduleParams = new JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobData),
                CullingJobProducer.JobReflectionData,
                dependency,
                ScheduleMode.Parallel
            );

            return ScheduleParallelFor(
                ref scheduleParams,
                arrayLength,
                batchSize
            );
        }

        public static JobHandle ScheduleNative(this NametagJobNative jobData, int arrayLength, int batchSize, JobHandle dependency = default)
        {
            var scheduleParams = new JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobData),
                NametagJobProducer.JobReflectionData,
                dependency,
                ScheduleMode.Parallel
            );

            return ScheduleParallelFor(
                ref scheduleParams,
                arrayLength,
                batchSize
            );
        }
    }
}
