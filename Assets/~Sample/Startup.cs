using UnityEngine;

public class Startup : MonoBehaviour{
    public Player        Player;
    public PlayerInput   PlayerInput;
    public EntityManager Em;

    private void Awake(){
        Singleton<EntityManager>.Create(Em);
        Singleton<Player>.Create(Player);
    }    
    
    private void Update(){
        PlayerInput.Execute();
        Em.Execute();
    }
}
