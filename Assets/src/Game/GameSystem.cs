public abstract class GameSystem {
	public bool Enabled;

	protected Game Game;

	public GameSystem(Game game, bool enabled = true) {
		Game    = game;
		Enabled = enabled;
		game.AppendSystem(this);
	}
	
	public virtual void Update() {}
	public virtual void Destroy() {}
	public virtual void OnEnable() {}
	public virtual void OnDisable() {}
}