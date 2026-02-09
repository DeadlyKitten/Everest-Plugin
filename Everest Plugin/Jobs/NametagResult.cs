using System.Runtime.InteropServices;

namespace Everest.Jobs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NametagResult
    {
        public float ScreenX;
        public float ScreenY;
        public float Alpha;
        public float Scale;
        public int IsVisible;
    }
}
