using AudioData;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TickCaller", menuName = "Scriptable Objects/TickCaller")]
public class TickCaller : ScriptableObject
{
    //VARIÁVEIS OCULTAS
        [HideInInspector]
    public List<TickListener> listeners = new();

    /* - - - - -
     * Chamada dos Ticks
     * - - - - - */
    public void TickTrigger(TickInfo i_tick)
    {
        PruneListeners();

        for (int i = 0; i < listeners.Count; i++)
        {
            if (listeners[i] == null)
                continue;

            listeners[i].OnTickCalled(i_tick);
        }
    }

    public void AddListener(TickListener listener)
    {
        if (listener == null || listeners.Contains(listener))
            return;

        listeners.Add(listener);
    }

    public void RemoveListener(TickListener listener)
    {
        if (listener == null)
            return;

        listeners.Remove(listener);
    }

    public void PruneListeners()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
        {
            if (listeners[i] == null)
                listeners.RemoveAt(i);
        }
    }
}
