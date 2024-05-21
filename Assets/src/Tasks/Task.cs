using System.Collections;
using System.Collections.Generic;
using static Assertions;

public struct TaskPtr {
    public Task Task;
    public bool IsOver;
}

public abstract class Task {
    public abstract bool Update(); // Returns true if task is over
    public virtual void  Reset() {
    }
}

public class TaskSequence : Task {
    public Task[] Tasks;
    
    private int _cursor;
    
    public TaskSequence(params Task[] tasks) {
        Assert(tasks != null);
        Assert(tasks.Length > 0);
        Tasks   = tasks;
        _cursor = 0;
    }
    
    public override bool Update() {
        if(Tasks[_cursor].Update()) {
            _cursor++;
            
            if(_cursor == Tasks.Length) {
                return true;
            }
        }
        
        return false;
    }
    
    public override void Reset() {
        foreach(var task in Tasks) {
            task.Reset();
        }
        
        _cursor = 0;
    }
}

public class ParallelTaskGroup : Task {
    public Task[]    Tasks;
    public List<int> TasksOver;
    
    public ParallelTaskGroup(params Task[] tasks) {
        Assert(tasks != null);
        Assert(tasks.Length > 0);
        Tasks = tasks;
        TasksOver = new List<int>(tasks.Length);
    }
    
    public override bool Update() {
        for(var i = 0; i < Tasks.Length; ++i) {
            if(TasksOver.Contains(i) == false) {
                if(Tasks[i].Update()) {
                    TasksOver.Add(i);
                }
            }
        }
        
        if(TasksOver.Count == Tasks.Length) {
            return true;
        } else {
            return false;
        }
    }
    
    public override void Reset() {
        foreach(var task in Tasks) {
            task.Reset();
        }
        TasksOver.Clear();
    }
}