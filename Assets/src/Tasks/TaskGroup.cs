using System;
using System.Collections;
using System.Runtime.CompilerServices;
using static UnityEngine.Assertions.Assert;

public class TaskGroup {
    public Task[] AllTasks;
    public int    TasksCount;
    
    public TaskGroup(int startCount) {
        AllTasks   = new Task[startCount];
        TasksCount = 0;
    }
    
    public int NewTask(IEnumerator task) {
        var index = TasksCount++;
        
        if(TasksCount == AllTasks.Length) {
            Array.Resize(ref AllTasks, TasksCount << 1);
        }
        
        AllTasks[index].Iterator = task;
        AllTasks[index].Index    = index;
        
        return index;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndTask(int i) {
        IsTrue(AllTasks[i].Iterator != null);
        AllTasks[i] = AllTasks[--TasksCount];
        AllTasks[i].Index = i;
        AllTasks[TasksCount] = default;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunTasks() {
        for(var i = 0; i < TasksCount; ++i) {
            IsTrue(AllTasks[i].Iterator != null);

            if(AllTasks[i].Iterator.MoveNext() == false) {
                EndTask(i);
                i--;
            }
        }
    }
}