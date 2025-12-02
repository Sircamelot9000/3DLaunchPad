using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Removed "Control" from the options
public enum PadActionType { Sample, Light, State, Transport } 

[System.Serializable]
public class PadActionEntry
{
    [Header("Main Settings")]
    public PadActionType type = PadActionType.Sample; 

    [Header("Common")]
    [Min(0f)] public float startDelay = 0f;

    [Header("Sample Settings")]
    public AudioClip clip;
    public bool loop;
    [Range(0f,1f)] public float gain = 1f;

    [Header("Light Settings")]
    [Range(0,8)] public int colorSlot = 0;
    public bool lightStayOn = false; 
    [Min(0.01f)] public float lightDuration = 0.25f;
    [Min(0f)]    public float lightDelay    = 0f;

    [Header("State Settings")]
    public LightStateSO stateAsset;
    public bool loopState = false; 

    [Header("Transport (Background Music)")]
    public string transport = ""; 
}

[System.Serializable]
public class PadProfile 
{
    public string profileName = "Step 1"; 
    public List<PadActionEntry> actions = new();
}

public class Pad : MonoBehaviour
{
    [Header("Identity")]
    [Range(0,63)] public int index; 
    public Renderer rend;
    
    // Kept the fix for "Stuck Lights"
    [HideInInspector] public Material baseMaterial; 

    [Header("Profiles")]
    public List<PadProfile> profiles = new();
    
    [Header("Status")]
    [SerializeField] private int currentProfileIndex = 0;

    void Awake(){
        if (!rend) rend = GetComponent<Renderer>();
        if (rend) baseMaterial = rend.material; 
    }

    public void Press(float velocity = 1f){
        // 1. Stop any looping state this pad was running (Fix for Loop Bug)
        LightDirector.I.StopStateLoop(index);

        // 2. Safety check
        if (profiles.Count == 0) { StartCoroutine(Punch()); return; }

        // 3. Get current profile
        if (currentProfileIndex >= profiles.Count) currentProfileIndex = 0;
        var activeProfile = profiles[currentProfileIndex];

        // 4. Run actions
        foreach (var a in activeProfile.actions) {
            StartCoroutine(RunAction(a, velocity));
        }

        // 5. Visuals
        StopCoroutine("Punch"); 
        StartCoroutine(Punch());

        // 6. Advance Step
        currentProfileIndex++;
        if (currentProfileIndex >= profiles.Count) currentProfileIndex = 0;
    }

    public void Release(){ }

    IEnumerator RunAction(PadActionEntry a, float velocity){
        if (a.startDelay > 0f) yield return new WaitForSeconds(a.startDelay);

        switch (a.type){
            case PadActionType.Transport:
                if (!string.IsNullOrEmpty(a.transport)) AudioHub.I.Transport(a.transport);
                break;

            case PadActionType.Sample:
                if (a.clip){
                    if (a.loop) AudioHub.I.ToggleLoop(a.clip, a.gain * velocity);
                    else        AudioHub.I.OneShot(a.clip,  a.gain * velocity);
                }
                break;

            case PadActionType.Light:
                LightDirector.I.LightOneBySlot(index, a.colorSlot, 
                                               Mathf.Max(0.01f, a.lightDuration), 
                                               Mathf.Max(0f, a.lightDelay),
                                               a.lightStayOn);
                break;

            case PadActionType.State:
                if (a.stateAsset) LightDirector.I.TriggerState(index, a.stateAsset, a.loopState);
                break;
        }
    }

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