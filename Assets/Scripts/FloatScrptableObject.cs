using UnityEngine;

[CreateAssetMenu(fileName = "FloatScrptableObject", menuName = "Base", order = 0)]
public class FloatScrptableObject : ScriptableObject
{
    public float value;
    public float nonChangeValue;
    public bool useDynamicValue;

    public float GetValue()
    {
        if (useDynamicValue)
        {
            return value;
        }
        else
        {
            return nonChangeValue;
        }
    }
}