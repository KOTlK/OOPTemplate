using UnityEngine;
using UnityEngine.InputSystem;

public class TestEntity : Entity {
	public override void UpdateEntity() {
		var right = 0f;
		var up    = 0f;

		var kb = Keyboard.current;

		if (kb.aKey.isPressed) {
			right -= 1.0f;
		} 
		if (kb.dKey.isPressed) {
			right += 1.0f;
		}
		if (kb.wKey.isPressed) {
			up += 1.0f;
		}
		if (kb.sKey.isPressed) {
			up -= 1.0f;
		}

		var move = new Vector3(right, up, 0f);

		Position += move * Clock.Delta;

		if (kb.bKey.wasPressedThisFrame) {
			DestroyThisEntity();
		}
	}
}