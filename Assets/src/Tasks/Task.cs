public abstract class Task{
    public bool          IsOver;
    public int           Index;
    public TaskGroupType Group;
    
    public abstract void Run();
    public virtual void OnCreate(){}
    public virtual void OnOver(){}
    
    public void Stop(){
        IsOver = true;
    }
}