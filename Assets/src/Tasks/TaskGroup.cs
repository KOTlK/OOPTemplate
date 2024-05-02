using System;
using System.Collections;
using System.Runtime.CompilerServices;
using static UnityEngine.Assertions.Assert;

public class TaskGroup {
    public Task[] AllTasks;
    public int[]  FreeTasks;
    public int    TasksCount; //not active tasks
    public int    FreeTasksCount;
    
    public TaskGroup(int startCount) {
        AllTasks       = new Task[startCount];
        FreeTasks      = new int[startCount];
        TasksCount     = 0;
        FreeTasksCount = 0;
    }
    
    public int NewTask(IEnumerator task) {
        var index = -1;
        
        if(FreeTasksCount > 0) {
            index = FreeTasks[--FreeTasksCount];
        } else {
            index = TasksCount++;
        }
        
        if(TasksCount == AllTasks.Length) {
            Array.Resize(ref AllTasks, TasksCount << 1);
        }
        
        AllTasks[index].Iterator = task;
        AllTasks[index].Index    = index;
        AllTasks[index].IsOver   = false;
        
        return index;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndTask(int i) {
        IsTrue(AllTasks[i].Iterator != null);
        
        if(FreeTasksCount == FreeTasks.Length) {
            Array.Resize(ref FreeTasks, FreeTasksCount << 1);
        }
        
        FreeTasks[FreeTasksCount++] = i;
        AllTasks[i].Iterator = null;
        AllTasks[i].IsOver   = true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunTasks() {
        for(var i = 0; i < TasksCount; ++i) {
            if(!AllTasks[i].IsOver) {
                if(AllTasks[i].Iterator.MoveNext() == false) {
                    EndTask(i);
                    i--;
                }
            }
        }
    }
}