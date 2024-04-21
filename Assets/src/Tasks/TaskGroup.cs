using System;
using System.Runtime.CompilerServices;
using static UnityEngine.Assertions.Assert;

public class TaskGroup{
    public Task[] AllTasks;
    public int    TasksCount;
    
    public TaskGroup(int startCount){
        AllTasks          = new Task[startCount];
        TasksCount        = 0;
    }
    
    public void NewTask(Task task){
        var index = TasksCount++;
        
        if(TasksCount == AllTasks.Length){
            Array.Resize(ref AllTasks, TasksCount << 1);
        }
        
        AllTasks[index] = task;
        task.Index       = index;
        task.IsOver      = false;
        task.OnCreate();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndTask(int index){
        IsTrue(AllTasks[index] != null);
        AllTasks[index].Stop();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunTasks(){
        for(var i = 0; i < TasksCount; ++i){
            IsTrue(AllTasks[i] != null);

            if(AllTasks[i].IsOver){
                RemoveAndSwapBack(i);
                i--;
            }else{
                AllTasks[i].Run();
                if(AllTasks[i].IsOver){
                    RemoveAndSwapBack(i);
                    i--;
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveAndSwapBack(int i){
        IsTrue(AllTasks[TasksCount - 1] != null);
        var task = AllTasks[i];
        AllTasks[i] = AllTasks[--TasksCount];
        AllTasks[i].Index = i;
        AllTasks[TasksCount] = null;
        task.OnOver();
    }
}