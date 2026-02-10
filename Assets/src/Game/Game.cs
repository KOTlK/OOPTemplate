using System;
using System.Collections.Generic;
using Reflex.Core;
using Reflex.Attributes;

using static Assertions;

public class Game {
	[Inject] private Container _container;

	private List<GameSystem> 			 _systems = new();
	private Dictionary<Type, GameSystem> _systemByType = new();

	public void Update() {
		foreach(var system in _systems) {
		// As all systems are in array, iterating them should be fast due to pointer prefetching and i can simply check flag.
			if (system.Enabled == false) continue;

			system.Update();
		}
	}

	public void Destroy() {
		foreach(var system in _systems) {
			system.Destroy();
		}
	}

	public void AppendSystem(GameSystem system) {
		_systems.Add(system);
		var type = system.GetType();

		_systemByType.Add(type, system);
	}

	public T GetSystem<T>() 
	where T : GameSystem {
		var type = typeof(T);

		Assert(_systemByType.ContainsKey(type), "Cannot get system. It is not registered.");

		return (T)_systemByType[type];
	}

	public void EnableSystem<T>() 
	where T : GameSystem {
		var type = typeof(T);
		Assert(_systemByType.ContainsKey(type), "Cannot enable system. It is not registered.");
		var system = _systemByType[type];
		Assert(system.Enabled == false, "Cannot enable system. It is already enabled.");
		system.OnEnable();
		system.Enabled = true;
	}

	public void DisableSystem<T>() 
	where T : GameSystem {
		var type = typeof(T);
		Assert(_systemByType.ContainsKey(type), "Cannot disable system. It is not registered.");
		var system = _systemByType[type];
		Assert(system.Enabled == true, "Cannot disable system. It is already disabled.");
		system.OnDisable();
		system.Enabled = false;
	}

	public static void Quit() {
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		UnityEngine.Application.Quit();
#endif
	}
}