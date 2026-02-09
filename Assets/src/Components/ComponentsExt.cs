public static class ComponentsExt {
    public static bool HasComponent<T>(this EntityHandle h)
    where T : struct {
        return ComponentSystem<T>.Has(h);
    }

    public static void AddComponent<T>(this EntityHandle h, T component = default(T))
    where T : struct {
        ComponentSystem<T>.Add(h, component);
    }

    public static void RemoveComponent<T>(this EntityHandle h)
    where T : struct {
        ComponentSystem<T>.Remove(h);
    }

    public static bool HasComponent<T>(this Entity e)
    where T : struct {
        return ComponentSystem<T>.Has(e.Handle);
    }

    public static void AddComponent<T>(this Entity e, T component = default(T))
    where T : struct {
        ComponentSystem<T>.Add(e.Handle, component);
    }

    public static void RemoveComponent<T>(this Entity e)
    where T : struct {
        ComponentSystem<T>.Remove(e.Handle);
    }
}