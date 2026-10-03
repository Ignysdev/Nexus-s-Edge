using System;
using UnityEngine;
using UnityEngine.Events;

namespace AudioData
{
    [Serializable]
    public class TickEvent : UnityEvent<TickInfo> { }

    /* - - - - -
     * Tipo de evento tick usado no TickInfo.
     * - - - - - */
    [Serializable]
    public enum TickType
    {
        Start,
        Tick,
        HalfTick
    }

    /* - - - - -
    * Tipos de AudioController.
    * - - - - - */
    [Serializable, Tooltip("")]
    public enum AudioControllerType
    {
        None,
        Main,
        Base,
        Intensity,
        Danger,
        Effects
    }

    /* - - - - -
    * Estágio atual da música.
    * - - - - - */
    public enum MusicStage
    {
        Climax,
        Passage,
        Fail,
        Variant,
        Anticipation,
        Final_Anticipation,
        Failed_Anticipation,
        Other
    }

    /* - - - - -
     * Clipes de áudio e suas características.
     * - - - - - */
    [Serializable, Tooltip("Item de áudio usado pelo sistema de som. As faixas de áudio precisam estar na ordem correta de reprodução e com o index do menor para o maior")]
    public struct AudioItem
    {
        public AudioClip source;
        public int barDuration;
        public int beatDuration;
        public MusicStage index;
    }

    /* - - - - -
     * Clipes de áudios-guias e suas características.
     * - - - - - */
    [Serializable] 
    public struct AudioGuideItem
    {
        public AudioClip source;
        public int bpm;
        public int beatPerBar;
    }

    /* - - - - -
     * Informações passadas a cada tick.
     * - - - - - */
    public struct TickInfo
    {
        public int bpm;
        public int beat;
        public int beatTotal;
        public int bar;
        public int beatFullCount;
        public double tickDuration;
        public double nextTickTimer;
        public TickType type;
    }
}
