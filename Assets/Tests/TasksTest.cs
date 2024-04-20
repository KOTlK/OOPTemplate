using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;

public class TasksTest
{
    [Test]
    public void StartAndOverTasksInCorrectOrder(){
        var tasksCount         = 100;
        var maxExecutionsCount = 100;
        var gameobject         = new GameObject();
        var tasksDict          = new Dictionary<SampleTask, int>();
        gameobject.AddComponent<TaskRunner>();
        var runner = gameobject.GetComponent<TaskRunner>();
        
        for(var i = 0; i < tasksCount; ++i){
            var exCount = Random.Range(0, maxExecutionsCount);
            var task    = new SampleTask(exCount);
            runner.StartTask(TaskGroupType.Gameplay, task);
            tasksDict.Add(task, exCount);
            Assert.True(task.Started);
        }
        
        for(var i = 0; i < maxExecutionsCount; ++i){
            runner.RunTaskGroup(TaskGroupType.Gameplay);
            
            foreach(var (task, exCount) in tasksDict){
                if(exCount == i){
                    Assert.True(task.Stopped);
                }
            }
        }
    }
    
    [Test]
    public void WrappedTasksWillBeStopped(){
        var gameobject = new GameObject();
        gameobject.AddComponent<TaskRunner>();
        var runner = gameobject.GetComponent<TaskRunner>();
        
        var task1 = new WrappedTask();
        var task2 = new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask()))))))))))))))))))))))))))))))))))))))))));
        task1.TaskToStop = task2;
        
        runner.StartTask(TaskGroupType.Gameplay, task2);
        runner.StartTask(TaskGroupType.Gameplay, task1);
        
        runner.RunTaskGroup(TaskGroupType.Gameplay);
        
        Assert.True(task1.IsOver);
        Assert.True(task2.IsOver);
    }
}

public class SampleTask : Task{
    public bool Started       = false;
    public bool Stopped       = false;
    public int ExecutionCount = 0;
    public int MaxExecutions  = 1;
    
    public SampleTask(int maxExecutions){
        MaxExecutions = maxExecutions;
    }
    
    public override void OnCreate(){
        Started = true;
    }
    
    public override void OnOver(){
        Stopped = true;
    }
    
    public override void Run(){
        ExecutionCount++;
        
        if(ExecutionCount >= MaxExecutions){
            Stop();
        }
    }
}

public class WrappedTask : Task{
    public WrappedTask TaskToStop;
    
    public WrappedTask(WrappedTask taskToStop){
        TaskToStop = taskToStop;
    }
    
    public WrappedTask(){
        TaskToStop = null;
    }
    
    public override void Run(){
        TaskToStop.Stop();
        Stop();
    }
}
