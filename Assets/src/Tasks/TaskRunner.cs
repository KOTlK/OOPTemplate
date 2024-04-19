using System;
using System.Collections.Generic;

public enum TaskGroupType{
    Gameplay,
    GUI,
    ExecuteAlways
}

public class TaskRunner : Entity{
    private Dictionary<TaskGroupType, TaskGroup> _groups = new();
    
    public void StartTask(TaskGroupType group, Task task){
        task.Group = group;
        
        if(_groups.ContainsKey(group)){
            _groups[group].NewTask(task);
        }else{
            _groups.Add(group, new TaskGroup(30));
            _groups[group].NewTask(task);
        }
    }
    
    public void EndTask(TaskGroupType group, int index){
        _groups[group].EndTask(index);
    }
    
    public void RunTaskGroup(TaskGroupType group){
        _groups[group].RunTasks();
    }
    
    public override void Execute(){
        RunTaskGroup(TaskGroupType.ExecuteAlways);
    }
}