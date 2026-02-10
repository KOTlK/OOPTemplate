using System.IO;
using System;
using UnityEngine;

public class ConfigLiveReloadSystem : GameSystem {
	private DateTime _lastUpdated = DateTime.MinValue;

	public ConfigLiveReloadSystem(Game game) : base(game, true) {
		if (!File.Exists(Config.Path)) return;

		_lastUpdated = File.GetLastWriteTime(Config.Path);
	}

	public override void Update() {
		var path = Config.Path;

		if (!File.Exists(Config.Path)) return;

		if (_lastUpdated == DateTime.MinValue) {
			Config.ParseVars();
			return;
		}

		var now = File.GetLastWriteTime(path);

		if (now != _lastUpdated) {
			Config.ParseVars();
			_lastUpdated = now;
			Debug.Log("Config updated.");
		}
	}
}