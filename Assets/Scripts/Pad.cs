using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PadActionType { Transport, Sample, Light, State }

[System.Serializable]
public class PadActionEntry
{
    public PadActionType type = PadActionType.Light;

    [Header("Common")]
    [Min(0f)] public float startDelay = 0f;

    [Header("Transport")]
    public string transport = "togglePlay";  // togglePlay|play|pause|stop|restart

    [Header("Sample")]
    public AudioClip clip;                   // drag from Resources/Audio/
    public bool loop;
    [Range(0f,1f)] public float gain = 1f;

    [Header("Light")]
    [Range(0,8)] public int colorSlot = 0;   // selects material slot in LightDirector
    [Min(0.01f)] public float lightDuration = 0.25f;
    [Min(0f)]    public float lightDelay    = 0f;

    [Header("State")]
    public LightStateSO stateAsset;
}

public class Pad : MonoBehaviour
{
    [Header("Identity")]
    [Range(0,63)] public int index;          // 0..63 for 8×8
    public Renderer rend;

    [Header("Actions fired on Press (in order)")]
    public List<PadActionEntry> actions = new();

    void Awake(){
        if (!rend) rend = GetComponent<Renderer>();
    }

    public void Press(float velocity = 1f){
        foreach (var a in actions) StartCoroutine(RunAction(a, velocity));
        StopAllCoroutines(); StartCoroutine(Punch()); // small feedback
    }

    public void Release(){ /* reserved for hold/gate modes if needed */ }

    IEnumerator RunAction(PadActionEntry a, float velocity){
        if (a.startDelay > 0f) yield return new WaitForSeconds(a.startDelay);

        switch (a.type){
            case PadActionType.Transport:
                AudioHub.I.Transport(a.transport);
                break;

            case PadActionType.Sample:
                if (a.clip){
                    var name = a.clip.name; // must be in Resources/Audio/
                    if (a.loop) AudioHub.I.ToggleLoop(name, a.gain * velocity);
                    else        AudioHub.I.OneShot(name,  a.gain * velocity);
                }
                break;

            case PadActionType.Light:
                LightDirector.I.LightOneBySlot(index, a.colorSlot,
                                               Mathf.Max(0.01f, a.lightDuration),
                                               Mathf.Max(0f, a.lightDelay));
                break;

            case PadActionType.State:
                if (a.stateAsset) LightDirector.I.TriggerState(a.stateAsset);
                break;
        }
    }

    // TEMP mouse so you can test now. Remove when switching to hand touch.
    void OnMouseDown(){ Press(1f); }

    IEnumerator Punch(){
        var s = transform.localScale; var b = s * 1.06f;
        float up=0.08f, down=0.22f, t0=Time.time;
        while(Time.time-t0<up){ float k=(Time.time-t0)/up; transform.localScale=Vector3.Lerp(s,b,k); yield return null; }
        t0=Time.time;
        while(Time.time-t0<down){ float k=1f-(Time.time-t0)/down; transform.localScale=Vector3.Lerp(s,b,k); yield return null; }
        transform.localScale = s;
    }
}
