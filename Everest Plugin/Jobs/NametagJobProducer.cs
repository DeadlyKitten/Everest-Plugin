using System;
using Everest.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace Everest.Jobs
{
    internal unsafe class NametagJobProducer
    {
        public static readonly IntPtr JobReflectionData = JobsUtility.CreateJobReflectionData(
            typeof(NametagJobNative),
            (ExecuteJobFunction)Execute
        );

        delegate void ExecuteJobFunction(ref NametagJobNative data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

        private static void Execute(ref NametagJobNative job, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
        {
            while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out int begin, out int end))
            {
                var jobPointer = UnsafeUtility.AddressOf(ref job);
                NativeInterop.ExecuteNametagJob(jobPointer, begin, end);
            }
        }
    }
}
