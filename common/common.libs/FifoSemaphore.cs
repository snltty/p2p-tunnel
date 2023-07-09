using System;
using System.Collections.Generic;
using System.Threading;

namespace common.libs
{
    /// <summary>
	/// A semaphore is a concurrency utility that contains a number of "tokens". Threads try to acquire
	/// (take) and release (put) these tokens into the semaphore. When a semaphore contains no tokens,
	/// threads that try to acquire a token will block until a token is released into the semaphore.
	/// </summary>
	public interface ISemaphore
    {
        /// <summary>
        /// Try to acquire a token but time out if a token cannot be acquired after certain amount of time.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait to acquire a token. This can be set to 
        /// <see cref="Timeout.Infinite"/> if you want to wait forever.
        /// </param>
        /// <returns>
        /// true if a token was acquired successfully; false if a token has not been acquired after the amount
        /// of time specified by <paramref name="millisecondsTimeout"/> has elapsed.
        /// </returns>
        /// <exception cref="ThreadInterruptedException">
        /// If the calling thread was interrupted while waiting to acquire a token
        /// </exception>
        bool TryAcquire(int millisecondsTimeout);

        /// <summary>
        /// Try to acquire a token but time out if a token cannot be acquired after certain amount of time.
        /// <see cref="ThreadInterruptedException"/> are guaranteed not be thrown by this method.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait to acquire a token. This can be set to 
        /// <see cref="Timeout.Infinite"/> if you want to wait forever.
        /// </param>
        /// <returns>
        /// true if a token was acquired successfully; false if a token has not been acquired after the amount
        /// of time specified by <paramref name="millisecondsTimeout"/> has elapsed.
        /// </returns>
        bool ForceTryAcquire(int millisecondsTimeout);

        /// <summary>
        /// Acquires a token waiting for as long as necessary to do so.
        /// </summary>
        /// <exception cref="ThreadInterruptedException">
        /// If the calling thread was interrupted while waiting to acquire a token
        /// </exception>
        void Acquire();

        /// <summary>
        /// Acquires a token waiting for as long as necessary to do so. 
        /// <see cref="ThreadInterruptedException"/> are guaranteed not be thrown by this method.
        /// </summary>
        void ForceAcquire();

        /// <summary>
        /// Releases a token.
        /// </summary>
        /// <exception cref="ThreadInterruptedException">
        /// If the calling thread was interrupted while waiting to release a token
        /// </exception>
        void Release();

        /// <summary>
        /// Releases many tokens.
        /// </summary>
        /// <param name="tokens">The number of tokens to release</param>
        /// <exception cref="ThreadInterruptedException">
        /// If the calling thread was interrupted while waiting to release tokens
        /// </exception>
        void ReleaseMany(int tokens);

        /// <summary>
        /// Releases a token. <see cref="ThreadInterruptedException"/> are guaranteed not be thrown by this
        /// method.
        /// </summary>
        void ForceRelease();

        /// <summary>
        /// Releases many tokens. <see cref="ThreadInterruptedException"/> are guaranteed not be thrown by this
        /// method.
        /// </summary>
        /// <param name="tokens">The number of tokens to release</param>
        void ForceReleaseMany(int tokens);
    }
    public class FifoSemaphore : ISemaphore
    {
        private readonly Queue<Waiter> _WaitQueue;

        /// <summary>
        /// The lock object for this class
        /// </summary>
        protected readonly object _Lock;

        /// <summary>
        /// The tokens count for this class
        /// </summary>
        protected int _Tokens;


        /// <summary>
        /// Constructor, creates a FifoSemaphore
        /// </summary>
        /// <param name="tokens">The number of tokens the semaphore will start with</param>
        public FifoSemaphore(int tokens)
        {
            _Tokens = tokens;
            _Lock = new object();
            _WaitQueue = new Queue<Waiter>();
        }


        /// <summary>
        /// Try to acquire a token but time out if a token cannot be acquired after certain amount of time.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait to acquire a token. This can be set to 
        /// <see cref="Timeout.Infinite"/> if you want to wait forever.
        /// </param>
        /// <returns>
        /// true if a token was acquired successfully; false if a token has not been acquired after the amount
        /// of time specified by <paramref name="millisecondsTimeout"/> has elapsed.
        /// </returns>
        /// <exception cref="ThreadInterruptedException">
        /// If the calling thread was interrupted while waiting to acquire a token
        /// </exception>
        public bool TryAcquire(int millisecondsTimeout)
        {
            Waiter waiter;

            lock (_Lock)
            {
                if (_Tokens > 0)
                {
                    _Tokens--;
                    return true;
                }

                waiter = new Waiter();
                _WaitQueue.Enqueue(waiter);
            }

            return waiter.TryWait(millisecondsTimeout); //I will be woken up when I'm at the head of the queue 
                                                        //or if I'm interrupted (an exception will be thrown then)
        }


        /// <summary>
        /// Try to acquire a token but time out if a token cannot be acquired after certain amount of time.
        /// <see cref="ThreadInterruptedException"/> are guaranteed not be thrown by this method.
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait to acquire a token. This can be set to 
        /// <see cref="Timeout.Infinite"/> if you want to wait forever.
        /// </param>
        /// <returns>
        /// true if a token was acquired successfully; false if a token has not been acquired after the amount
        /// of time specified by <paramref name="millisecondsTimeout"/> has elapsed.
        /// </returns>
        public bool ForceTryAcquire(int millisecondsTimeout)
        {
            bool retval;

            try
            {
            }
            finally
            {
                retval = TryAcquire(millisecondsTimeout);
            }

            return retval;
        }


        /// <summary>
        /// Acquires a token waiting for as long as necessary to do so.
        /// </summary>
        /// <exception cref="ThreadInterruptedException">
        /// If the calling thread was interrupted while waiting to acquire a token
        /// </exception>
        public void Acquire()
        {
            TryAcquire(Timeout.Infinite);
        }


        /// <summary>
        /// Acquires a token waiting for as long as necessary to do so. 
        /// <see cref="ThreadInterruptedException"/> are guaranteed not be thrown by this method.
        /// </summary>
        public void ForceAcquire()
        {
            try
            {
            }
            finally
            {
                Acquire();
            }
        }


        /// <summary>
        /// Releases a token.
        /// </summary>
        /// <exception cref="ThreadInterruptedException">
        /// If the calling thread was interrupted while waiting to release a token
        /// </exception>
        public void Release()
        {
            ReleaseMany(1);
        }


        /// <summary>
        /// Releases many tokens.
        /// </summary>
        /// <param name="tokens">The number of tokens to release</param>
        /// <exception cref="ThreadInterruptedException">
        /// If the calling thread was interrupted while waiting to release tokens
        /// </exception>
        public virtual void ReleaseMany(int tokens)
        {
            lock (_Lock)
            {
                for (int i = 0; i < tokens; i++)
                {
                    if (_WaitQueue.Count > 0)
                    {
                        Waiter waiter = _WaitQueue.Dequeue();
                        bool releasedSuccessfully = waiter.Release();

                        if (!releasedSuccessfully) //That thread was interrupted or timed out!
                            i--; //Try again with the next
                    }
                    else
                    {
                        //We've got no one waiting, so add a token
                        _Tokens++;
                    }
                }
            }
        }


        /// <summary>
        /// Releases a token. <see cref="ThreadInterruptedException"/> are guaranteed not be thrown by this
        /// method.
        /// </summary>
        public void ForceRelease()
        {
            try
            {
            }
            finally
            {
                Release();
            }
        }


        /// <summary>
        /// Releases many tokens. <see cref="ThreadInterruptedException"/> are guaranteed not be thrown by this
        /// method.
        /// </summary>
        /// <param name="tokens">The number of tokens to release</param>
        public void ForceReleaseMany(int tokens)
        {
            try
            {
            }
            catch (Exception)
            {
                ReleaseMany(_Tokens);
            }
        }


        /// <summary>
        /// Waiter helper class that allows threads to queue for tokens
        /// </summary>
        private class Waiter
        {
            private readonly object _Lock;
            private bool _Released;


            public Waiter()
            {
                _Lock = new object();
                _Released = false;
            }


            /// <summary>
            /// Causes a thread to acquire an internal lock and then wait on it until it is pulsed or a timeout
            /// occurs
            /// </summary>
            /// <param name="millisecondsTimeout">
            /// The number of milliseconds to wait on the lock. This can be set to <see cref="Timeout.Infinite"/>
            /// if you want to wait forever.
            /// </param>
            /// <returns>True if the thread was released successfully, false if a timeout occurred</returns>
            public bool TryWait(int millisecondsTimeout)
            {
                lock (_Lock)
                {
                    if (_Released) //We've been released before we even started waiting!
                        return true;

                    try
                    {
                        bool success = Monitor.Wait(_Lock, millisecondsTimeout);

                        if (!success)
                            _Released = true; //Note that we've been released early

                        return success;
                    }
                    catch (ThreadInterruptedException)
                    {
                        if (_Released == false)
                        {
                            _Released = true; //Note that we've been released early
                            throw;
                        }

                        //We've already been released, so we might as well succeed at
                        //the operation and get interrupted later
                        Thread.CurrentThread.Interrupt();
                        return true;
                    }
                }
            }


            /// <summary>
            /// Causes the thread currently waiting on the lock to be woken up if it is still waiting
            /// </summary>
            /// <returns>
            /// True if the thread was woken successfully, false if it was woken early by an interrupt or a timeout
            /// </returns>
            public bool Release()
            {
                lock (_Lock)
                {
                    if (_Released) //If released already (this means we've been interrupted or we timed out!)
                        return false;

                    _Released = true;
                    Monitor.Pulse(_Lock);
                    return true;
                }
            }
        }
    }
}
