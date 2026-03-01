using UnityEngine.InputSystem;

public sealed class QuitGameSystem : GameSystem {
	public QuitGameSystem(Game game) : base (game, true) {
	}

	public override void Update() {
		if (Keyboard.current.escapeKey.wasPressedThisFrame) {
			Game.Quit();
		}
	}
}