using UnityEngine;

[CreateAssetMenu(fileName="LightState", menuName="Launchpad/Light State")]
public class LightStateSO : ScriptableObject
{
    [System.Serializable] public struct Step {
        [Range(0,15)] public int padIndex;   // which pad
        [Range(0,8)]  public int colorSlot;  // 0..8, picks a material from LightDirector
        [Min(0.01f)]  public float duration; // seconds lit
        [Min(0f)]     public float delay;    // per-pad start delay
    }
    public Step[] steps;
}
