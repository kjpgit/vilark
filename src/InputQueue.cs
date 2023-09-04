using System.Runtime.InteropServices;

namespace vilark;

record struct InputEvent(KeyPress? keyPress = null, PosixSignal? signal = null);

class InputQueue
{
    // Don't queue multiple of these, coalesce them
    // This avoids blocking the signal handler
    private bool pending_sigwinch = false;

    // Maximum number of events before blocking AddEvent()
    private const int max_queue_size = 100;

    private Queue<InputEvent> m_queue = new();

    public void AddEvent(InputEvent evt) {
        lock(m_queue) {
            while (m_queue.Count >= max_queue_size) {
                Monitor.Wait(m_queue);
            }
            if (evt.signal == PosixSignal.SIGWINCH) {
                // Coalesce these
                if (!pending_sigwinch) {
                    pending_sigwinch = true;
                    m_queue.Enqueue(evt);
                }
            } else {
                m_queue.Enqueue(evt);
            }

            if (m_queue.Count == 1) {
                // Wake up any blocked readers
                //Log.Info("waking up blocked reader");
                Monitor.PulseAll(m_queue);
            }
        }
    }

    public InputEvent WaitForEvent() {
        while (true) {
            lock(m_queue) {
                while (m_queue.Count == 0) {
                    Monitor.Wait(m_queue);
                }

                InputEvent ret = m_queue.Dequeue();
                if (ret.signal == PosixSignal.SIGWINCH) {
                    pending_sigwinch = false;
                }

                if (m_queue.Count == max_queue_size - 1) {
                    // wake up any blocked enqueue
                    //Log.Info("waking up blocked enqueue");
                    Monitor.PulseAll(m_queue);
                }

                return ret;
            }
        }
    }

}

