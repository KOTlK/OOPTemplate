using System;
using System.Runtime.CompilerServices;
using static UnityEngine.Assertions.Assert;

public class TaskGroup{
    private Task[] _allTasks;
    private int    _tasksCount;
    
    public TaskGroup(int startCount){
        _allTasks          = new Task[startCount];
        _tasksCount        = 0;
    }
    
    public void NewTask(Task task){
        var index = _tasksCount++;
        
        if(_tasksCount == _allTasks.Length){
            Array.Resize(ref _allTasks, _tasksCount << 1);
        }
        
        _allTasks[index] = task;
        task.Index       = index;
        task.IsOver      = false;
        task.OnCreate();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndTask(int index){
        IsTrue(_allTasks[index] != null);
        _allTasks[index].Stop();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RunTasks(){
        for(var i = 0; i < _tasksCount; ++i){
            IsTrue(_allTasks[i] != null);

            if(_allTasks[i].IsOver){
                RemoveAndSwapBack(i);
                i--;
            }else{
                _allTasks[i].Run();
                if(_allTasks[i].IsOver){
                    RemoveAndSwapBack(i);
                    i--;
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveAndSwapBack(int i){
        IsTrue(_allTasks[_tasksCount - 1] != null);
        var task = _allTasks[i];
        _allTasks[i] = _allTasks[--_tasksCount];
        _allTasks[i].Index = i;
        _allTasks[_tasksCount] = null;
        task.OnOver();
    }
}