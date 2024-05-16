using System;
using System.Collections;
using System.Runtime.CompilerServices;
using static UnityEngine.Assertions.Assert;

public class TaskGroup {
    public TaskPtr[] AllTasks;
    public int[]     FreeTasks;
    public int[]     RemovedTasks;
    public int       RemovedTasksCount;
    public int       TasksCount; //not active tasks
    public int       FreeTasksCount;
    
    public TaskGroup(int startCount) {
        AllTasks          = new TaskPtr[startCount];
        FreeTasks         = new int[startCount];
        RemovedTasks      = new int[startCount];
        TasksCount        = 0;
        FreeTasksCount    = 0;
        RemovedTasksCount = 0;
    }
    
    public int NewTask(Task task) {
        var index = -1;
        
        if(FreeTasksCount > 0) {
            index = FreeTasks[--FreeTasksCount];
        } else {
            index = TasksCount++;
        }
        
        if(TasksCount == AllTasks.Length) {
            Array.Resize(ref AllTasks, TasksCount << 1);
        }
        
        // AllTasks[index].Iterator = task;
        // AllTasks[index].Index    = index;
        AllTasks[index].Task   = task;
        AllTasks[index].IsOver = false;
        
        return index;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndTask(int i) {
        IsTrue(AllTasks[i].Task != null);
        
        if(RemovedTasksCount == RemovedTasks.Length) {
            Array.Resize(ref RemovedTasks, RemovedTasksCount << 1);
        }
        
        RemovedTasks[RemovedTasksCount++] = i;
        AllTasks[i].IsOver = true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunTasks() {
        for(var i = 0; i < RemovedTasksCount; ++i) {
            if(FreeTasksCount >= FreeTasks.Length) {
                Array.Resize(ref FreeTasks, FreeTasksCount << 1);
            }
            
            FreeTasks[FreeTasksCount++] = RemovedTasks[i];
            AllTasks[RemovedTasks[i]].Task = null;
        }
        RemovedTasksCount = 0;
        
        for(var i = 0; i < TasksCount; ++i) {
            if(!AllTasks[i].IsOver) {
                AllTasks[i].IsOver = AllTasks[i].Task.Update();
                if(AllTasks[i].IsOver) {
                    EndTask(i);
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TaskOver(int index) {
        return AllTasks[index].IsOver;
    }
}