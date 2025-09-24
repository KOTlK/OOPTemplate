using UnityEngine;

public static class Context {
    public static Arena         SingleFrameArena;
    public static EntityManager GameplayEntityManager;

    public static void InitContext(EntityManager gameplayEntityManager) {
        SingleFrameArena      = new Arena(65536);
        GameplayEntityManager = gameplayEntityManager;
    }

    public static void DestroyContext() {
        SingleFrameArena.Dispose();
    }

    public static EntityManager GetGameplayEntityManager() {
        return GameplayEntityManager;
    }
}