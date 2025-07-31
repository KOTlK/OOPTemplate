using UnityEngine;

public static class Context {
    public static Arena SingleFrameArena;

    public static void InitContext() {
        SingleFrameArena = new Arena(65536);
    }

    public static void DestroyContext() {
        SingleFrameArena.Dispose();
    }
}