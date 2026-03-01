using System;
// Unity can't serialize 64 bit flags... What a joke.
[Flags]
public enum EntityFlags : uint {
    None    = 0,
    Dynamic = 0x1,
    Ecs     = 0x2,
    EcsOnly = 0x4,
}