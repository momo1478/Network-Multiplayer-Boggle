// Written by Joe Zachary for CS 3500, November 2012
// Revised by Joe Zachary April 2016

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CustomNetworking
{
    /// <summary> 
    /// A StringSocket is a wrapper around a Socket.  It provides methods that
    /// asynchronously read lines of text (strings terminated by newlines) and 
    /// write strings. (As opposed to Sockets, which read and write raw bytes.)  
    ///
    /// StringSockets are thread safe.  This means that two or more threads may
    /// invoke methods on a shared StringSocket without restriction.  The
    /// StringSocket takes care of the synchronization.
    /// 
    /// Each StringSocket contains a Socket object that is provided by the client.  
    /// A StringSocket will work properly only if the client refrains from calling
    /// the contained Socket's read and write methods.
    /// 
    /// If we have an open Socket s, we can create a StringSocket by doing
    /// 
    ///    StringSocket ss = new StringSocket(s, new UTF8Encoding());
    /// 
    /// We can write a string to the StringSocket by doing
    /// 
    ///    ss.BeginSend("Hello world", callback, payload);
    ///    
    /// where callback is a SendCallback (see below) and payload is an arbitrary object.
    /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
    /// successfully written the string to the underlying Socket, or failed in the 
    /// attempt, it invokes the callback.  The parameters to the callback are a
    /// (possibly null) Exception and the payload.  If the Exception is non-null, it is
    /// the Exception that caused the send attempt to fail.
    /// 
    /// We can read a string from the StringSocket by doing
    /// 
    ///     ss.BeginReceive(callback, payload)
    ///     
    /// where callback is a ReceiveCallback (see below) and payload is an arbitrary object.
    /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
    /// string of text terminated by a newline character from the underlying Socket, or
    /// failed in the attempt, it invokes the callback.  The parameters to the callback are
    /// a (possibly null) string, a (possibly null) Exception, and the payload.  Either the
    /// string or the Exception will be non-null, but nor both.  If the string is non-null, 
    /// it is the requested string (with the newline removed).  If the Exception is non-null, 
    /// it is the Exception that caused the send attempt to fail.
    /// </summary>
    public class StringSocket
    {
        /// <summary>
        /// Object that links CallBack to its payload
        /// </summary>
        public class CallBackObject
        {
            internal SendCallback method;
            internal object payload;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="s"></param>
            /// <param name="p"></param>
            public CallBackObject(SendCallback s, object p)
            {
                method = s;
                payload = p;
            }


        }

        /// <summary>
        /// Same as CallBackObject but for Recieve objects.
        /// </summary>
        public class CallBackReceive
        {
            internal ReceiveCallback method;
            internal object payload;

            /// <summary>
            /// Constructor 2.
            /// </summary>
            /// <param name="r"></param>
            /// <param name="p"></param>
            public CallBackReceive(ReceiveCallback r, object p)
            {
                method = r;
                payload = p;
            }
        }
        /// <summary>
        /// The type of delegate that is called when a send has completed.
        /// </summary>
        public delegate void SendCallback(Exception e, object payload);

        /// <summary>
        /// The type of delegate that is called when a receive has completed.
        /// </summary>
        public delegate void ReceiveCallback(String s, Exception e, object payload);

        // Underlying socket
        private Socket socket;

        // Encoding for Socket to translate bytes.
        private static System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

        // synching object for send.
        private readonly object sendSync = new object();

        // String buffer for pending bytes.
        private StringBuilder outgoing;
        // String buffer for recieving bytes.
        private StringBuilder incoming;

        // Determines if an ansyc send operation is happening.
        private bool sendIsOngoing = false;

        //Byte buffer for asynchronous send.
        //Amount of bytes successfully sent.
        private byte[] pendingBytes = new byte[0];
        private int pendingIndex = 0;

        // A queue to add call back methods to.
        private ConcurrentQueue<CallBackObject> callbackQueue = new ConcurrentQueue<CallBackObject>();
        private ConcurrentQueue<CallBackReceive> callbackReceiveQueue = new ConcurrentQueue<CallBackReceive>();

        // Used to decoud our UTF8-encoded byte stream.
        private Decoder decoder = encoding.GetDecoder();

        // Used to help declare our array size.
        private const int BUFFER_SIZE = 1024;

        // Buffers that will contain incoming bytes and characters.
        private byte[] incomingBytes = new byte[BUFFER_SIZE];
        private char[] incomingChars = new char[BUFFER_SIZE];

        private readonly object sync = new object();

        /// <summary>
        /// Creates a StringSocket from a regular Socket, which should already be connected.  
        /// The read and write methods of the regular Socket must not be called after the
        /// StringSocket is created.  Otherwise, the StringSocket will not behave properly.  
        /// The encoding to use to convert between raw bytes and strings is also provided.
        /// </summary>
        public StringSocket(Socket s, UTF8Encoding e)
        {
            socket = s;
            encoding = e;

            outgoing = new StringBuilder();
            incoming = new StringBuilder();
        }

        /// <summary>
        /// Shuts down and closes the socket.  No need to change this.
        /// </summary>
        public void Shutdown()
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// We can write a string to a StringSocket ss by doing
        /// 
        ///    ss.BeginSend("Hello world", callback, payload);
        ///    
        /// where callback is a SendCallback (see above) and payload is an arbitrary object.
        /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
        /// successfully written the string to the underlying Socket, or failed in the 
        /// attempt, it invokes the callback.  The parameters to the callback are a
        /// (possibly null) Exception and the payload.  If the Exception is non-null, it is
        /// the Exception that caused the send attempt to fail. 
        /// 
        /// This method is non-blocking.  This means that it does not wait until the string
        /// has been sent before returning.  Instead, it arranges for the string to be sent
        /// and then returns.  When the send is completed (at some time in the future), the
        /// callback is called on another thread.
        /// 
        /// This method is thread safe.  This means that multiple threads can call BeginSend
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginSend must take care of synchronization instead.  On a given StringSocket, each
        /// string arriving via a BeginSend method call must be sent (in its entirety) before
        /// a later arriving string can be sent.
        /// </summary>
        public void BeginSend(String s, SendCallback callback, object payload)
        {
            //writes string to socket (break string into bytes to send)
            callbackQueue.Enqueue(new CallBackObject(callback, payload) );
            SendMessage(s);

            //sends string via socket connection

            //calls callback on THREAD when send is complete

        }

        private void SendMessage(string lines)
        {
            // Get exclusive access to send mechanism
            lock (sendSync)
            {
                // Append the message to the outgoing lines
                outgoing.Append(lines);

                // If there's not a send ongoing, start one.
                if (!sendIsOngoing)
                {
                    sendIsOngoing = true;
                    SendBytes();
                }
            }
        }

        /// <summary>
        /// Attempts to send the entire outgoing string.
        /// This method should not be called unless sendSync has been acquired.
        /// </summary>
        private void SendBytes()
        {
            // If we're in the middle of the process of sending out a block of bytes,
            // keep doing that.
            if (pendingIndex < pendingBytes.Length)
            {
                socket.BeginSend(pendingBytes, pendingIndex, pendingBytes.Length - pendingIndex,
                                 SocketFlags.None, MessageSent, null);
            }

            // If we're not currently dealing with a block of bytes, make a new block of bytes
            // out of outgoing and start sending that.
            else if (outgoing.Length > 0)
            {
                pendingBytes = encoding.GetBytes(outgoing.ToString());
                pendingIndex = 0;
                outgoing.Clear();
                socket.BeginSend(pendingBytes, 0, pendingBytes.Length,
                                 SocketFlags.None, MessageSent, null);

                CallBackObject del;
                bool hasInvoked = false;
                while (!hasInvoked && callbackQueue.TryDequeue(out del))
                {
                    hasInvoked = true;

                    Task.Run(() => del.method.Invoke(null, del.payload));
                }
            }

            // If there's nothing to send, shut down for the time being.
            else
            {
                //Call callback here.
                sendIsOngoing = false;

                CallBackObject del;
                bool hasInvoked = false;
                while (!hasInvoked && callbackQueue.TryDequeue(out del))
                {
                    hasInvoked = true;

                    Task.Run(() => del.method.Invoke(null, del.payload));
                }
            }
        }

        /// <summary>
        /// Called when a message has been successfully sent
        /// </summary>
        private void MessageSent(IAsyncResult result)
        {
            // Find out how many bytes were actually sent
            int bytesSent = socket.EndSend(result);

            // Get exclusive access to send mechanism
            lock (sendSync)
            {
                // The socket has been closed
                if (bytesSent == 0)
                {
                    socket.Close();
                    Debug.WriteLine("Socket closed");
                }

                // Update the pendingIndex and keep trying
                else
                {
                    pendingIndex += bytesSent;
                    SendBytes();
                }
            }
        }

        /// <summary>
        /// We can read a string from the StringSocket by doing
        /// 
        ///     ss.BeginReceive(callback, payload)
        ///     
        /// where callback is a ReceiveCallback (see above) and payload is an arbitrary object.
        /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
        /// string of text terminated by a newline character from the underlying Socket, or
        /// failed in the attempt, it invokes the callback.  The parameters to the callback are
        /// a (possibly null) string, a (possibly null) Exception, and the payload.  Either the
        /// string or the Exception will be null, or possibly both.  If the string is non-null, 
        /// it is the requested string (with the newline removed).  If the Exception is non-null, 
        /// it is the Exception that caused the send attempt to fail.  If both are null, this
        /// indicates that the sending end of the remote socket has been shut down.
        /// 
        /// This method is non-blocking.  This means that it does not wait until a line of text
        /// has been received before returning.  Instead, it arranges for a line to be received
        /// and then returns.  When the line is actually received (at some time in the future), the
        /// callback is called on another thread.
        /// 
        /// This method is thread safe.  This means that multiple threads can call BeginReceive
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginReceive must take care of synchronization instead.  On a given StringSocket, each
        /// arriving line of text must be passed to callbacks in the order in which the corresponding
        /// BeginReceive call arrived.
        /// 
        /// Note that it is possible for there to be incoming bytes arriving at the underlying Socket
        /// even when there are no pending callbacks.  StringSocket implementations should refrain
        /// from buffering an unbounded number of incoming bytes beyond what is required to service
        /// the pending callbacks.
        /// </summary>
        public void BeginReceive(ReceiveCallback callback, object payload, int length = 0)
        {
            callbackReceiveQueue.Enqueue(new CallBackReceive(callback, payload));
            socket.BeginReceive(incomingBytes, 0, incomingBytes.Length, SocketFlags.None, MessageReceived, null);
            //TODO : Invoke call back method when a string of text is terminated by a newline character or failed to atempt to do so.
        }

        /// <summary>
        /// Called when some data has been received.
        /// </summary>
        private void MessageReceived(IAsyncResult result)
        {
            if (!socket.Connected)
            {
                return;
            }

            //Have we hit a \n ?
            bool readMore = true;

            // Figure out how many bytes have come in
            int bytesRead = socket.EndReceive(result);

            // If no bytes were received, it means the client closed its side of the socket.
            // Report that to the debug and close our socket.
            if (bytesRead == 0)
            {
                Debug.WriteLine("Socket closed");
                socket.Close();
            }
            // Otherwise, decode and display the incoming bytes.  Then request more bytes.
            else
            {
                // Convert the bytes into characters and appending to incoming
                int charsRead = decoder.GetChars(incomingBytes, 0, bytesRead, incomingChars, 0, false);

                int newLineIndex = charsRead;// = Array.FindIndex(incomingChars, (c) => (c.Equals('\n'))) == -1 ? 0 : Array.FindIndex(incomingChars, (c) => (c.Equals('\n')));
                for (int i = 0; i < charsRead; i++)
                {
                    if (incomingChars[i].Equals('\n'))
                    {
                        readMore = false;

                        newLineIndex = i;
                        break;
                    }

                }
                incoming.Append(incomingChars, 0, newLineIndex);
                Debug.WriteLine("Data Recieved is : " + incoming);
                
                if (readMore)
                {
                    // Ask for some more data
                    socket.BeginReceive(incomingBytes, 0, incomingBytes.Length, SocketFlags.None, MessageReceived, null);
                }
                else
                {
                    // If we are done recieving data call the callback method.

                    CallBackReceive del;
                    bool hasInvoked = false;
                    while (!hasInvoked && callbackReceiveQueue.TryDequeue(out del))
                    {
                        hasInvoked = true;
                        Task.Run(() => del.method.Invoke(incoming.ToString(), null, del.payload));
                    }
                }


            }
        }

    }
}