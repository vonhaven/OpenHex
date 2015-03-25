﻿using System;
using System.Collections.Generic;

public sealed class Tuple<T1, T2>
{
    private readonly T1 item1;
    private readonly T2 item2;

    public T1 Item1
    {
        get { return item1; }
    }

    public T2 Item2
    {
        get { return item2; }
    }

    public Tuple(T1 item1, T2 item2)
    {
        this.item1 = item1;
        this.item2 = item2;
    }

    public override string ToString()
    {
        return string.Format("Tuple({0}, {1})", Item1, Item2);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 23 + (item1 == null ? 0 : item1.GetHashCode());
        hash = hash * 23 + (item2 == null ? 0 : item2.GetHashCode());
        return hash;
    }

    public override bool Equals(object o)
    {
        if (!(o is Tuple<T1, T2>)) {
            return false;
        }
        var other = (Tuple<T1, T2>) o;
        return this == other;
    }

    public bool Equals(Tuple<T1, T2> other)
    {
        return this == other;
    }

    public static bool operator==(Tuple<T1, T2> a, Tuple<T1, T2> b)
    {
        if (object.ReferenceEquals(a, null)) {
            return object.ReferenceEquals(b, null);
        }
        if (a.item1 == null && b.item1 != null) return false;
        if (a.item2 == null && b.item2 != null) return false;
        return a.item1.Equals(b.item1) && a.item2.Equals(b.item2);
    }

    public static bool operator!=(Tuple<T1, T2> a, Tuple<T1, T2> b)
    {
        return !(a == b);
    }

    public void Unpack(Action<T1, T2> unpackerDelegate)
    {
        unpackerDelegate(Item1, Item2);
    }
}