using static Assertions;

public delegate bool UpdateFunction();

public struct Task {
    public UpdateFunction Update;
    public bool IsOver;
}

public static class TasksUtils {
    public static UpdateFunction TaskSequence(params UpdateFunction[] funcs) {
        var seq = new TaskSequence(funcs);
        return seq.Update;
    }

    public static UpdateFunction ParallelTasks(params UpdateFunction[] funcs) {
        var parr = new ParallelTaskGroup(funcs);
        return parr.Update;
    }
}

public struct TaskSequence {
    public UpdateFunction[] Tasks;
    
    private int _cursor;
    
    public TaskSequence(params UpdateFunction[] tasks) {
        Assert(tasks != null);
        Assert(tasks.Length > 0);
        Tasks   = tasks;
        _cursor = 0;
    }
    
    public bool Update() {
        if(Tasks[_cursor].Invoke()) {
            _cursor++;
            
            if(_cursor == Tasks.Length) {
                return true;
            }
        }
        
        return false;
    }
}

public struct ParallelTaskGroup {
    public UpdateFunction[] Tasks;
    public bool[]           TasksOver;
    
    public ParallelTaskGroup(params UpdateFunction[] tasks) {
        Assert(tasks != null);
        Assert(tasks.Length > 0);
        Tasks = tasks;
        TasksOver = new bool[tasks.Length];
    }
    
    public bool Update() {
        var tasksOverCount = 0;
        for(var i = 0; i < Tasks.Length; ++i) {
            if(TasksOver[i] == false) {
                TasksOver[i] = Tasks[i].Invoke();
            }

            if(TasksOver[i] == true) {
                tasksOverCount++;
            }
        }
        
        if(tasksOverCount == Tasks.Length) {
            return true;
        } else {
            return false;
        }
    }
}