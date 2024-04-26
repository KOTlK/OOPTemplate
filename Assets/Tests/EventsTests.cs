using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

public class EventsTests{
    [Test]
    public void CreateExecuteEventAndClear(){
        var events  = new Events();
        var handler = new SampleEventHandler();
        events.AddHandler<SampleEvent>(handler.HandleEvents);
        
        events.RaiseEvent<SampleEvent>(new SampleEvent{SomeData = 10});
        
        events.HandleEvents<SampleEvent>();
        
        Assert.True(events.AllEvents[typeof(SampleEvent)].Count == 0);
        Assert.True(handler.EventsCount == 1);
        Assert.True(handler.TotalDataSum == 10);
        
        events.Dispose();
    }
    
    [Test]
    public void ResizeAndSaveData(){
        var events  = new Events(2);
        
        events.RaiseEvent<SampleEvent>(new SampleEvent{SomeData = 10});
        events.RaiseEvent<SampleEvent>(new SampleEvent{SomeData = 11});
        events.RaiseEvent<SampleEvent>(new SampleEvent{SomeData = 12});
        
        Assert.True(events.AllEvents[typeof(SampleEvent)].Get<SampleEvent>(0).SomeData == 10);
        Assert.True(events.AllEvents[typeof(SampleEvent)].Get<SampleEvent>(1).SomeData == 11);
        Assert.True(events.AllEvents[typeof(SampleEvent)].Get<SampleEvent>(2).SomeData == 12);
        
        
        events.Dispose();
    }
    
    [Test]
    public void PerformaceTest(){
        var sw      = new Stopwatch();
        sw.Start();
        var events  = new Events();
        var handler = new SampleEventHandler();
        events.AddHandler<SampleEvent>(handler.HandleEvents);
        sw.Stop();
        
        Debug.Log($"Initialization: {sw.ElapsedTicks.ToString()} ticks");
        
        sw.Restart();
        
        const int eventsCount = 10000;
        var someEvent = new SampleEvent{SomeData = 10};
        for(var i = 0; i < eventsCount; ++i){
            events.RaiseEvent<SampleEvent>(someEvent);
        }
        sw.Stop();
        
        Debug.Log($"Raising {eventsCount} identical Events: {sw.ElapsedTicks.ToString()} ticks");
        
        sw.Restart();
        
        events.HandleEvents<SampleEvent>();
        sw.Stop();
        
        Debug.Log($"Handling {eventsCount} identical Events: {sw.ElapsedTicks.ToString()} ticks");
        
        Debug.Log($"{handler.EventsCount}, {handler.TotalDataSum}");
        
        events.Dispose();
    }
}

public struct SampleEvent{
    public int SomeData;
}

public class SampleEventHandler{
    public int EventsCount;
    public int TotalDataSum;
    
    public void HandleEvents(EventData events){
        EventsCount = events.Count;
        for(var i = 0; i < events.Count; ++i){
            var evnt = events.Get<SampleEvent>(i);
            TotalDataSum += evnt.SomeData;
        }
    }
}