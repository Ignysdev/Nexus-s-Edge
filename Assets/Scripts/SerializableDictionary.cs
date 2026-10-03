using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>,
    ISerializationCallbackReceiver
{
    [Serializable]
    private struct Entry
    {
        public TKey key;
        public TValue value;

        public Entry(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }
    }

    [SerializeField]
    private List<Entry> _entries = new();

    public void OnBeforeSerialize()
    {
        _entries.Clear();

        foreach (var pair in this)
        {
            _entries.Add(new Entry(pair.Key, pair.Value));
        }
    }

    public void OnAfterDeserialize()
    {
        Clear();

        foreach (var entry in _entries)
        {
            if (ContainsKey(entry.key))
            {
                if(entry.key is int)
                {
                    int i_newInt = (int)(object) entry.key;
                    TKey i_key = (TKey)(object)(i_newInt + 1);

                    Add(i_key, entry.value);

                    continue;
                }

                Debug.LogWarning(
                    $"Duplicate key '{entry.key}' found in {GetType().Name}. " +
                    $"The duplicate entry will be ignored."
                );

                continue;
            }

            Add(entry.key, entry.value);
        }
    }
}