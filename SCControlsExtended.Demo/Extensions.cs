using System;

namespace SCControlsExtended.Demo
{
    internal static class Extensions
    {
        public static T Prepare<T>(this T obj, Action<T> action)
        {
            action(obj);
            return obj;
        }
    }
}
