using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightDirector : MonoBehaviour
{
    public static LightDirector I { get; private set; }

    [Header("Auto-collected at Start")]
    List<Pad> allPads = new();
    Dictionary<int, Pad> padByIndex = new();
    
    // Tracks active light timers
    Dictionary<int, Coroutine> activeLightRoutines = new(); 
    
    // Tracks state loops
    Dictionary<int, Coroutine> activeStateLoops = new();

    [Header("9-Color Palette (Materials)")]
    public Material[] palette = new Material[9];

    void Awake(){ I = this; }

    void Start(){
        allPads.Clear();
        allPads.AddRange(FindObjectsOfType<Pad>());

        padByIndex.Clear();
        foreach (var p in allPads){
            if (!padByIndex.ContainsKey(p.index))
                padByIndex.Add(p.index, p);
        }
    }

    public Pad GetPadByIndex(int idx) {
        if (padByIndex.TryGetValue(idx, out var p)) return p;
        return null;
    }

    // Crucial fix: Allows us to stop a specific pad's loop before starting a new one
    public void StopStateLoop(int padIndex){
        if (activeStateLoops.TryGetValue(padIndex, out var routine)) {
            if(routine != null) StopCoroutine(routine);
            activeStateLoops.Remove(padIndex);
        }
    }

    public void LightOneBySlot(int padIndex, int slot, float duration, float delay = 0f, bool stayOn = false){
        if (!padByIndex.TryGetValue(padIndex, out var p)) return;
        
        // --- STUCK LIGHT FIX ---
        if (activeLightRoutines.ContainsKey(padIndex)) {
            // 1. Stop the running timer
            if(activeLightRoutines[padIndex] != null) StopCoroutine(activeLightRoutines[padIndex]);
            activeLightRoutines.Remove(padIndex);

            // 2. Force reset to base material immediately
            if (p.rend && p.baseMaterial) p.rend.material = p.baseMaterial;
        }
        // -----------------------

        var routine = StartCoroutine(LightWithMatCo(p, padIndex, SafeGet(slot), duration, delay, stayOn));
        activeLightRoutines[padIndex] = routine;
    }

    public void TriggerState(int originPadIndex, LightStateSO def, bool loop = false){
        if (!def || def.steps == null) return;
        
        // Stop any old loop from this pad first
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

                LightOneBySlot(s.padIndex, s.colorSlot,
                               Mathf.Max(0.01f, s.duration),
                               Mathf.Max(0f, s.delay),
                               s.stayOn);
            }

            if (maxDuration > 0f) yield return new WaitForSeconds(maxDuration);
            else yield return null;

        } while (loop); 
        
        if(activeStateLoops.ContainsKey(originIdx)) activeStateLoops.Remove(originIdx);
    }

    IEnumerator LightWithMatCo(Pad pad, int idx, Material mat, float dur, float delay, bool stayOn){
        if (delay > 0f) yield return new WaitForSeconds(delay);

        var rend = pad.rend;

        // "Stay On" updates the Base Material so we know this is the new normal
        if (stayOn) {
            if (mat) {
                rend.material = mat;
                pad.baseMaterial = mat; 
            }
            if (activeLightRoutines.ContainsKey(idx) && activeLightRoutines[idx] == ((Coroutine)null))
                 activeLightRoutines.Remove(idx);
            yield break; 
        }

        if (mat) rend.material = mat;

        yield return new WaitForSeconds(dur);

        // Revert to whatever the base is
        if (pad.baseMaterial) rend.material = pad.baseMaterial;
        
        if (activeLightRoutines.ContainsKey(idx)) activeLightRoutines.Remove(idx);
    }

    Material SafeGet(int slot){
        if (palette == null || slot < 0 || slot >= palette.Length || palette[slot] == null)
            return (palette != null && palette.Length > 0) ? palette[0] : null;
        return palette[slot];
    }
}