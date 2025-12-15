using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PadActionType { Sample, Light, State, Transport } 

[System.Serializable]
public class PadActionEntry
{
    [Header("Main Settings")]
    public PadActionType type = PadActionType.Sample; 
    [Min(0f)] public float startDelay = 0f;

    [Header("Sample")]
    public AudioClip clip;
    public bool loop;
    [Range(0f,1f)] public float gain = 1f;

    [Header("Light")]
    [Range(0,8)] public int colorSlot = 0;
    public bool lightStayOn = false; 
    public float lightDuration = 0.25f;

    [Header("State")]
    public LightStateSO stateAsset;
    public bool loopState = false; 

    [Header("Transport")]
    public string transport = ""; 
}

[System.Serializable]
public class PadProfile {
    public string profileName = "Step 1"; 
    public List<PadActionEntry> actions = new();
}

public class Pad : MonoBehaviour
{
    [Range(0,63)] public int index; 
    public Renderer rend;
    [HideInInspector] public Material baseMaterial; 
    public List<PadProfile> profiles = new();
    [SerializeField] private int currentProfileIndex = 0;

    // Prevents double-triggering
    private bool isPressed = false;

    void Awake(){
        if (!rend) rend = GetComponent<Renderer>();
        if (rend) baseMaterial = rend.material; 
        
        // Ensure Pad detects physics
        Collider col = GetComponent<Collider>();
        if (!col) col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true; 
    }

    // --- NEW: PHYSICS TRIGGER ---
    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Finger") && !isPressed) {
            isPressed = true;
            Press(1f);
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.CompareTag("Finger")) isPressed = false;
    }

    // Keep Mouse for testing
    void OnMouseDown(){ Press(1f); }

    public void Press(float velocity = 1f){
        LightDirector.I.StopStateLoop(index);
        if (profiles.Count == 0) { StartCoroutine(Punch()); return; }

        var activeProfile = profiles[currentProfileIndex];
        foreach (var a in activeProfile.actions) StartCoroutine(RunAction(a, velocity));

        StopCoroutine("Punch"); StartCoroutine(Punch());

        currentProfileIndex++;
        if (currentProfileIndex >= profiles.Count) currentProfileIndex = 0;
    }

    IEnumerator RunAction(PadActionEntry a, float velocity){
        if (a.startDelay > 0f) yield return new WaitForSeconds(a.startDelay);
        switch (a.type){
            case PadActionType.Transport: if (!string.IsNullOrEmpty(a.transport)) AudioHub.I.Transport(a.transport); break;
            case PadActionType.Sample: if (a.clip) { if (a.loop) AudioHub.I.ToggleLoop(a.clip, a.gain); else AudioHub.I.OneShot(a.clip, a.gain); } break;
            case PadActionType.Light: LightDirector.I.LightOneBySlot(index, a.colorSlot, a.lightDuration, 0, a.lightStayOn); break;
            case PadActionType.State: if (a.stateAsset) LightDirector.I.TriggerState(index, a.stateAsset, a.loopState); break;
        }
    }

    IEnumerator Punch(){
        var s = transform.localScale; var b = s * 1.06f;
        float t=0; while(t<0.05f){ t+=Time.deltaTime; transform.localScale=Vector3.Lerp(s,b,t/0.05f); yield return null; }
        t=0; while(t<0.15f){ t+=Time.deltaTime; transform.localScale=Vector3.Lerp(b,s,t/0.15f); yield return null; }
        transform.localScale = s;
    }
}