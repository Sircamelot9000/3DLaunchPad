using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LightDirector : MonoBehaviour
{
    public static LightDirector I { get; private set; }

    [Header("Auto-collected at Start")]
    List<Pad> allPads = new();
    Dictionary<int, Pad> padByIndex = new();      // lookup by Pad.index

    [Header("9-Color Palette (Materials)")]
    public Material[] palette = new Material[9];  // drag MAT_0..MAT_8 here

    void Awake(){ I = this; }

    void Start(){
        allPads.Clear();
        allPads.AddRange(FindObjectsOfType<Pad>());

        padByIndex.Clear();
        foreach (var p in allPads){
            if (!padByIndex.ContainsKey(p.index))
                padByIndex.Add(p.index, p);
            else
                Debug.LogWarning($"[LightDirector] Duplicate pad index {p.index} on {p.name}");
        }
    }

    public void LightOneBySlot(int padIndex, int slot, float duration, float delay = 0f){
        if (!padByIndex.TryGetValue(padIndex, out var p)) return;
        var mat = SafeGet(slot);
        StartCoroutine(LightWithMatCo(p, mat, duration, delay));
    }

    public void TriggerState(LightStateSO def){
        if (!def || def.steps == null) return;
        foreach (var s in def.steps){
            LightOneBySlot(s.padIndex, s.colorSlot,
                           Mathf.Max(0.01f, s.duration),
                           Mathf.Max(0f, s.delay));
        }
    }

    IEnumerator LightWithMatCo(Pad pad, Material mat, float dur, float delay){
        if (delay > 0f) yield return new WaitForSeconds(delay);

        var rend = pad.rend;
        var original = rend.material;  // capture instance
        if (mat) rend.material = mat;

        yield return new WaitForSeconds(dur);

        // restore (note: last-wins if multiple lights overlap)
        rend.material = original;
    }

    Material SafeGet(int slot){
        if (palette == null || slot < 0 || slot >= palette.Length || palette[slot] == null)
            return (palette != null && palette.Length > 0) ? palette[0] : null;
        return palette[slot];
    }
}
