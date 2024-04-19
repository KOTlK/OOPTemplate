public abstract class Task{
    public delegate void TaskOver(int index);
    public TaskOver Over = delegate { };
    
    public int           Index;
    public TaskGroupType Group;
    
    public abstract void Run();
    public virtual void OnCreate(){}
    public virtual void OnOver(){}
}