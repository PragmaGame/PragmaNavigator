using System;
using System.Collections.Generic;

namespace Pragma.ScreenNavigator
{
    public static class Extensions
    {
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T obj in source)
            {
                action(obj);   
            }
            
            return source;
        }
    }
}