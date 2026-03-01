using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Pool;

using static Assertions;

public delegate void ForEachDelegate<T>(ref T c0);
public delegate void ForEachDelegate<T0, T1>(ref T0 c0, ref T1 c1);
public delegate void ForEachDelegate<T0, T1, T2>(ref T0 c0, ref T1 c1, ref T2 c2);
public delegate void ForEachDelegate<T0, T1, T2, T3>(ref T0 c0, ref T1 c1, ref T2 c2, ref T3 c3);
public delegate void ForEachDelegate<T0, T1, T2, T3, T4>(ref T0 c0, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4);
public delegate void ForEachDelegate<T0, T1, T2, T3, T4, T5>(ref T0 c0, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5);

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
	public struct QueryResult {
		public List<BitSet> Archetypes;
	}

	public IComponentTable[]      		   Tables;
	public Dictionary<Type, uint> 		   BitByType;
	public Dictionary<BitSet, List<uint>>  EntitiesByArchetype;
	public Dictionary<Type, BitSet>        ArchetypeByType;
	public Dictionary<BitSet, QueryResult> QueryCache;
	public uint 						   ComponentsCount;

	private EntityManager _em;

	private readonly BitSet 	  _emptyBitset;
	private readonly Pool<BitSet> _bitsetPool;

	public Ecs(EntityManager em) {
		_em                 = em;
		em.Ecs              = this;
		Tables              = null;
		BitByType           = null;
		EntitiesByArchetype = null;
		ArchetypeByType     = null;
		QueryCache          = null;
		BitByType           = new();
		EntitiesByArchetype = new();
		ArchetypeByType     = new();
		QueryCache          = new();
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
		_bitsetPool = new(() => {
			return new BitSet(ComponentsCount);
		},
		(BitSet bitset) => {
			bitset.ClearAll();
		});
		// _tempBitset  = new(ComponentsCount);
		// _tempBitset2  = new(ComponentsCount);
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
			var a = arch.Copy();
			EntitiesByArchetype.Add(a, elist); // make copy of archetype so it doesn't refere to the same underlying array.

			CleanupQueries(BitByType[typeof(T)]);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CleanupQueries(uint componentBit) {
		var removeList = ListPool<BitSet>.Get();
		var mask = _bitsetPool.Get();
		mask.SetBit(componentBit);

		foreach(var (qArch, query) in QueryCache) {
			var result = _bitsetPool.Get();
			qArch.And(mask, result);

			if (mask == result) {
				removeList.Add(qArch);
			}

			_bitsetPool.Release(result);
		}

		_bitsetPool.Release(mask);

		foreach (var arch in removeList) {
			QueryCache.Remove(arch);
		}

		removeList.Clear();

		ListPool<BitSet>.Release(removeList);
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
			CleanupQueries(BitByType[typeof(T)]);
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
		var mask = _bitsetPool.Get();
		mask.SetBit(BitByType[type0]);

		var query = Query(mask);

		foreach(var arch in query.Archetypes) {
			var list = EntitiesByArchetype[arch];

			for (var i = 0; i < list.Count; i++) {
				del.Invoke(ref table0.Get(list[i]));
			}
		}

		_bitsetPool.Release(mask);
	}

	public void ForEach<T0, T1>(ForEachDelegate<T0, T1> del) 
	where T0 : struct 
	where T1 : struct {
		var type0 = typeof(T0);
		Assert(BitByType.ContainsKey(type0), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type0.FullName);
		var type1 = typeof(T1);
		Assert(BitByType.ContainsKey(type1), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type1.FullName);

		var mask = _bitsetPool.Get();

		mask.SetBit(BitByType[type0]);
		mask.SetBit(BitByType[type1]);

		var query = Query(mask);

		var table0 = (ComponentTable<T0>)Tables[BitByType[type0]];
		var table1 = (ComponentTable<T1>)Tables[BitByType[type1]];

		foreach(var arch in query.Archetypes) {
			var list = EntitiesByArchetype[arch];

			for (var i = 0; i < list.Count; i++) {
				var entity = list[i];
				del.Invoke(ref table0.Get(entity), ref table1.Get(entity));
			}
		}

		_bitsetPool.Release(mask);
	}

	public void ForEach<T0, T1, T2>(ForEachDelegate<T0, T1, T2> del) 
	where T0 : struct 
	where T1 : struct 
	where T2 : struct {
		var type0 = typeof(T0);
		Assert(BitByType.ContainsKey(type0), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type0.FullName);
		var type1 = typeof(T1);
		Assert(BitByType.ContainsKey(type1), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type1.FullName);
		var type2 = typeof(T2);
		Assert(BitByType.ContainsKey(type2), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type2.FullName);

		var mask = _bitsetPool.Get();

		mask.SetBit(BitByType[type0]);
		mask.SetBit(BitByType[type1]);
		mask.SetBit(BitByType[type2]);

		var query = Query(mask);

		var table0 = (ComponentTable<T0>)Tables[BitByType[type0]];
		var table1 = (ComponentTable<T1>)Tables[BitByType[type1]];
		var table2 = (ComponentTable<T2>)Tables[BitByType[type2]];

		foreach(var arch in query.Archetypes) {
			var list = EntitiesByArchetype[arch];

			for (var i = 0; i < list.Count; i++) {
				var entity = list[i];
				del.Invoke(ref table0.Get(entity), ref table1.Get(entity), ref table2.Get(entity));
			}
		}

		_bitsetPool.Release(mask);
	}

	public void ForEach<T0, T1, T2, T3>(ForEachDelegate<T0, T1, T2, T3> del) 
	where T0 : struct 
	where T1 : struct 
	where T2 : struct 
	where T3 : struct {
		var type0 = typeof(T0);
		Assert(BitByType.ContainsKey(type0), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type0.FullName);
		var type1 = typeof(T1);
		Assert(BitByType.ContainsKey(type1), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type1.FullName);
		var type2 = typeof(T2);
		Assert(BitByType.ContainsKey(type2), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type2.FullName);
		var type3 = typeof(T3);
		Assert(BitByType.ContainsKey(type3), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type3.FullName);

		var mask = _bitsetPool.Get();

		mask.SetBit(BitByType[type0]);
		mask.SetBit(BitByType[type1]);
		mask.SetBit(BitByType[type2]);
		mask.SetBit(BitByType[type3]);

		var query = Query(mask);

		var table0 = (ComponentTable<T0>)Tables[BitByType[type0]];
		var table1 = (ComponentTable<T1>)Tables[BitByType[type1]];
		var table2 = (ComponentTable<T2>)Tables[BitByType[type2]];
		var table3 = (ComponentTable<T3>)Tables[BitByType[type3]];

		foreach(var arch in query.Archetypes) {
			var list = EntitiesByArchetype[arch];

			for (var i = 0; i < list.Count; i++) {
				var entity = list[i];
				del.Invoke(ref table0.Get(entity), 
						   ref table1.Get(entity), 
						   ref table2.Get(entity),
						   ref table3.Get(entity));
			}
		}

		_bitsetPool.Release(mask);
	}

	public void ForEach<T0, T1, T2, T3, T4>(ForEachDelegate<T0, T1, T2, T3, T4> del) 
	where T0 : struct 
	where T1 : struct 
	where T2 : struct 
	where T3 : struct 
	where T4 : struct {
		var type0 = typeof(T0);
		Assert(BitByType.ContainsKey(type0), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type0.FullName);
		var type1 = typeof(T1);
		Assert(BitByType.ContainsKey(type1), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type1.FullName);
		var type2 = typeof(T2);
		Assert(BitByType.ContainsKey(type2), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type2.FullName);
		var type3 = typeof(T3);
		Assert(BitByType.ContainsKey(type3), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type3.FullName);
		var type4 = typeof(T4);
		Assert(BitByType.ContainsKey(type4), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type4.FullName);

		var mask = _bitsetPool.Get();

		mask.SetBit(BitByType[type0]);
		mask.SetBit(BitByType[type1]);
		mask.SetBit(BitByType[type2]);
		mask.SetBit(BitByType[type3]);
		mask.SetBit(BitByType[type4]);

		var query = Query(mask);

		var table0 = (ComponentTable<T0>)Tables[BitByType[type0]];
		var table1 = (ComponentTable<T1>)Tables[BitByType[type1]];
		var table2 = (ComponentTable<T2>)Tables[BitByType[type2]];
		var table3 = (ComponentTable<T3>)Tables[BitByType[type3]];
		var table4 = (ComponentTable<T4>)Tables[BitByType[type4]];

		foreach(var arch in query.Archetypes) {
			var list = EntitiesByArchetype[arch];

			for (var i = 0; i < list.Count; i++) {
				var entity = list[i];
				del.Invoke(ref table0.Get(entity), 
						   ref table1.Get(entity), 
						   ref table2.Get(entity),
						   ref table3.Get(entity),
						   ref table4.Get(entity));
			}
		}

		_bitsetPool.Release(mask);
	}

	public void ForEach<T0, T1, T2, T3, T4, T5>(ForEachDelegate<T0, T1, T2, T3, T4, T5> del) 
	where T0 : struct 
	where T1 : struct 
	where T2 : struct 
	where T3 : struct 
	where T4 : struct 
	where T5 : struct {
		var type0 = typeof(T0);
		Assert(BitByType.ContainsKey(type0), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type0.FullName);
		var type1 = typeof(T1);
		Assert(BitByType.ContainsKey(type1), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type1.FullName);
		var type2 = typeof(T2);
		Assert(BitByType.ContainsKey(type2), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type2.FullName);
		var type3 = typeof(T3);
		Assert(BitByType.ContainsKey(type3), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type3.FullName);
		var type4 = typeof(T4);
		Assert(BitByType.ContainsKey(type4), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type4.FullName);
		var type5 = typeof(T5);
		Assert(BitByType.ContainsKey(type4), "There is no corresponding table for type %. Did you mark your component with ComponentAttribute?", type5.FullName);

		var mask = _bitsetPool.Get();

		mask.SetBit(BitByType[type0]);
		mask.SetBit(BitByType[type1]);
		mask.SetBit(BitByType[type2]);
		mask.SetBit(BitByType[type3]);
		mask.SetBit(BitByType[type4]);
		mask.SetBit(BitByType[type5]);

		var query = Query(mask);

		var table0 = (ComponentTable<T0>)Tables[BitByType[type0]];
		var table1 = (ComponentTable<T1>)Tables[BitByType[type1]];
		var table2 = (ComponentTable<T2>)Tables[BitByType[type2]];
		var table3 = (ComponentTable<T3>)Tables[BitByType[type3]];
		var table4 = (ComponentTable<T4>)Tables[BitByType[type4]];
		var table5 = (ComponentTable<T5>)Tables[BitByType[type5]];

		foreach(var arch in query.Archetypes) {
			var list = EntitiesByArchetype[arch];

			for (var i = 0; i < list.Count; i++) {
				var entity = list[i];
				del.Invoke(ref table0.Get(entity), 
						   ref table1.Get(entity), 
						   ref table2.Get(entity),
						   ref table3.Get(entity),
						   ref table4.Get(entity),
						   ref table5.Get(entity));
			}
		}

		_bitsetPool.Release(mask);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private QueryResult Query(BitSet mask) {
		if (QueryCache.ContainsKey(mask) == false) {
			var result = new QueryResult();
			result.Archetypes = new();
			var and = _bitsetPool.Get();

			foreach (var (arch, list) in EntitiesByArchetype) {
				arch.And(mask, and);

				if (and != mask) {
					and.ClearAll();
					continue;
				}

				result.Archetypes.Add(arch.Copy());
				and.ClearAll();
			}

			_bitsetPool.Release(and);

			QueryCache.Add(mask.Copy(), result);
			return result;
		} else {
			return QueryCache[mask];
		}
	}
}