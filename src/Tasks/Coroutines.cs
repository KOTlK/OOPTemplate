using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CoroutineHandle {
    public int Id;
    public int Tag;
}

public enum CoroutineStatus : byte {
    Running = 0,
    Stopped = 1,
}

public static class Coroutines {
    public struct Slot {
        public IEnumerator Enumerator;
        public int         Tag;
    }

    public static Slot[]     Slots;
    public static Stack<int> FreeSlots;
    public static int        Max;

    private const int StartSlotsCount = 256;

    public static void InitCoroutines() {
        FreeSlots = new();
        FreeSlots.Clear();
        Slots = null;
        Slots = new Slot[StartSlotsCount];
        Max = 0;
    }

    // Not Start but Begin, because c# static dispatch can suck my balls.
    public static CoroutineHandle BeginCoroutine(IEnumerator func) {
        var handle = new CoroutineHandle();

        int id;

        if(FreeSlots.Count > 0) {
            id = FreeSlots.Pop();
        } else {
            id = Max++;
        }

        Slots[id].Enumerator = func;

        handle.Tag = Slots[id].Tag;
        handle.Id  = id;

        return handle;
    }

    public static void EndCoroutine(CoroutineHandle handle) {
        if(Slots[handle.Id].Tag == handle.Tag && Slots[handle.Id].Enumerator != null) {
            Stop(handle.Id);
        }
    }

    public static CoroutineStatus GetCoroutineStatus(CoroutineHandle handle) {
        if(Slots[handle.Id].Tag == handle.Tag && Slots[handle.Id].Enumerator != null) {
            return CoroutineStatus.Running;
        }

        return CoroutineStatus.Stopped;
    }

    public static void RunCoroutines() {
        for(var i = 0; i < Max; ++i) {
            if(Slots[i].Enumerator != null) {
                var run = RunEnumeratorRecursively(Slots[i].Enumerator);

                if(!run) {
                    Stop(i);
                }
            }
        }
    }

    public static IEnumerator Wait(float sec) {
        var time = Time.time + sec;

        while(Time.time < time) {
            yield return null;
        }
    }

    public static IEnumerator WaitRealTime(float sec) {
        var time = Time.unscaledTime + sec;

        while(Time.unscaledTime < time) {
            yield return null;
        }
    }

    private static void Stop(int id) {
        Slots[id].Enumerator = null;
        Slots[id].Tag++;
        FreeSlots.Push(id);
    }

    private static void Resize(int newLen) {
        Array.Resize(ref Slots, newLen);
    }

    private static bool RunEnumeratorRecursively(IEnumerator e) {
        switch(e.Current) {
            case IEnumerator nested : {
                if(RunEnumeratorRecursively(nested)) {
                    return true;
                } else {
                    return e.MoveNext();
                }
            }
            case AsyncOperation op : {
                if(op.isDone) return e.MoveNext();

                return true;
            }
        }

        return e.MoveNext();
    }
}