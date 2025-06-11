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
        return new Array(list.Select(x => (Variant.From(x))));
    }

    public static T[] FromUGdArray<[MustBeVariant] T>(this Array array)
    {
        return array.Cast<T>().ToArray();
    }
}