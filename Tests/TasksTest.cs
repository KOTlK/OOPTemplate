using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using static TasksUtils;

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
            runner.StartTask(TaskGroupType.Gameplay, task.Update);
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
        
        runner.StartTask(TaskGroupType.Gameplay, task2.Update);
        runner.StartTask(TaskGroupType.Gameplay, task1.Update);
        
        runner.RunTaskGroup(TaskGroupType.Gameplay);
        
        Assert.True(task1.Stopped);
        Assert.True(task2.Stopped);
    }
    
    [Test]
    public void TaskSequenceWillExecute() {
        var group = new TaskGroup(10);
        
        var task1 = new TaskForSequence();
        var task2 = new TaskForSequence();
        var task3 = new TaskForSequence();
        var task4 = new TaskForSequence();
        
        var seqIndex = group.NewTask(TaskSequence(task1.Update,
                                              task2.Update, 
                                              task3.Update, 
                                              task4.Update));
        
        Assert.True(task1.Executed == false);
        Assert.True(task2.Executed == false);
        Assert.True(task3.Executed == false);
        Assert.True(task4.Executed == false);
        
        group.RunTasks();
        
        Assert.True(task1.Executed == true);
        Assert.True(task2.Executed == false);
        Assert.True(task3.Executed == false);
        Assert.True(task4.Executed == false);
        
        group.RunTasks();
        
        Assert.True(task1.Executed == true);
        Assert.True(task2.Executed == true);
        Assert.True(task3.Executed == false);
        Assert.True(task4.Executed == false);
        
        group.RunTasks();
        
        Assert.True(task1.Executed == true);
        Assert.True(task2.Executed == true);
        Assert.True(task3.Executed == true);
        Assert.True(task4.Executed == false);
        
        group.RunTasks();
        
        Assert.True(task1.Executed == true);
        Assert.True(task2.Executed == true);
        Assert.True(task3.Executed == true);
        Assert.True(task4.Executed == true);
        
        Assert.True(group.TaskOver(seqIndex));
    }
    
    [Test]
    public void ParallelTasksWillExecute() {
        var group = new TaskGroup(10);
        
        var task1 = new SampleTask(1);
        var task2 = new SampleTask(3);
        var task3 = new SampleTask(2);
        var task4 = new SampleTask(4);
        
        var seqIndex = group.NewTask(ParallelTasks(task1.Update,
                                              task2.Update,
                                              task3.Update,
                                              task4.Update));
        
        Assert.True(task1.Stopped == false);
        Assert.True(task2.Stopped == false);
        Assert.True(task3.Stopped == false);
        Assert.True(task4.Stopped == false);
        
        group.RunTasks();
        
        Assert.True(task1.Stopped == true);
        Assert.True(task2.Stopped == false);
        Assert.True(task3.Stopped == false);
        Assert.True(task4.Stopped == false);
        
        group.RunTasks();
        
        Assert.True(task1.Stopped == true);
        Assert.True(task2.Stopped == false);
        Assert.True(task3.Stopped == true);
        Assert.True(task4.Stopped == false);
        
        group.RunTasks();
        
        Assert.True(task1.Stopped == true);
        Assert.True(task2.Stopped == true);
        Assert.True(task3.Stopped == true);
        Assert.True(task4.Stopped == false);
        
        group.RunTasks();
        
        Assert.True(task1.Stopped == true);
        Assert.True(task2.Stopped == true);
        Assert.True(task3.Stopped == true);
        Assert.True(task4.Stopped == true);
        
        Assert.True(group.TaskOver(seqIndex));
    }
    
    [Test]
    public void SequenceOfParallelTasksWillExecute() {
        var group = new TaskGroup(10);
        
        var task1 = new TaskForSequence();
        var task2 = new TaskForSequence();
        var task3 = new TaskForSequence();
        var task4 = new TaskForSequence();
        var task5 = new TaskForSequence();
                            
        var seqIndex = group.NewTask(TaskSequence(ParallelTasks(task1.Update,
                                                                task2.Update,
                                                                task3.Update),
                                                  ParallelTasks(task4.Update, 
                                                                task5.Update)));
        
        group.RunTasks();
                            
        Assert.True(task1.Executed == true);
        Assert.True(task2.Executed == true);
        Assert.True(task3.Executed == true);
        Assert.True(task4.Executed == false);
        Assert.True(task5.Executed == false);
        Assert.True(group.TaskOver(seqIndex) == false);
        
        group.RunTasks();
                            
        Assert.True(task1.Executed == true);
        Assert.True(task2.Executed == true);
        Assert.True(task3.Executed == true);
        Assert.True(task4.Executed == true);
        Assert.True(task5.Executed == true);
        Assert.True(group.TaskOver(seqIndex) == true);
    }
}

public class SampleTask {
    public bool Started       = false;
    public bool Stopped       = false;
    public int ExecutionCount = 0;
    public int MaxExecutions  = 1;
    
    public SampleTask(int maxExecutions) {
        MaxExecutions = maxExecutions;
        Started       = true;
    }
    
    public bool Update() {
        ExecutionCount++;
        
        if(ExecutionCount >= MaxExecutions) {
            Stopped = true;
            return true;
        }
        
        return false;
    }
}

public class WrappedTask {
    public bool Stopped = false;
    public WrappedTask TaskToStop;
    
    public WrappedTask(WrappedTask taskToStop){
        TaskToStop = taskToStop;
    }
    
    public WrappedTask() {
        TaskToStop = null;
    }
    
    public bool Update() {
        Stopped = true;
        
        if(TaskToStop != null) {
            TaskToStop.Update();
        }
        return true;
    }
}

public class TaskForSequence {
    public bool Executed = false;
    
    public bool Update() {
        Executed = true;
        return true;
    }
}