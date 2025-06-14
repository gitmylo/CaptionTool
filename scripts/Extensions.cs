using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace CaptionTool.scripts;

public static class Extensions
{
    public static Array<T> ToGdArray<[MustBeVariant] T>(this IEnumerable<T> list)
    {
        return new Array<T>(list);
    }
    
    public static Array ToUGdArray<[MustBeVariant] T>(this IEnumerable<T> list)
    {
        return new Array(list.Select(x => Variant.From(x)).ToArray());
    }

    public static T[] FromUGdArray<[MustBeVariant] T>(this Array array)
    {
        return array.Select(x => x.As<T>()).ToArray();
    }

    public static (T1, T2)[] GrowZip<T1, T2>(this T1[] list1, T2[] list2)
    {
        if (list1.Length == list2.Length)
        {
            return list1.Zip(list2).ToArray();
        }
        else
        {
            // Provide every permutation
            var outList = new List<(T1, T2)>();
            foreach (var item1 in list1)
            {
                foreach (var item2 in list2)
                {
                    outList.Add((item1, item2));
                }
            }

            return outList.ToArray();
        }
    }

    public static (T1, T2)[] GrowZip<[MustBeVariant] T1, [MustBeVariant] T2>(this Array array, Array other)
    {
        return array.FromUGdArray<T1>().GrowZip(other.FromUGdArray<T2>());
    }
}