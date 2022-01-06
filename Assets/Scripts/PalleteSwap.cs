using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Gismo.ReadOnly;
using System;

namespace Gismo.PalletSwap
{
    public class PalleteSwap : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;

        [SerializeField]
        private ColorDictionary colorDictionary;

        private ColorAtlas atlas;

        public virtual void Start()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            UpdateSpriteWithPallete();
        }

        [ContextMenu("Reset Sprite")]
        public void ResetSprite()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = colorDictionary.refrenceSprite;
        }

        [ContextMenu("Randomize Color Pallete")]
        public void RandomizeColorSet()
        {
            for (int i = 0; i < colorDictionary.colorPairings.Count; i++)
            {
                ColorPairing pairing = new ColorPairing(colorDictionary.colorPairings[i].source,
                    StaticFunctions.GetRandomColor());

                colorDictionary.colorPairings[i] = pairing;
            }
        }
        
        [ContextMenu("Update Sprite With Pallete")]
        public void UpdateSpriteWithPallete()
        {
            if(spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            atlas = new ColorAtlas(colorDictionary);

            spriteRenderer.sprite = Sprite.Create(atlas.SwapPallete(colorDictionary.refrenceSprite),
                new Rect(0.0f, 0.0f, spriteRenderer.sprite.texture.width, spriteRenderer.sprite.texture.height),
                new Vector2(0.5f, 0.5f), spriteRenderer.sprite.pixelsPerUnit);

            spriteRenderer.sprite.name = $"{colorDictionary.refrenceSprite.name}: MOD {spriteRenderer.GetInstanceID()}";
        }
    }

    [Serializable]
    public struct ColorPairing
    {
        [ReadOnly]
        public Color source;
        public Color replacement;

        public ColorPairing(Color source, Color replacement)
        {
            this.source = source;
            this.replacement = replacement;
        }
    }

    [Serializable]
    public class ColorDictionary
    {
        public Sprite refrenceSprite;
        public List<ColorPairing> colorPairings;

        public bool compareAlpha;

        public ColorDictionary(List<ColorPairing> pairing)
        {
            colorPairings = new List<ColorPairing>(pairing);
        }

        public void GenerateColorLookup()
        {
            GenerateColorLookup(refrenceSprite,compareAlpha);
        }
        public void GenerateColorLookup(Sprite refrenceSprite, bool compareAlpha)
        {
            colorPairings = new List<ColorPairing>();
            foreach (Color c in refrenceSprite.texture.GetPixels().Distinct().ToList())
            {
                if (compareAlpha)
                {
                    if (c.a != 0f)
                    {
                        colorPairings.Add(new ColorPairing(c, c));
                    }
                }
                else
                {
                    colorPairings.Add(new ColorPairing(c, c));
                }
            }
        }

        public static explicit operator ColorDictionary(UnityEngine.Object v)
        {
            throw new NotImplementedException();
        }
    }

    public class ColorAtlas
    {
        private ColorDictionary colorDict;

        public ColorAtlas()
        {
            colorDict = new ColorDictionary(new List<ColorPairing>());
        }

        public ColorAtlas (List<ColorPairing> pairs)
        {
            colorDict = new ColorDictionary(pairs);
        }

        public ColorAtlas(ColorDictionary dictionary)
        {
            colorDict = dictionary;
        }

        public void UpateColorDictionary(ColorDictionary colorDictionary)
        {
            colorDict = colorDictionary;
        }

        public Color ReplaceColor(Color color)
        {
            foreach(ColorPairing set in colorDict.colorPairings)
            {
                if (StaticFunctions.ColorCompare(set.source, color))
                {
                    return set.replacement;
                }
            }
            Debug.LogError($"Color {color} doesn't exist in set!");
            return color;
        }

        public Texture2D SwapPallete(Sprite refrenceSprite, FilterMode filterMode = FilterMode.Point, TextureWrapMode wrapMode = TextureWrapMode.Repeat)
        {
            Texture2D generatedTexture = new Texture2D(refrenceSprite.texture.width, 
                refrenceSprite.texture.height);

            for (int x = 0; x < generatedTexture.width; x++)
            {
                for (int y = 0; y < generatedTexture.height; y++)
                {
                    if(colorDict.compareAlpha && refrenceSprite.texture.GetPixel(x, y).a == 0f)
                    {
                        generatedTexture.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    generatedTexture.SetPixel(x, y, ReplaceColor(refrenceSprite.texture.GetPixel(x, y)));
                }
            }

            generatedTexture.filterMode = filterMode;
            generatedTexture.wrapMode = wrapMode;

            generatedTexture.Apply();
            return generatedTexture;
        }
    }
}