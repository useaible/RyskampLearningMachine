using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLM.Memory.Utility
{
    public class RlmArray<T>
    {
        private T[] arr;
        private int index = -1;
        private int size = 0;
        private T defaultValue;

        public RlmArray(int size, T defaultVal = default(T))
        {
            this.size = size;
            this.defaultValue = defaultVal;
            arr = Enumerable.Repeat(defaultVal, size).ToArray();
        }

        public T this[int i]
        {
            get { return arr[i]; }
            set { arr[i] = value; }
        }   

        public T[] DataArray
        {
            get { return arr; }
        }

        public int Index
        {
            get { return index; }
        }

        public void Reset()
        {
            Array.Clear(arr, 0, arr.Length);
            //arr = Enumerable.Repeat(defaultValue, arr.Length).ToArray();
        }

        public void Resize(int newSize)
        {
            if (arr.Length != newSize)
            {
                int oldLength = arr.Length;
                Array.Resize(ref arr, newSize);
                for (int i = oldLength; i < newSize; i++)
                {
                    arr[i] = defaultValue;
                }
            }
        }

        public void Add(T data)
        {
            index++;
            if (index != 0 && (index % (arr.Length - 1)) == 0)
            {
                Resize(arr.Length + size);
            }
            DataArray[index] = data;
        }
    }
}
