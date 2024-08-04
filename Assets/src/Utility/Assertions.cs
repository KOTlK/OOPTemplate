using System.Diagnostics;
using Debug = UnityEngine.Debug;

public static class Assertions {
    [Conditional("UNITY_ASSERTIONS")]
    public static void Assert(bool expr) {
        if(!expr) {
            Debug.LogError("Assertion Failed");
        }
    }

    [Conditional("UNITY_ASSERTIONS")]
    public static void Assert(bool expr, string errorMessage) {
        if(!expr) {
            Debug.LogError(errorMessage);
        }
    }
}