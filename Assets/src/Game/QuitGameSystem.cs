using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using Reflex.Attributes;

public sealed class QuitGameSystem : GameSystem {
	[Inject] Ecs _ecs;
	[Inject] EntityManager _em;

	List<EntityHandle> _entities = new();

	public QuitGameSystem(Game game) : base (game, true) {
	}

	public override void Update() {
		if (Keyboard.current.escapeKey.wasPressedThisFrame) {
			Game.Quit();
		}

		if (Keyboard.current.spaceKey.isPressed) {
			var entity = _ecs.CreateEntity();

			_ecs.AddComponent(entity, new TestComponent() {
				Position = Vector2.zero,
				Size = new Vector2(1, 1)
			});

			_ecs.AddComponent(entity, new TestComponent2(){
				c = -10,
				b = 23
			});

			_entities.Add(entity);
		}

		if (Keyboard.current.qKey.isPressed) {
			if (_entities.Count > 0) {
				var rand = Random.Range(0, _entities.Count);
				var entity = _entities[rand];

				_ecs.DestroyEntity(entity);
				_entities.RemoveAt(rand);
			}
		}

		if (Keyboard.current.eKey.isPressed) {
			if (_entities.Count > 0) {
				var rand = Random.Range(0, _entities.Count);
				var entity = _entities[rand];

				if (_ecs.HasComponent<TestComponent2>(entity)) {
					_ecs.RemoveComponent<TestComponent2>(entity);
				}
			}
		}

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

		var move = new Vector2(right, up);

		_ecs.ForEach((ref TestComponent t) => {
			t.Position += move * Clock.Delta;
		});

		_ecs.ForEach((ref TestComponent t, ref TestComponent2 t2) => {
			var extent = t.Size * 0.5f;

			var lb = new Vector3(t.Position.x - extent.x, t.Position.y - extent.y, 0);
			var rb = new Vector3(t.Position.x + extent.x, t.Position.y - extent.y, 0);
			var rt = new Vector3(t.Position.x + extent.x, t.Position.y + extent.y, 0);
			var lt = new Vector3(t.Position.x - extent.x, t.Position.y + extent.y, 0);

			Debug.DrawLine(lb, rb, Color.blue);
			Debug.DrawLine(rb, rt, Color.blue);
			Debug.DrawLine(rt, lt, Color.blue);
			Debug.DrawLine(lt, lb, Color.blue);
		});
	}
}