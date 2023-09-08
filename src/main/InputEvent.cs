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

    public InputEvent() { }

    public void AddEventAndWait(T e) {
        m_event = e;
        WaitHandle.SignalAndWait(consumerGo, producerGo);
    }

    public void AddEvent(T e) {
        m_event = e;
        consumerGo.Set();
    }

    public T TakeEvent() {
        if (m_event == null) {
            throw new Exception("null");
        }
        return m_event;
    }

    public EventWaitHandle ConsumerWaitHandle => consumerGo;
    public EventWaitHandle ProducerWaitHandle => producerGo;

}

