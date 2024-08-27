using System;
using System.Runtime.CompilerServices;
using static Assertions;

public unsafe class TaskGroup {
    public Task[] AllTasks;
    public int[]  FreeTasks;
    public int[]  RemovedTasks;
    public int    RemovedTasksCount;
    public int    TasksCount; //not active tasks
    public int    FreeTasksCount;
    
    public TaskGroup(int startCount) {
        AllTasks          = new Task[startCount];
        FreeTasks         = new int[startCount];
        RemovedTasks      = new int[startCount];
        TasksCount        = 0;
        FreeTasksCount    = 0;
        RemovedTasksCount = 0;
    }
    
    public int NewTask(UpdateFunction update) {
        var index = -1;
        
        if(FreeTasksCount > 0) {
            index = FreeTasks[--FreeTasksCount];
        } else {
            index = TasksCount++;
        }
        
        if(TasksCount == AllTasks.Length) {
            Array.Resize(ref AllTasks, TasksCount << 1);
        }
        
        AllTasks[index].Update   = update;
        AllTasks[index].IsOver = false;
        
        return index;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndTask(int i) {
        Assert(AllTasks[i].Update != null);
        
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
            AllTasks[RemovedTasks[i]].Update = null;
        }
        RemovedTasksCount = 0;
        
        for(var i = 0; i < TasksCount; ++i) {
            if(!AllTasks[i].IsOver) {
                AllTasks[i].IsOver = AllTasks[i].Update();
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