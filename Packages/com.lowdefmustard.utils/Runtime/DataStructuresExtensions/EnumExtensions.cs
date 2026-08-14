using System;

namespace LowDefMustard.Utils
{
    public static class EnumExtensions
    {
        public static T NextClamped<T>(this T src) where T : struct, Enum
        {
            var arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf(arr, src) + 1;
            return j >= arr.Length ? arr[^1] : arr[j];
        }
    }
}
