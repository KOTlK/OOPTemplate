using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;

public unsafe struct Region : IDisposable {
    public byte   *Data;
    public Region *Next;
    public long    Allocated;
    public long    Capacity;

    private const long MinSize = 65536;

    public void Init(long capacity) {
        var size = MinSize;
        if(MinSize < capacity) {
            size = capacity;
        }

#if UNITY_EDITOR_WIN
        Data = (byte*)MallocTracked(size, AlignOf<byte>(), Allocator.Persistent, 0);
#else
        Data = (byte*)Malloc(size, AlignOf<byte>(), Allocator.Persistent);
#endif

        Allocated = 0;
        Capacity  = size;
        Next      = null;
    }

    public void Dispose() {
#if UNITY_EDITOR_WIN
        FreeTracked((void*)Data, Allocator.Persistent);
#else
        UnsafeUtility.Free((void*)Data, Allocator.Persistent);
#endif

        if(Next != null) {
            Next->Dispose();
#if UNITY_EDITOR_WIN
            FreeTracked((void*)Next, Allocator.Persistent);
#else
            UnsafeUtility.Free((void*)Next, Allocator.Persistent);
#endif
        }
    }

    public void Free() {
        Allocated = 0;

        if(Next != null) {
            Next->Free();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T *Alloc<T>(long count)
    where T : unmanaged {
        return (T*)Alloc(sizeof(T) * count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void *Alloc(long size) {
        if(Allocated + size > Capacity) {
            if(Next == null) {
#if UNITY_EDITOR_WIN
                Next = (Region*)MallocTracked(sizeof(Region), AlignOf<Region>(), Allocator.Persistent, 0);
#else
                Next = (Region*)Malloc(sizeof(Region), AlignOf<Region>(), Allocator.Persistent);
#endif
                Next->Init(size);
            }

            return Next->Alloc(size);
        } else {
            var data = (void*)(Data + Allocated);
            Allocated += size;

            return data;
        }
    }
}

public unsafe struct Arena {
    public Region *Root;

    public Arena(long size) {
#if UNITY_EDITOR_WIN
        Root = (Region*)MallocTracked(sizeof(Region), AlignOf<Region>(), Allocator.Persistent, 0);
#else
        Root = (Region*)Malloc(sizeof(Region), AlignOf<Region>(), Allocator.Persistent);
#endif

        Root->Init(size);
    }

    public void Dispose() {
        if(Root == null)
            return;

        Root->Dispose();

#if UNITY_EDITOR_WIN
        FreeTracked((void*)Root, Allocator.Persistent);
#else
        UnsafeUtility.Free((void*)Root, Allocator.Persistent);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T *Alloc<T>(uint count)
    where T : unmanaged {
        return Root->Alloc<T>(count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void *Alloc(uint size) {
        return Root->Alloc(size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free() {
        Root->Free();
    }
}