using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

static public class MenuButtonSfx
{
    static public void Hook(
        Button i_button,
        AudioClip i_hoverClip,
        AudioClip i_clickClip,
        float i_volume)
    {
        if (i_button == null)
            return;

        if (i_clickClip != null)
            i_button.onClick.AddListener(() => Play(i_clickClip, i_volume));

        AddHover(i_button.gameObject, i_hoverClip, i_volume);
    }

    static public void Hook(
        Toggle i_toggle,
        AudioClip i_hoverClip,
        AudioClip i_clickClip,
        float i_volume)
    {
        if (i_toggle == null)
            return;

        if (i_clickClip != null)
            i_toggle.onValueChanged.AddListener(_ => Play(i_clickClip, i_volume));

        AddHover(i_toggle.gameObject, i_hoverClip, i_volume);
    }

    static private void AddHover(GameObject i_target, AudioClip i_hoverClip, float i_volume)
    {
        if (i_target == null || i_hoverClip == null)
            return;

        EventTrigger i_trigger = i_target.GetComponent<EventTrigger>();

        if (i_trigger == null)
            i_trigger = i_target.AddComponent<EventTrigger>();

        EventTrigger.Entry i_entry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };

        i_entry.callback.AddListener(_ => Play(i_hoverClip, i_volume));
        i_trigger.triggers.Add(i_entry);
    }

    static public void Play(AudioClip i_clip, float i_volume = 0.8f)
    {
        if (i_clip == null)
            return;

        GameAudio.PlaySfx(
            i_clip,
            Vector3.zero,
            Mathf.Clamp01(i_volume),
            1f,
            false);
    }
}
