public static class Singleton<T>{
    public static T    Instance { get; private set; }
    public static bool Exist { get; private set; }
    
    public static void Create(T instance){
        Instance = instance;
        Exist    = true;
    }
    
    public static void CreateIfNotExist(T instance){
        if(!Exist){
            Instance = instance;
            Exist    = true;
        }
    }
    
    public static void Remove(){
        if(Exist){
            Instance = default(T);
            Exist    = false;
        }
    }
}
