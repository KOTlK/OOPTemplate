using System;

public class TaskGroup{
    private Task[] _allTasks;
    private int[]  _removedTasks;
    private int    _tasksCount;
    private int    _removedTasksCount;
    
    public TaskGroup(int startCount){
        _allTasks          = new Task[startCount];
        _removedTasks      = new int[startCount];
        _tasksCount        = 0;
        _removedTasksCount = 0;
    }
    
    public void NewTask(Task task){
        var index = _tasksCount++;
        
        if(_tasksCount == _allTasks.Length){
            Array.Resize(ref _allTasks, _tasksCount << 1);
        }
        
        _allTasks[index] = task;
        task.Index               = index;
        task.Over += EndTask;
        task.OnCreate();
    }
    
    public void EndTask(int index){
        var task = _allTasks[index];
        
        task.Over -= EndTask;

        //remove and swapback
        _allTasks[index] = _allTasks[--_tasksCount];
        _allTasks[index].Index = index;
        _allTasks[_tasksCount] = null;
        
        if(_tasksCount > 0){ 
            if(_removedTasksCount == _removedTasks.Length){
                Array.Resize(ref _removedTasks, _removedTasksCount << 1);
            }
            
            _removedTasks[_removedTasksCount++] = index;
        }
        
        task.OnOver();
    }
    
    public void RunTasks(){
        for(var i = 0; i < _tasksCount; ++i){
            _allTasks[i].Run();
        }
        
        //iterate through all tasks that were swapped back and execute it
        while(_removedTasksCount > 0){
            _allTasks[--_removedTasksCount].Run();
        }
    }
}