/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2018 Cedric Coste

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace Myrtille.Helpers
{
    public static class PipeHelper
    {
        /// <summary>
        /// read data from a named pipe
        /// </summary>
        /// <param name="pipe"></param>
        /// <param name="pipeName"></param>
        public static byte[] ReadPipeMessage(
            PipeStream pipe,
            string pipeName)
        {
            if (pipe == null)
            {
                Trace.TraceError("Failed to read message from pipe {0} (not set)", (string.IsNullOrEmpty(pipeName) ? "<unknown>" : pipeName));
                return null;
            }

            if (!pipe.IsConnected)
            {
                Trace.TraceError("Failed to read message from pipe {0} (not connected)", (string.IsNullOrEmpty(pipeName) ? "<unknown>" : pipeName));
                return null;
            }

            try
            {
                if (pipe.CanRead)
                {
                    var memoryStream = new MemoryStream();
                    var buffer = new byte[4];

                    var bytesRead = 0;
                    if ((bytesRead = pipe.Read(buffer, 0, 4)) == 4)
                    {
                        var size = BitConverter.ToInt32(buffer, 0);
                        buffer = new byte[size];
                        if ((bytesRead = pipe.Read(buffer, 0, size)) == size)
                        {
                            memoryStream.Write(buffer, 0, bytesRead);
                        }
                    }

                    return memoryStream.ToArray();
                }
                else
                {
                    throw new Exception("not readable");
                }
            }
            catch (IOException)
            {
                Trace.TraceError("Failed to read message from pipe {0} (I/O error)", (string.IsNullOrEmpty(pipeName) ? "<unknown>" : pipeName));
                throw;
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to read message from pipe {0} ({1})", (string.IsNullOrEmpty(pipeName) ? "<unknown>" : pipeName), exc);
                throw;
            }
        }

        /// <summary>
        /// write data to a named pipe
        /// </summary>
        /// <param name="pipe"></param>
        /// <param name="pipeName"></param>
        /// <param name="message"></param>
        public static void WritePipeMessage(
            PipeStream pipe,
            string pipeName,
            string message)
        {
            if (pipe == null)
            {
                Trace.TraceError("Failed to write message to pipe {0} (not set)", (string.IsNullOrEmpty(pipeName) ? "<unknown>" : pipeName));
                return;
            }

            if (!pipe.IsConnected)
            {
                Trace.TraceError("Failed to write message to pipe {0} (not connected)", (string.IsNullOrEmpty(pipeName) ? "<unknown>" : pipeName));
                return;
            }

            try
            {
                if (pipe.CanWrite)
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    pipe.Write(buffer, 0, buffer.Length);
                    pipe.Flush();
                }
                else
                {
                    throw new Exception("not writable");
                }
            }
            catch (IOException)
            {
                Trace.TraceError("Failed to write message to pipe {0} (I/O error)", (string.IsNullOrEmpty(pipeName) ? "<unknown>" : pipeName));
                throw;
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to write message to pipe {0} ({1})", (string.IsNullOrEmpty(pipeName) ? "<unknown>" : pipeName), exc);
                throw;
            }
        }
    }
}