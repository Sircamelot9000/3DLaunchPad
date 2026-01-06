using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightDirector : MonoBehaviour
{
    // Singleton access
    public static LightDirector I { get; private set; }

    // All pads in scene
    List<Pad> allPads = new();

    // Quick lookup: pad index -> Pad
    Dictionary<int, Pad> padByIndex = new();

    // Active one-shot light coroutines per pad
    Dictionary<int, Coroutine> activeLightRoutines = new();

    // Active looping light states per pad
    Dictionary<int, Coroutine> activeStateLoops = new();

    // Color/material palette
    public Material[] palette = new Material[9];

    void Awake(){
        I = this;
    }

    void Start(){
        // Find all pads in scene and cache by index
        allPads.Clear();
        allPads.AddRange(FindObjectsOfType<Pad>());

        padByIndex.Clear();
        foreach (var p in allPads){
            if (!padByIndex.ContainsKey(p.index))
                padByIndex.Add(p.index, p);
        }
    }

    // Custom wait that respects PauseManager (freezes light timing when paused)
    IEnumerator WaitOrPause(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            // Advance time only if not paused
            if (PauseManager.I == null || !PauseManager.I.isPaused)
            {
                timer += Time.deltaTime;
            }
            yield return null;
        }
    }

    // Stop looping light state on a specific pad
    public void StopStateLoop(int padIndex){
        if (activeStateLoops.TryGetValue(padIndex, out var routine)) {
            if (routine != null) StopCoroutine(routine);
            activeStateLoops.Remove(padIndex);
        }
    }

    // Trigger a single light flash on a pad
    public void LightOneBySlot(int padIndex, int slot, float duration, float delay = 0f, bool stayOn = false){
        if (!padByIndex.TryGetValue(padIndex, out var p)) return;

        // Stop any existing light routine on this pad
        if (activeLightRoutines.ContainsKey(padIndex)) {
            if (activeLightRoutines[padIndex] != null)
                StopCoroutine(activeLightRoutines[padIndex]);

            activeLightRoutines.Remove(padIndex);

            // Restore base material
            if (p.rend && p.baseMaterial)
                p.rend.material = p.baseMaterial;
        }

        // Start new light coroutine
        var routine = StartCoroutine(
            LightWithMatCo(p, padIndex, SafeGet(slot), duration, delay, stayOn)
        );
        activeLightRoutines[padIndex] = routine;
    }

    // Trigger a predefined light state (sequence of steps)
    public void TriggerState(int originPadIndex, LightStateSO def, bool loop = false){
        if (!def || def.steps == null) return;

        // Stop previous state loop from this pad
        StopStateLoop(originPadIndex);

        var routine = StartCoroutine(RunStateLoop(def, loop, originPadIndex));
        activeStateLoops[originPadIndex] = routine;
    }

    // Execute a light state (sequence of pad light steps)
    IEnumerator RunStateLoop(LightStateSO def, bool loop, int originIdx){
        do {
            float maxDuration = 0f;

            // Fire all steps in the state
            foreach (var s in def.steps){
                float stepEnd = s.delay + s.duration;
                if (stepEnd > maxDuration)
                    maxDuration = stepEnd;

                LightOneBySlot(
                    s.padIndex,
                    s.colorSlot,
                    Mathf.Max(0.01f, s.duration),
                    Mathf.Max(0f, s.delay),
                    s.stayOn
                );
            }

            // Wait until the longest step finishes (pause-aware)
            if (maxDuration > 0f)
                yield return StartCoroutine(WaitOrPause(maxDuration));
            else
                yield return null;

        } while (loop);

        // Cleanup when loop ends
        if (activeStateLoops.ContainsKey(originIdx))
            activeStateLoops.Remove(originIdx);
    }

    // Handle a single pad light change
    IEnumerator LightWithMatCo(Pad pad, int idx, Material mat, float dur, float delay, bool stayOn){
        // Optional delay (pause-aware)
        if (delay > 0f)
            yield return StartCoroutine(WaitOrPause(delay));

        var rend = pad.rend;

        // Keep light on permanently
        if (stayOn) {
            if (mat) {
                rend.material = mat;
                pad.baseMaterial = mat;
            }

            if (activeLightRoutines.ContainsKey(idx) &&
                activeLightRoutines[idx] == ((Coroutine)null))
            {
                activeLightRoutines.Remove(idx);
            }
            yield break;
        }

        // Apply light material
        if (mat) rend.material = mat;

        // Light duration (pause-aware)
        yield return StartCoroutine(WaitOrPause(dur));

        // Restore original material
        if (pad.baseMaterial)
            rend.material = pad.baseMaterial;

        if (activeLightRoutines.ContainsKey(idx))
            activeLightRoutines.Remove(idx);
    }

    // Safe material lookup from palette
    Material SafeGet(int slot){
        if (palette == null || slot < 0 || slot >= palette.Length || palette[slot] == null)
            return (palette != null && palette.Length > 0) ? palette[0] : null;

        return palette[slot];
    }
}
