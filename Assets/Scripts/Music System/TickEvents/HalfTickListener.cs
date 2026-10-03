using AudioData;
using UnityEngine;

public class HalfTickListener : TickListener
{
    public override void OnTickCalled(TickInfo i_tickInfo)
    {
        if (i_tickInfo.type == TickType.HalfTick)
            tickEvent.Invoke(i_tickInfo);
    }
}
