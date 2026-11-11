using AudioData;
using UnityEngine;

public class AllTickListener : TickListener
{
    public override void OnTickCalled(TickInfo i_tickInfo)
    {
        if (i_tickInfo.type != TickType.Start)
            tickEvent.Invoke(i_tickInfo);
    }
}
