using System.Runtime.InteropServices;

namespace Everest.Utilities
{
    internal static unsafe class NativeInterop
    {
        private const string DLL_NAME = "Everest_Native";

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int InitializeNativePlugin();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void ExecuteCullingJob(void* data, int begin, int end);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void ExecuteNametagJob(void* data, int begin, int end);
    }
}
