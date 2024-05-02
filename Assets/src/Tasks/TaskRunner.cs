using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using static UnityEngine.Assertions.Assert;

public enum TaskGroupType {
    Gameplay,
    GUI,
    ExecuteAlways
}

public class TaskRunner {
    private Dictionary<TaskGroupType, TaskGroup> _groups = new();
    
    public int TasksCount => _groups.Sum(group => group.Value.TasksCount);
    
    public TaskRunner(){
        var types = Enum.GetValues(typeof(TaskGroupType));
        
        foreach(var type in types){
            _groups.Add((TaskGroupType)type, new TaskGroup(30));
        }
    }
    
    public void StartTask(TaskGroupType group, IEnumerator task){
        IsTrue(task != null);
        IsTrue(_groups.ContainsKey(group));
        _groups[group].NewTask(task);
    }
    
    public void EndTask(TaskGroupType group, int index){
        IsTrue(_groups.ContainsKey(group));
        _groups[group].EndTask(index);
    }
    
    public void RunTaskGroup(TaskGroupType group){
        IsTrue(_groups.ContainsKey(group));
        _groups[group].RunTasks();
    }
}