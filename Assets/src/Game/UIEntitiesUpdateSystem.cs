using Reflex.Attributes;

public sealed class UIEntitiesUpdateSystem : GameSystem {
	[Inject] private UIEntityManager _entityManager;

	public UIEntitiesUpdateSystem(Game game) : base(game, true) {
	}

	public override void Update() {
		_entityManager.Update();
	}
}