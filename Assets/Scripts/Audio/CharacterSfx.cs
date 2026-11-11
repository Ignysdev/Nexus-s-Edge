using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterSfx : MonoBehaviour
{
    #region Estruturas
    [Serializable]
    public class Entry
    {
        [SerializeField]
        private string _id = string.Empty;

        [SerializeField]
        private AudioClip _clip;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        [SerializeField, Min(0f)]
        private float _pitchMin = 0.95f;

        [SerializeField, Min(0f)]
        private float _pitchMax = 1.05f;

        [SerializeField, Min(0f)]
        private float _delay = 0f;

        [Header("Espacial")]
        [SerializeField]
        private bool _spatial = true;

        [SerializeField, Min(0.01f)]
        private float _minDistance = 1f;

        [SerializeField, Min(0.01f)]
        private float _maxDistance = 30f;

        public string Id => _id;
        public AudioClip Clip => _clip;
        public float Volume => _volume;
        public float PitchMin => _pitchMin;
        public float PitchMax => _pitchMax;
        public float Delay => _delay;
        public bool Spatial => _spatial;
        public float MinDistance => _minDistance;
        public float MaxDistance => _maxDistance;
    }
    #endregion

    #region Variáveis
    // Constantes
    public const string Attack = "Attack";
    public const string Heavy = "Heavy";
    public const string Counter = "Counter";
    public const string Dash = "Dash";
    public const string BlockStart = "BlockStart";
    public const string BlockEnd = "BlockEnd";
    public const string BlockHit = "BlockHit";
    public const string Parry = "Parry";
    public const string Hit = "Hit";
    public const string LightHit = "LightHit";
    public const string HeavyHit = "HeavyHit";
    public const string Die = "Die";
    public const string Kneel = "Kneel";
    public const string StandUp = "StandUp";

    // VARIÁVEIS VISÍVEIS
    [Header("Sons")]
        [SerializeField]
    private Entry[] _entries = Array.Empty<Entry>();

    [Header("Geral")]
        [SerializeField, Range(0f, 2f), Tooltip("Multiplicador de volume só deste personagem")]
    private float _volumeScale = 1f;

    // VARIÁVEIS OCULTAS
    private Dictionary<string, List<Entry>> _lookup;
    #endregion

    #region Funções Unity
    private void Awake()
    {
        BuildLookup();
    }

    private void OnValidate()
    {
        if (_lookup != null)
            BuildLookup();
    }
    #endregion

    #region Main
    public void Play(string i_id)
    {
        PlayAt(i_id, transform.position);
    }

    public void PlayAt(string i_id, Vector3 i_position)
    {
        if (string.IsNullOrEmpty(i_id))
            return;

        if (_lookup == null)
            BuildLookup();

        if (!_lookup.TryGetValue(i_id, out List<Entry> i_entries) ||
            i_entries == null ||
            i_entries.Count == 0)
            return;

        Entry i_entry = i_entries[UnityEngine.Random.Range(0, i_entries.Count)];

        if (i_entry == null || i_entry.Clip == null)
            return;

        float i_pitch = UnityEngine.Random.Range(
            Mathf.Min(i_entry.PitchMin, i_entry.PitchMax),
            Mathf.Max(i_entry.PitchMin, i_entry.PitchMax));

        GameAudio.PlaySfx(
            i_entry.Clip,
            i_position,
            i_entry.Volume * _volumeScale,
            i_pitch,
            i_entry.Spatial,
            i_entry.MinDistance,
            i_entry.MaxDistance,
            i_entry.Delay);
    }

    public void PlayRandom(string[] i_ids)
    {
        if (i_ids == null || i_ids.Length == 0)
            return;

        Play(i_ids[UnityEngine.Random.Range(0, i_ids.Length)]);
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, List<Entry>>(StringComparer.Ordinal);

        if (_entries == null)
            return;

        for (int i = 0; i < _entries.Length; i++)
        {
            Entry i_entry = _entries[i];

            if (i_entry == null || string.IsNullOrEmpty(i_entry.Id))
                continue;

            if (!_lookup.TryGetValue(i_entry.Id, out List<Entry> i_list))
            {
                i_list = new List<Entry>();
                _lookup.Add(i_entry.Id, i_list);
            }

            i_list.Add(i_entry);
        }
    }
    #endregion
}
