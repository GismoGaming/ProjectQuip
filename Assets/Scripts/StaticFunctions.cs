using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

namespace Gismo
{
    [System.Serializable]
    public struct SerializableVector2
    {
        public float x;
        public float y;

        public SerializableVector2(float rX, float rY)
        {
            x = rX;
            y = rY;
        }

        public SerializableVector2(Vector2 input)
        {
            x = input.x;
            y = input.y;
        }

        public override string ToString()
        {
            return $"/{{{x}, /{{{y}";
        }

        public static implicit operator Vector2(SerializableVector2 self)
        {
            return new Vector2(self.x, self.y);
        }

        public static implicit operator SerializableVector2(Vector2 self)
        {
            return new SerializableVector2(self.x, self.y);
        }
    }

    public class EnumTools<T> where T : Enum
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

    public static class ClassExtensions
    { 
        public static Vector2 ToVector2(this Vector3 input)
        {
            return new Vector2(input.x, input.y);
        }

        public static Vector3 ToVector3(this Vector2 input,float z = 0f)
        {
            return new Vector3(input.x, input.y,z);
        }

        public static Vector2 AddRandomOnUnitCircle(this Vector3 input, float radius)
        {
            return input.ToVector2() + StaticFunctions.RandomOnUnitCircle(radius);
        }

        public static Vector3 ChangeZ(this Vector3 input, float zValue)
        {
            input.z = zValue;
            return input;
        }

        public static string GetString(this byte[] bytes)
        {
            string returnable = "{";
            foreach (byte b in bytes)
            {
                returnable += b;
            }

            return returnable + "}";
        }

        public static T GetRandomItem<T>(this List<T> self, int lowerBound = 0, int upperBound = 0)
        {
            return self[Random.Range(lowerBound, self.Count - 1 - upperBound)];
        }

        public static T GetRandomItem<T>(this T[] self, int lowerBound = 0, int upperBound = 0)
        {
            return self[Random.Range(lowerBound, self.Length - 1 - upperBound)];
        }

        public static K GetRandomKey<K,V>(this Tools.VisualDictionary<K,V> self, int lowerBound = 0, int upperBound = 0)
        {
            return self.elements.GetRandomItem(lowerBound, upperBound).key;
        }

        public static V GetRandomValue<K, V>(this Tools.VisualDictionary<K, V> self, int lowerBound = 0, int upperBound = 0)
        {
            return self.GetAllItems().GetRandomItem(lowerBound,upperBound).value;
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
#if UNITY_EDITOR
        public static void MoveToMainScene(GameObject go)
        {
            Scene main = SceneManager.GetSceneByBuildIndex(0);
            SceneManager.MoveGameObjectToScene(go, main);
        }
#endif

        public static Vector2 RandomOnUnitCircle(float radius)
        {
            return Random.insideUnitCircle.normalized * radius;
        }
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
            if (doAlpha)
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

        public static bool IsStruct<T>()
        {
            return typeof(T).IsValueType && !typeof(T).IsEnum;
        }
        public static bool IsClass<T>()
        {
            return typeof(T).IsClass;
        }
    }
}
