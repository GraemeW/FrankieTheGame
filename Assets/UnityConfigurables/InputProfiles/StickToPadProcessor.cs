using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
[InitializeOnLoad]
#endif

public class StickToPadProcessor : InputProcessor<Vector2>
{
    // Constants
    private const float _lowerSplit = 0.4142135624f;   // :: Mathf.Sqrt(2) - 1;
    private const float _upperSplit = 2.4142135624f;   // :: Mathf.Sqrt(2) + 1;
    private const float _angleOutput = 0.7071067812f;  // :: 1 / Mathf.Sqrt(2);
    
    #if UNITY_EDITOR
    static StickToPadProcessor()
    {
        Initialize();
    }
    #endif

    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
        InputSystem.RegisterProcessor<StickToPadProcessor>();
    }
        
    public override Vector2 Process(Vector2 value, InputControl control)
    { 
        float stickRatio = Mathf.Abs(value.y) / Mathf.Abs(value.x);
        Vector2 temp = stickRatio switch
        {
            < _lowerSplit => Vector2.right * Mathf.Sign(value.x),
            > _upperSplit => Vector2.up * Mathf.Sign(value.y),
            _ => new Vector2(Mathf.Sign(value.x) * _angleOutput, Mathf.Sign(value.y) * _angleOutput)
        };
        
        UnityEngine.Debug.Log($"Input as {value} -> output as {temp}");
        
        return temp;
    }
}
