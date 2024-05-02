using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;
using System.Collections;

public class TasksTest
{
    [Test]
    public void StartAndOverTasksInCorrectOrder() {
        var tasksCount         = 100;
        var maxExecutionsCount = 100;
        var tasksDict          = new Dictionary<SampleTask, int>();
        var runner = new TaskRunner();
        
        for(var i = 0; i < tasksCount; ++i) {
            var exCount = Random.Range(0, maxExecutionsCount);
            var task    = new SampleTask(exCount);
            runner.StartTask(TaskGroupType.Gameplay, task);
            tasksDict.Add(task, exCount);
            Assert.True(task.Started);
        }
        
        for(var i = 0; i < maxExecutionsCount; ++i) {
            runner.RunTaskGroup(TaskGroupType.Gameplay);
            
            foreach(var (task, exCount) in tasksDict) {
                if(exCount == i) {
                    Assert.True(task.Stopped);
                }
            }
        }
    }
    
    [Test]
    public void WrappedTasksWillBeStopped() {
        var runner = new TaskRunner();
        
        var task1 = new WrappedTask();
        var task2 = new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask(new WrappedTask()))))))))))))))))))))))))))))))))))))))))));
        task1.TaskToStop = task2;
        
        runner.StartTask(TaskGroupType.Gameplay, task2);
        runner.StartTask(TaskGroupType.Gameplay, task1);
        
        runner.RunTaskGroup(TaskGroupType.Gameplay);
        
        Assert.True(task1.Stopped);
        Assert.True(task2.Stopped);
    }
}

public class SampleTask : IEnumerator {
    public bool Started       = false;
    public bool Stopped       = false;
    public int ExecutionCount = 0;
    public int MaxExecutions  = 1;
    
    public SampleTask(int maxExecutions) {
        MaxExecutions = maxExecutions;
        Started       = true;
    }
    
    public object Current => null;
    
    public void Reset() {}
    
    public bool MoveNext() {
        ExecutionCount++;
        
        if(ExecutionCount >= MaxExecutions) {
            Stopped = true;
            return false;
        }
        
        return true;
    }
}

public class WrappedTask : IEnumerator {
    public bool Stopped = false;
    public WrappedTask TaskToStop;
    
    public WrappedTask(WrappedTask taskToStop){
        TaskToStop = taskToStop;
    }
    
    public WrappedTask() {
        TaskToStop = null;
    }
    
    public object Current => null;
    
    public void Reset() {}
    
    public bool MoveNext() {
        Stopped = true;
        
        if(TaskToStop != null) {
            TaskToStop.MoveNext();
        }
        return false;
    }
}
