using Reflex.Attributes;

public sealed class EntitiesUpdateSystem : GameSystem {
	[Inject] private EntityManager _entityManager;

	public EntitiesUpdateSystem(Game game) : base(game, true) {
	}

	public override void Update() {
		_entityManager.Update();
	}
}