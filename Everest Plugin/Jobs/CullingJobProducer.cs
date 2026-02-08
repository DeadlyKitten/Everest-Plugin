using System;
using Everest.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace Everest.Jobs
{
    public static unsafe class CullingJobProducer
    {
        public static readonly IntPtr JobReflectionData = JobsUtility.CreateJobReflectionData(
            typeof(CullingJobNative),
            (ExecuteJobFunction)Execute
        );

        delegate void ExecuteJobFunction(ref CullingJobNative data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

        private static void Execute(ref CullingJobNative job, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
        {
            while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out int begin, out int end))
            {
                var jobPointer = UnsafeUtility.AddressOf(ref job);
                NativeInterop.ExecuteCullingJob(jobPointer, begin, end);
            }
        }
    }
}
