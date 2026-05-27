using System;
using SDColor = System.Drawing.Color;
using Vector4 = System.Numerics.Vector4;

namespace WaystoneNotify
{
    public static class ColorUtil
    {
        // Convert an ImGui-style normalized Vector4 (RGBA 0..1) into a System.Drawing.Color
        // for use with ExileCore2 Graphics drawing calls.
        public static SDColor ToSdColor(this Vector4 v)
        {
            byte Clamp(float f) => (byte)Math.Max(0, Math.Min(255, (int)(f * 255f)));
            return SDColor.FromArgb(Clamp(v.W), Clamp(v.X), Clamp(v.Y), Clamp(v.Z));
        }
    }
}
