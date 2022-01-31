using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gismo
{
    public static class ClassExtensions
    {
        public static string GetString(this byte[] bytes)
        {
            string returnable = "{";
            foreach (byte b in bytes)
            {
                returnable += b;
            }

            return returnable + "}";
        }

        public static Rect GetGlobalPosition(this RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);
        }

        public static bool IsInsideRect(this RectTransform rect, Vector3 position)
        {
            return GetGlobalPosition(rect).Contains(position);
        }

        public static bool IsRectWithin(this RectTransform rect, RectTransform other)
        {
            return rect.GetGlobalPosition().Overlaps(other.GetGlobalPosition());
        }
    }
    public class StaticFunctions
    {
        public static bool IsRectWithinScreen(RectTransform rect, float offset = 0f)
        {
            Vector3[] corners = new Vector3[4];

            rect.GetWorldCorners(corners);
            
            int visibleCorners = 0;
            Rect screenRect = new Rect(0f, 0f, Screen.width, Screen.height);

            foreach (Vector3 corner in corners)
            {
                if (screenRect.Contains(corner))
                {
                    visibleCorners++;
                }
            }

            if (visibleCorners == 4)
            {
                return true;
            }
            return false;
        }
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
