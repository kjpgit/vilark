// Copyright (C) 2023 Karl Pickett / Vilark Project
using System.Runtime.InteropServices;
namespace vilark;

// Use cases:
// Single keyboard thread -> Add(keypress)
// Single directory scanner thread -> Add(progress)
// Multiple signal threads -> Add(signal)
// Note: Signal threads send an individual waithandle as payload to wait for an ack.


/*
 * Combines Queue + A wait handle
 *
 * Multiple producers are allowed, but only one consumer should be reading it,
 * and using the wait handle.
 *
 * This is to support a consumer thread (main event loop) using WaitAny().
 *
 * This code is so freaking simple, it might actually be correct.
 *
 */
class EventQueue<T>
{
    private Queue<T> m_queue = new();
    private object m_lock = new();

    public EventWaitHandle ConsumerWaitHandle = new EventWaitHandle(
            initialState:false, EventResetMode.ManualReset);

    public EventQueue() { }

    // Producer: Set the event
    public void AddEvent(T e) {
        lock (m_lock) {
            m_queue.Enqueue(e);
            if (m_queue.Count == 1) {
                ConsumerWaitHandle.Set();
            }
        }
    }

    // Consumer: call this if (and only if) ConsumerWaitHandle is signaled
    public T TakeEvent() {
        T ret;
        lock (m_lock) {
            //Log.Info($"Consumer read, size={m_queue.Count}");
            if (m_queue.Count == 0) {
                throw new Exception("empty queue");
            }
            ret = m_queue.Dequeue(); // This also throws an exception if empty
            if (m_queue.Count == 0) {
                ConsumerWaitHandle.Reset();
            }
        }
        return ret;
    }

}

