using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

public class IntrusiveList {
	public uint[] Next;
	public uint[] Prev;
	public uint   First;
	public uint   Capacity;

	public IntrusiveList(uint capacity) {
		Next     = new uint[capacity];
		Prev     = new uint[capacity];
		Capacity = capacity;
		First    = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(uint index) {
		Next[index] = First;
		Prev[First] = index;
		First       = index;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Remove(uint id) {
        var prev = Prev[id];
        var next = Next[id];

        if (First == id) {
            First = next;
        } else {
            Next[prev] = next;
        }

        if (next > 0) {
            Prev[next] = prev;
        }

        Next[id] = 0;
        Prev[id] = 0;
	}

	public void Resize(uint size) {
		Capacity = size;
		var sz = (int)size;
		Array.Resize(ref Next, sz);
		Array.Resize(ref Prev, sz);
	}

	public void Clear() {
		for (var i = 0; i < Capacity; i++) {
			Next[i]  = 0;
			Prev[i]  = 0;
			First    = 0;
		}
	}

	public IEnumerable<uint> Iterate() {
		var next = First;

		while(next > 0) {
			var current = next;
			next = Next[next];
			yield return current;
		}
	}
}