using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightDirector : MonoBehaviour
{
    public static LightDirector I { get; private set; }

    List<Pad> allPads = new();
    Dictionary<int, Pad> padByIndex = new();
    Dictionary<int, Coroutine> activeLightRoutines = new(); 
    Dictionary<int, Coroutine> activeStateLoops = new();

    public Material[] palette = new Material[9];

    void Awake(){ I = this; }

    void Start(){
        allPads.Clear();
        allPads.AddRange(FindObjectsOfType<Pad>());
        padByIndex.Clear();
        foreach (var p in allPads){
            if (!padByIndex.ContainsKey(p.index)) padByIndex.Add(p.index, p);
        }
    }

    // --- NEW SMART WAIT HELPER ---
    // This effectively "freezes" time for the lights when PauseManager is active
    IEnumerator WaitOrPause(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            // Only advance timer if NOT paused
            if (PauseManager.I == null || !PauseManager.I.isPaused)
            {
                timer += Time.deltaTime;
            }
            yield return null;
        }
    }
    // -----------------------------

    public void StopStateLoop(int padIndex){
        if (activeStateLoops.TryGetValue(padIndex, out var routine)) {
            if(routine != null) StopCoroutine(routine);
            activeStateLoops.Remove(padIndex);
        }
    }

    public void LightOneBySlot(int padIndex, int slot, float duration, float delay = 0f, bool stayOn = false){
        if (!padByIndex.TryGetValue(padIndex, out var p)) return;
        
        if (activeLightRoutines.ContainsKey(padIndex)) {
            if(activeLightRoutines[padIndex] != null) StopCoroutine(activeLightRoutines[padIndex]);
            activeLightRoutines.Remove(padIndex);
            if (p.rend && p.baseMaterial) p.rend.material = p.baseMaterial;
        }

        var routine = StartCoroutine(LightWithMatCo(p, padIndex, SafeGet(slot), duration, delay, stayOn));
        activeLightRoutines[padIndex] = routine;
    }

    public void TriggerState(int originPadIndex, LightStateSO def, bool loop = false){
        if (!def || def.steps == null) return;
        StopStateLoop(originPadIndex);
        var routine = StartCoroutine(RunStateLoop(def, loop, originPadIndex));
        activeStateLoops[originPadIndex] = routine;
    }

    IEnumerator RunStateLoop(LightStateSO def, bool loop, int originIdx){
        do {
            float maxDuration = 0f;
            foreach (var s in def.steps){
                float stepEnd = s.delay + s.duration;
                if(stepEnd > maxDuration) maxDuration = stepEnd;

                // Fire individual lights
                LightOneBySlot(s.padIndex, s.colorSlot,
                               Mathf.Max(0.01f, s.duration),
                               Mathf.Max(0f, s.delay),
                               s.stayOn);
            }

            // USE SMART WAIT INSTEAD OF WAITFORSECONDS
            if (maxDuration > 0f) yield return StartCoroutine(WaitOrPause(maxDuration));
            else yield return null;

        } while (loop); 
        
        if(activeStateLoops.ContainsKey(originIdx)) activeStateLoops.Remove(originIdx);
    }

    IEnumerator LightWithMatCo(Pad pad, int idx, Material mat, float dur, float delay, bool stayOn){
        // Delay with pause support
        if (delay > 0f) yield return StartCoroutine(WaitOrPause(delay));

        var rend = pad.rend;

        if (stayOn) {
            if (mat) { rend.material = mat; pad.baseMaterial = mat; }
            if (activeLightRoutines.ContainsKey(idx) && activeLightRoutines[idx] == ((Coroutine)null))
                 activeLightRoutines.Remove(idx);
            yield break; 
        }

        if (mat) rend.material = mat;

        // Duration with pause support
        yield return StartCoroutine(WaitOrPause(dur));

        if (pad.baseMaterial) rend.material = pad.baseMaterial;
        if (activeLightRoutines.ContainsKey(idx)) activeLightRoutines.Remove(idx);
    }

    Material SafeGet(int slot){
        if (palette == null || slot < 0 || slot >= palette.Length || palette[slot] == null)
            return (palette != null && palette.Length > 0) ? palette[0] : null;
        return palette[slot];
    }
}