using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gismo
{
    public class StaticFunctions
    {
        public static Color GetRandomColor(bool doAlpha = false)
        {
            if(doAlpha)
                return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            else
                return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        }

        public static bool ColorCompare(Color a, Color b, bool compareAlpha = false)
        {
            if (Mathf.Approximately(a.r, b.r) &&
                    Mathf.Approximately(a.g, b.g) &&
                    Mathf.Approximately(a.b, b.b))
            {
                if (compareAlpha)
                {
                    if (Mathf.Approximately(a.a, b.a))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
