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
    
    [Test]
    public void TaskSequenceWillExecute() {
        var group = new TaskGroup(10);
        
        var task1 = new TaskForSequence();
        var task2 = new TaskForSequence();
        var task3 = new TaskForSequence();
        var task4 = new TaskForSequence();
        
        var seq = new TaskSequence(new Task[] {
            task1,
            task2,
            task3,
            task4
        });
        
        var seqIndex = group.NewTask(seq);
        
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
        
        var seq = new ParallelTaskGroup(new Task[] {
            task1,
            task2,
            task3,
            task4
        });
        
        var seqIndex = group.NewTask(seq);
        
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
        
        var seq = new TaskSequence(
                        new ParallelTaskGroup(
                            task1,
                            task2,
                            task3),
                        new ParallelTaskGroup(
                            task4,
                            task5));
                            
        var seqIndex = group.NewTask(seq);
        
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

public class SampleTask : Task {
    public bool Started       = false;
    public bool Stopped       = false;
    public int ExecutionCount = 0;
    public int MaxExecutions  = 1;
    
    public SampleTask(int maxExecutions) {
        MaxExecutions = maxExecutions;
        Started       = true;
    }
    
    public override bool Update() {
        ExecutionCount++;
        
        if(ExecutionCount >= MaxExecutions) {
            Stopped = true;
            return true;
        }
        
        return false;
    }
}

public class WrappedTask : Task {
    public bool Stopped = false;
    public WrappedTask TaskToStop;
    
    public WrappedTask(WrappedTask taskToStop){
        TaskToStop = taskToStop;
    }
    
    public WrappedTask() {
        TaskToStop = null;
    }
    
    public override bool Update() {
        Stopped = true;
        
        if(TaskToStop != null) {
            TaskToStop.Update();
        }
        return true;
    }
}

public class TaskForSequence : Task {
    public bool Executed = false;
    
    public override bool Update() {
        Executed = true;
        return true;
    }
    
    public override void Reset() {
        Executed = false;
    }
}