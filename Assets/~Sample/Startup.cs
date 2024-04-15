using UnityEngine;

public class Startup : MonoBehaviour{
    public Character     PlayerPrefab;
    public PlayerInput   PlayerInput;
    public EntityManager Em;

    private void Awake(){
        Singleton<EntityManager>.Create(Em);
    }    
    
    private void Update(){
        PlayerInput.Execute();
        Em.Execute();
    }
}
