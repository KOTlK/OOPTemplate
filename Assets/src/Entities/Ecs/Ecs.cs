using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

using static Assertions;

public delegate void ForEachDelegate<T>(ref T c0);
public delegate void ForEachDelegate<T0, T1>(ref T0 c0, ref T1 c1);

public class ComponentAttribute : Attribute {
}

[Component]
public struct TestComponent {
	public Vector2 Position;
	public Vector2 Size;
}

[Component]
public struct TestComponent2 {
	public int c;
	public uint b;
}

[UnityEngine.Scripting.Preserve]
public class Ecs {
	public IComponentTable[]      		  Tables;
	public Dictionary<Type, uint> 		  BitByType;
	public Dictionary<BitSet, List<uint>> EntitiesByArchetype;
	public Dictionary<Type, BitSet>       ArchetypeByType;
	public uint 						  ComponentsCount;

	private EntityManager _em;

	private readonly BitSet _emptyBitset;
	// Used to avoid gc. Not thread safe.
	private readonly BitSet _tempBitset; // !!! clear after use !!!

	public Ecs(EntityManager em) {
		_em                 = em;
		em.Ecs              = this;
		Tables              = null;
		BitByType           = null;
		EntitiesByArchetype = null;
		ArchetypeByType     = null;
		BitByType           = new();
		EntitiesByArchetype = new();
		ArchetypeByType     = new();
		EntitiesByArchetype.Clear();
		ArchetypeByType.Clear();
		var  ass         = Assembly.GetExecutingAssembly();
		var  tables      = new List<IComponentTable>();
		uint index       = 0;

		foreach(var type in ass.GetTypes()) {
			if (type.IsValueType == false) continue;

			var attr = type.GetCustomAttribute<ComponentAttribute>();
			if (attr != null) {
				var tableType = typeof(ComponentTable<>);
				var concreteTableType = tableType.MakeGenericType(type);
				var ctor  = concreteTableType.GetConstructor(new Type[]{typeof(uint)});
				var id    = index++;
				var table = (IComponentTable)ctor.Invoke(new object[] {id});

				BitByType.Add(type, id);
				tables.Add(table);
			}
		}

		ComponentsCount = (uint)tables.Count;

		for(uint i = 0; i < ComponentsCount; i++) {
			var bitset = new BitSet(ComponentsCount);
			bitset.SetBit(i);
			ArchetypeByType.Add(tables[(int)i].GetComponentType(), bitset);
		}

		Tables = tables.ToArray();
		_emptyBitset = new(ComponentsCount);
		EntitiesByArchetype.Add(_emptyBitset, new ()); // make empty archetype
		_tempBitset  = new(ComponentsCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public EntityHandle CreateEntity() {
		var h = _em.CreatePureEcsEntity();
		_em.MakeEmptyBitset(h, ComponentsCount);

		EntitiesByArchetype[_emptyBitset].Add(h.Id);
		return h;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DestroyEntity(EntityHandle handle) {
		ClearComponents(handle);

		_em.DestroyEntity(handle, true);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ClearComponents(EntityHandle handle) {
		var arch = _em.GetArchetype(handle);
		EntitiesByArchetype[arch].Remove(handle.Id);

		for(uint i = 0; i < ComponentsCount; i++) {
			if (arch.TestBit(i)) {
				Debug.Log($"Removing {i} from {handle.Id}");
				var table = Tables[i];
				table.Remove(handle.Id);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ComponentTable<T> GetComponentTable<T>() 
	where T : struct {
		return (ComponentTable<T>)Tables[BitByType[typeof(T)]];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AddComponent<T>(EntityHandle handle, T component = default(T)) 
	where T : struct {
		var table = GetComponentTable<T>();

		Assert(_em.IsEcsEntity(handle), "Cannot add component to non ecs entity, add Ecs or EcsOnly flags to an entity");

		var arch = _em.GetArchetype(handle);
		EntitiesByArchetype[arch].Remove(handle.Id);
		table.Add(handle.Id, component);
		_em.SetComponentBit(handle.Id, BitByType[typeof(T)]);
		arch = _em.GetArchetype(handle);

		if (EntitiesByArchetype.TryGetValue(arch, out var list)) {
			list.Add(handle.Id);
		} else {
			var elist = new List<uint>();
			elist.Add(handle.Id);
			EntitiesByArchetype.Add(arch.Copy(), elist); // make copy of archetype so it doesn't refere to the same underlying array.
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RemoveComponent<T>(EntityHandle handle) 
	where T : struct {
		var table = GetComponentTable<T>();

		var arch = _em.GetArchetype(handle);
		EntitiesByArchetype[arch].Remove(handle.Id);
		table.Remove(handle.Id);
		_em.ClearComponentBit(handle.Id, BitByType[typeof(T)]);
		arch = _em.GetArchetype(handle);

		if (EntitiesByArchetype.TryGetValue(arch, out var list)) {
			list.Add(handle.Id);
		} else {
			var elist = new List<uint>();
			elist.Add(handle.Id);
			EntitiesByArchetype.Add(arch.Copy(), elist);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool HasComponent<T>(EntityHandle handle) {
		var arch = _em.GetArchetype(handle);

		return arch.TestBit(BitByType[typeof(T)]);
	}

	public void ForEach<T>(ForEachDelegate<T> del) 
	where T : struct {
		var type0 = typeof(T);
		Assert(BitByType.ContainsKey(type0), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type0.FullName);
		var table0 = (ComponentTable<T>)Tables[BitByType[type0]];

		var c = table0.GetComponentsCount();

		for(var i = 1; i < c; i++) {
			del.Invoke(ref table0.Dense[i]);
		}
	}

	public void ForEach<T0, T1>(ForEachDelegate<T0, T1> del) 
	where T0 : struct 
	where T1 : struct {
		var type0 = typeof(T0);
		Assert(BitByType.ContainsKey(type0), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type0.FullName);
		var type1 = typeof(T1);
		Assert(BitByType.ContainsKey(type1), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type1.FullName);

		_tempBitset.SetBit(BitByType[type0]);
		_tempBitset.SetBit(BitByType[type1]);

		if (EntitiesByArchetype.TryGetValue(_tempBitset, out var entities)) {
			var table0 = (ComponentTable<T0>)Tables[BitByType[type0]];
			var table1 = (ComponentTable<T1>)Tables[BitByType[type1]];

			for(var i = 0; i < entities.Count; i++) {
				var entity = entities[i];
				ref var c0 = ref table0.Get(entity);
				ref var c1 = ref table1.Get(entity);
				del(ref c0, ref c1);
			}
		}

		_tempBitset.ClearAll();
	}
}