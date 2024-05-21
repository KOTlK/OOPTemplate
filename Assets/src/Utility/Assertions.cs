using System.Diagnostics;
using Debug = UnityEngine.Debug;
using TMPro;

public static class Assertions {
    [Conditional("UNITY_ASSERTIONS")]
    public static void Assert(bool expr) {
        if(!expr) {
            Debug.LogError("Assertion Failed");
        }
    }
}