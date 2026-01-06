using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Types of actions a pad can trigger
public enum PadActionType { Sample, Light, State }

[System.Serializable]
public class PadActionEntry
{
    public PadActionType type = PadActionType.Sample; // Action type
    [Min(0f)] public float startDelay = 0f;           // Delay before action starts

    [Header("Sample Settings")]
    public AudioClip clip;                            // Audio clip to play
    public bool loop;                                 // Loop the sound
    [Range(0f,1f)] public float gain = 1f;            // Volume

    // If true, this sound can be modified by hand gestures
    [Tooltip("Check this if you want to change this sound with Hand Gestures.")]
    public bool affectedByGestures = false;

    [Header("Light Settings")]
    [Range(0,8)] public int colorSlot = 0;             // Light color index
    public bool lightStayOn = false;                   // Keep light on
    public float lightDuration = 0.25f;                // Light duration

    public LightStateSO stateAsset;                    // Light state asset
    public bool loopState = false;                     // Loop state animation
}

[System.Serializable]
public class PadProfile {
    public string profileName = "Step 1";              // Profile label
    public List<PadActionEntry> actions = new();       // Actions in this profile
}

public class Pad : MonoBehaviour
{
    [Range(0,63)] public int index;                    // Pad index (ID)
    public Renderer rend;                              // Pad renderer
    [HideInInspector] public Material baseMaterial;    // Original material

    public List<PadProfile> profiles = new();          // All pad profiles
    [SerializeField] private int currentProfileIndex = 0; // Active profile

    private float lastPressTime = -1f;                 // Last press timestamp
    private float cooldown = 0.25f;                    // Press cooldown

    void Awake(){
        // Cache renderer and base material
        if (!rend) rend = GetComponent<Renderer>();
        if (rend) baseMaterial = rend.material;

        // Ensure pad has a trigger collider
        Collider col = GetComponent<Collider>();
        if (!col) col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true;
    }

    // Triggered by finger collider
    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Finger")) TryPress();
    }

    // Mouse click support (editor/testing)
    void OnMouseDown(){ TryPress(); }

    // Check conditions before pressing
    void TryPress() {
        if (PauseManager.I != null && PauseManager.I.isPaused) return;
        if (Time.time < lastPressTime + cooldown) return;

        lastPressTime = Time.time;
        Press(1f);
    }

    // Execute pad press
    public void Press(float velocity = 1f){
        // Stop any looping light state on this pad
        LightDirector.I.StopStateLoop(index);

        // If no profiles, only play press animation
        if (profiles.Count == 0) {
            StartCoroutine(Punch());
            return;
        }

        // Run all actions in current profile
        var activeProfile = profiles[currentProfileIndex];
        foreach (var a in activeProfile.actions)
            StartCoroutine(RunAction(a, velocity));

        // Visual press feedback
        StopCoroutine("Punch");
        StartCoroutine(Punch());

        // Switch to next profile (cycle)
        currentProfileIndex++;
        if (currentProfileIndex >= profiles.Count)
            currentProfileIndex = 0;
    }

    // Execute a single action entry
    IEnumerator RunAction(PadActionEntry a, float velocity){
        if (a.startDelay > 0f)
            yield return new WaitForSeconds(a.startDelay);

        switch (a.type){
            case PadActionType.Sample:
                if (a.clip) {
                    // Send sound to AudioHub, with gesture control flag
                    if (a.loop)
                        AudioHub.I.ToggleLoop(a.clip, a.gain, a.affectedByGestures);
                    else
                        AudioHub.I.OneShot(a.clip, a.gain, a.affectedByGestures);
                }
                break;

            case PadActionType.Light:
                // Trigger light effect on this pad
                LightDirector.I.LightOneBySlot(
                    index, a.colorSlot, a.lightDuration, 0, a.lightStayOn
                );
                break;

            case PadActionType.State:
                // Trigger a predefined light state
                if (a.stateAsset)
                    LightDirector.I.TriggerState(index, a.stateAsset, a.loopState);
                break;
        }
    }

    // Simple scale animation for press feedback
    IEnumerator Punch(){
        var s = transform.localScale;
        var b = s * 1.06f;

        float t = 0;
        while(t < 0.05f){
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(s, b, t / 0.05f);
            yield return null;
        }

        t = 0;
        while(t < 0.15f){
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(b, s, t / 0.15f);
            yield return null;
        }

        transform.localScale = s;
    }
}
