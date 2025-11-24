using System;

namespace IcpDas.Daq.Service
{

    /// <summary>
    /// A generic, thread-safe cache for storing the last known value of a specific data type.
    /// It provides an event to notify subscribers of updates.
    /// </summary>
    /// <typeparam name="T">The type of data to be cached. It should be a reference type.</typeparam>
    public class SharedDataCache<T> where T : class
    {
        private readonly object _lock = new object();
        private T _lastData;

        /// <summary>
        /// Gets the last data received. Note: This returns a direct reference.
        /// For a thread-safe copy, use GetSafeSnapshot().
        /// </summary>
        public T LastData
        {
            get
            {
                lock (_lock)
                {
                    return _lastData;
                }
            }
            private set
            {
                _lastData = value;
            }
        }

        /// <summary>
        /// Occurs when the data is updated.
        /// </summary>
        public event EventHandler<T> DataUpdated;

        /// <summary>
        /// Updates the cached data and fires the DataUpdated event.
        /// </summary>
        /// <param name="newData">The new data to cache.</param>
        public void Update(T newData)
        {
            if (newData == null) return;

            lock (_lock)
            {
                this.LastData = newData;
            }

            // Fire the event outside the lock to prevent deadlocks and performance issues.
            DataUpdated?.Invoke(this, newData);
        }

        /// <summary>
        /// Gets a thread-safe snapshot of the last data.
        /// IMPORTANT: This method returns a shallow copy if the object is ICloneable,
        /// otherwise it returns the reference. For full safety against mutation,
        /// ensure T implements a proper cloning mechanism.
        /// </summary>
        /// <returns>A snapshot of the data.</returns>
        public T GetSafeSnapshot()
        {
            lock (_lock)
            {
                if (this.LastData == null)
                    return null;

                // If the object can be cloned, return a clone to prevent external modification.
                if (this.LastData is ICloneable cloneable)
                {
                    return (T)cloneable.Clone();
                }

                // Fallback: If not cloneable, return the reference.
                // This carries a risk if the caller modifies the object.
                return this.LastData;
            }
        }
    }
}
