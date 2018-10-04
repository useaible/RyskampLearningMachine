using RLM.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RLM.Database
{
    public class RlmObjectEnqueuer
    {
        private const int DELAY = 10;

        private long taskEnqueuedCounter = 0;
        public long TotalTaskEnqueued
        {
            get
            {
                return Interlocked.Read(ref taskEnqueuedCounter);
            }
        }

        public void QueueObjects<T>(ConcurrentQueue<T> cache, BlockingCollection<T> bc, CancellationToken token)
        {
            int index = 0;
            while (!token.IsCancellationRequested)
            {
                int batch = 10;
                int max = batch + index;
                //var values = cache.Skip(index).Take(batch);
                //index += values.Count();
                //foreach(var item in values)
                //{
                //    bc.Add(item);
                //}
                //for (int i = index; i < max; i++)
                //{
                //    if (i <= cache.Count - 1)
                //    {
                //        bc.Add(cache.ElementAt(i));
                //        index++;
                //    }
                //    else
                //    {
                //        break;
                //    }
                //}

                for (int i = 0; i < batch; i++)
                {
                    T item;
                    if (cache.TryDequeue(out item))
                    {
                        try
                        {
                            if (bc.TryAdd(item))
                            {
                                Interlocked.Increment(ref taskEnqueuedCounter);
                            }
                        }
                        catch (InvalidOperationException e)
                        {
                            System.Diagnostics.Debug.WriteLine($"TODO must handle this error properly: {e.Message}");
                        }
                    }
                }

                //Task.Delay(DELAY).Wait();
                Thread.Sleep(DELAY);
            }
        }

        public void QueueObjects(ConcurrentQueue<Queue<Case>> cache, BlockingCollection<Case> bc, object queue_lock, CancellationToken token)
        {
            int index = 0;
            while (!token.IsCancellationRequested)
            {
                Queue<Case> currentReadQueue;
                Queue<Case> doneReadQueue;
                if (cache.TryPeek(out currentReadQueue))
                {
                    if (currentReadQueue.Count > 0)
                    {
                        int batch = 10;
                        int max = batch + index;

                        lock (queue_lock)
                        {
                            for (int i = 0; i < batch; i++)
                            {
                                if (currentReadQueue.Count > 0)
                                {
                                    Case item = currentReadQueue.Dequeue();
                                    if (bc.TryAdd(item))
                                    {
                                        Interlocked.Increment(ref taskEnqueuedCounter);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (cache.Count > 1)
                        {
                            if (cache.TryDequeue(out doneReadQueue))
                            {
                                doneReadQueue = null;
                            }
                        }
                    }
                }

                //Task.Delay(DELAY).Wait();
                Thread.Sleep(DELAY);
            }
        }
    }
}
