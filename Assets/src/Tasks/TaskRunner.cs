using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static Assertions;

public enum TaskGroupType {
    Gameplay,
    GUI,
    ExecuteAlways
}

public class TaskRunner {
    private Dictionary<TaskGroupType, TaskGroup> _groups = new();
    
    public TaskRunner() {
        var types = Enum.GetValues(typeof(TaskGroupType));
        
        foreach(var type in types) {
            _groups.Add((TaskGroupType)type, new TaskGroup(30));
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StartTask(TaskGroupType group, Task task) {
        Assert(task != null);
        Assert(_groups.ContainsKey(group));
        _groups[group].NewTask(task);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndTask(TaskGroupType group, int index) {
        Assert(_groups.ContainsKey(group));
        _groups[group].EndTask(index);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunTaskGroup(TaskGroupType group) {
        Assert(_groups.ContainsKey(group));
        _groups[group].RunTasks();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TaskGroup GetGroup(TaskGroupType type) {
        return _groups[type];
    }
}