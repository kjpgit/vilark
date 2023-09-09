using System.Runtime.InteropServices;
namespace vilark;

/*
 * The purpose of this is to gate access to the tty, specifically, to stop
 * Console.ReadKey() from running while we are attempting to shut down.
 *
 * It turns a synchronous/blocking API (ReadKey) into an async API, where we can
 * use WaitAny() to wait on a single keypress, as well as any other event source.
 *
 * It also helps with signals, because we can delay the signal handler thread returning
 * until we are done processing it.
 *
 */

class InputEvent<T>
{
    private EventWaitHandle consumerGo = new EventWaitHandle(initialState:false, EventResetMode.AutoReset);
    private EventWaitHandle producerGo = new EventWaitHandle(initialState:true, EventResetMode.AutoReset);
    private T? m_event = default(T);

    // For producers who want to keep replacing the event (like the loading progress),
    // without waiting for consumer to take it.
    private readonly object m_event_lock = new object();

    // Create a new InputEvent
    public InputEvent() { }

    public EventWaitHandle ConsumerWaitHandle => consumerGo;
    public EventWaitHandle ProducerWaitHandle => producerGo;

    // Producer: Add/Replace the event, and block until the consumer says you can go again
    // Call this after waiting on ProducerWaitHandle
    public void AddEventAndWait(T e) {
        lock (m_event_lock) {
            m_event = e;
        }
        WaitHandle.SignalAndWait(consumerGo, producerGo);
    }

    // Producer: Add/Replace the event
    // Call this after waiting on ProducerWaitHandle
    public void AddEvent(T e) {
        lock (m_event_lock) {
            m_event = e;
        }
        consumerGo.Set();
    }

    // Consumer: call this after waiting on ConsumerWaitHandle
    public T TakeEvent() {
        T? ret = default(T);
        lock (m_event_lock) {
            ret = m_event ?? throw new Exception("null event");
            m_event = default(T);
        }
        return ret;
    }


}

