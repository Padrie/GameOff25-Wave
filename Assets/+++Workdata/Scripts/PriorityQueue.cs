using System;
using System.Collections.Generic;

public class PriorityQueue<T>
{
    private readonly List<(T item, float priority)> heap = new List<(T, float)>();

    public int Count => heap.Count;

    public void Enqueue(T item, float priority)
    {
        heap.Add((item, priority));
        HeapifyUp(heap.Count - 1);
    }

    public T Dequeue()
    {
        if (heap.Count == 0) throw new InvalidOperationException("PQ empty");

        T top = heap[0].item;

        heap[0] = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);

        if (heap.Count > 0)
            HeapifyDown(0);

        return top;
    }

    public bool Contains(T item)
    {
        for (int i = 0; i < heap.Count; i++)
            if (EqualityComparer<T>.Default.Equals(heap[i].item, item))
                return true;

        return false;
    }

    private void HeapifyUp(int idx)
    {
        while (idx > 0)
        {
            int parent = (idx - 1) / 2;
            if (heap[idx].priority >= heap[parent].priority)
                break;

            (heap[idx], heap[parent]) = (heap[parent], heap[idx]);
            idx = parent;
        }
    }

    private void HeapifyDown(int idx)
    {
        int last = heap.Count - 1;

        while (true)
        {
            int left = idx * 2 + 1;
            int right = left + 1;

            if (left > last) break;

            int smallest = (right <= last && heap[right].priority < heap[left].priority)
                ? right
                : left;

            if (heap[idx].priority <= heap[smallest].priority)
                break;

            (heap[idx], heap[smallest]) = (heap[smallest], heap[idx]);
            idx = smallest;
        }
    }
}
