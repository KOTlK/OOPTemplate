using UnityEngine;

public static class Clock {
    public static float Delta; //usually used for gameplay code
    public static float RealTimeDelta; //usually used for ui / non-gameplay code
    public static float FixedDelta;
    public static float Time;
    public static float RealTime;
    public static float Scale = 1f;
    public static int   FrameCount;
    
    public static void Update() {
        var dt = UnityEngine.Time.unscaledDeltaTime;
        RealTimeDelta = dt;
        Delta         = dt * Scale;
        FixedDelta    = UnityEngine.Time.fixedDeltaTime;
        Time          += Delta;
        RealTime      += dt;
        FrameCount++;
    }
}