using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Windows.Forms;
using System.Drawing;

namespace mic_output
{
    public class FixedSizedList<T>
    {
        List<T> l = new List<T>();

        public FixedSizedList(int limit)
        {
            Limit = limit;
        }

        public int Limit { get; set; }

        void _EnqueueLocked(T obj)
        {
            l.Insert(0, obj);
            while (l.Count > Limit && l.Count > 0)
                l.RemoveAt(l.Count - 1);
        }

        public void Enqueue(T obj)
        {
            lock (l)
            {
                _EnqueueLocked(obj);
            }
        }

        public void EnqueueRange(T[] values, int skipCount)
        {
            lock (l)
            {
                for (int i = 0; i < values.Count(); i++)
                {
                    if (i % skipCount == 0)
                        _EnqueueLocked(values[i]);
                }
            }
        }

        public int Count()
        {
            lock (l)
            {
                return l.Count;
            }
        }

        public T[] Copy()
        {
            lock (l)
            {
                T[] a = new T[l.Count];
                l.CopyTo(a);
                return a;
            }
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
        }
    }
}
