using System;
using System.Collections.Generic;

namespace Gismo.Tools
{
    [Serializable]
    class VisualDictionary<K,V>
    {
        [System.Serializable]
        public struct VisualDictionaryElement
        {
            public K key;
            public V value;
        }

        private Dictionary<K, V> dict;
        public List<VisualDictionaryElement> elements;
        private bool alreadyInit;

        public void Initalize(List<VisualDictionaryElement> element)
        {
            elements = element;
        }

        public void Initalize(VisualDictionaryElement[] element)
        {
            elements = new List<VisualDictionaryElement>(element);
        }

        public void Initalize(bool ignoreAlreadyInit = false)
        {
            if(alreadyInit)
            {
                if (!ignoreAlreadyInit)
                    return;
            }


            dict = new Dictionary<K, V>();
            foreach(VisualDictionaryElement e in elements)
            {
                dict.Add(e.key, e.value);
            }

            alreadyInit = true;
        }

        public VisualDictionaryElement[] GetAllItems()
        {
            return elements.ToArray();
        }

        public V GetItem(K key)
        {
            Initalize();
            return dict[key];
        }

        public bool ItemExists(K key)
        {
            Initalize();
            return dict.ContainsKey(key);
        }

        public V this[K i]
        {
            get 
            {
                return GetItem(i); 
            }
        }
    }
}
