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
        for (int i = 0; i < listeners.Count; i++)
        {
            listeners[i].OnTickCalled(i_tick);
        }
    }

    public void AddListener(TickListener listener)
    {
        listeners.Add(listener);
    }

    public void RemoveListener(TickListener listener)
    {
        listeners.Remove(listener);
    }
}
