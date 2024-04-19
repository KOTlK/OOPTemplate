using UnityEngine;

[System.Serializable]
public class EnemySpawner : Task{
    public Vector3 MaxBounds;
    public Vector3 MinBounds;
    public Vector3 WorldCenter = Vector3.zero;
    public Enemy   Prefab;
    public float   SpawnRate; // spawns per minute
    
    private float _delay;
    
    public override void OnCreate(){
        _delay = 60f / SpawnRate;
    }

    public override void Run(){
        var em = Singleton<EntityManager>.Instance;
        _delay += Time.deltaTime;
        
        if(_delay >= 60f / SpawnRate){
            em.CreateEntity(Prefab, GetRandomPosition());
            _delay = 0f;
        }
    }
    
    private Vector3 GetRandomPosition(){
        var x = Random.Range(MinBounds.x, MaxBounds.x);
        var z = Random.Range(MinBounds.z, MaxBounds.z);
        
        return new Vector3(x, 0, z);
    }
}
