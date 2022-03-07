using System.Collections.Generic;
using System;

public class BuildNRunPPHelpers<T> where T : Enum
{
    public static T[] GetAllSelected(T sourceMask, List<T> ignored)
    {
        List<T> targets = new List<T>();
        foreach (T check in Enum.GetValues(typeof(T)))
        {
            if (ignored.Contains(check))
                continue;
            if (sourceMask.HasFlag(check))
            {
                targets.Add(check);
            }
        }

        return targets.ToArray();
    }
}
