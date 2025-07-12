using UnityEngine;

public class EventsTest : MonoBehaviour {
    public struct TestEvent {
        public float Data;
    }

    public class TestEvent2 {
        public int a;
    }

    public class TestEvent3 : TestEvent2 {
        public int b;
    }

    private const string PrivateQueue = "Private Queue";

    private void Awake() {
        Events.Init();
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.Q)) {
            var evnt  = new TestEvent();
            evnt.Data = Random.Range(-10.2f, 50.3f);
            Events.RaiseGeneral(evnt);

            var evnt2 = new TestEvent2();
            evnt2.a = 100;
            Events.RaiseGeneral(evnt2);

            var evnt3 = new TestEvent3();
            evnt3.a = 111;
            evnt3.b = 222;
            Events.RaiseGeneral(evnt3);
        }

        if(Input.GetKeyDown(KeyCode.W)) {
            var evnt  = new TestEvent();
            evnt.Data = 20.8f;
            Events.RaisePrivate(PrivateQueue, evnt);
        }

        if(Input.GetKeyDown(KeyCode.E)) {
            Events.SubGeneral<TestEvent>(EatEvent);
            Events.SubGeneral<TestEvent2>(EatEvent2);
            Events.SubGeneral<TestEvent3>(EatEvent3);
        }

        if(Input.GetKeyDown(KeyCode.R)) {
            Events.UnsubGeneral<TestEvent>(EatEvent);
            Events.UnsubGeneral<TestEvent2>(EatEvent2);
            Events.UnsubGeneral<TestEvent3>(EatEvent3);
        }

        if(Input.GetKeyDown(KeyCode.T)) {
            Events.SubPrivate<TestEvent>(PrivateQueue, EatEvent);
        }

        if(Input.GetKeyDown(KeyCode.Y)) {
            Events.UnsubPrivate<TestEvent>(PrivateQueue, EatEvent);
        }
    }

    public void EatEvent(TestEvent evnt) {
        Debug.Log(evnt.Data);
    }

    public void EatEvent2(TestEvent2 evnt) {
        Debug.Log(evnt.a);
    }

    public void EatEvent3(TestEvent3 evnt) {
        Debug.Log(evnt.a);
        Debug.Log(evnt.b);
    }
}