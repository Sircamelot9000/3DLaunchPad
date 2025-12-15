using UnityEngine;

[CreateAssetMenu(fileName="LightState", menuName="Launchpad/Light State")]
public class LightStateSO : ScriptableObject
{
    [System.Serializable] public struct Step {
        [Range(0,63)] public int padIndex;   
        [Range(0,8)]  public int colorSlot;  
        [Min(0.01f)]  public float duration; 
        [Min(0f)]     public float delay;    
        public bool stayOn; 
    }
    public Step[] steps;
}