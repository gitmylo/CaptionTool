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

    public static List<(T1, T2)> GrowZip<T1, T2>(this List<T1> list1, List<T2> list2)
    {
        if (list1.Count == list2.Count)
        {
            return list1.Zip(list2).ToList();
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

            return outList;
        }
    }
}