public abstract class GameSystem {
	public bool Enabled;
	// Yes, game system can know about the game. It's not the
	protected Game Game;

	public GameSystem(Game game, bool enabled = true) {
		Game    = game;
		Enabled = true;
		game.AppendSystem(this);
	}
	
	public virtual void Update() {}
	public virtual void Destroy() {}
	public virtual void OnEnable() {}
	public virtual void OnDisable() {}
}