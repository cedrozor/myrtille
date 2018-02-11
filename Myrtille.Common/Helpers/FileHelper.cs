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
using System.Threading;
using System.Web;

namespace Myrtille.Helpers
{
    public static class FileHelper
    {
        /// <summary>
        /// copy a file
        /// </summary>
        /// <param name="sourceFileStream"></param>
        /// <param name="targetFileStream"></param>
        /// <returns>success or failure</returns>
        public static bool CopyFile(
            Stream sourceFileStream,
            Stream targetFileStream)
        {
            var streamOk = true;

            try
            {
                if (sourceFileStream == null || targetFileStream == null)
                    throw new Exception("source and/or target streams are null, operation aborted");

                int bytesRead;
                var buffer = new byte[4096];
                while ((bytesRead = sourceFileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    targetFileStream.Write(buffer, 0, bytesRead);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to copy file ({0})", exc);
                streamOk = false;
            }
            finally
            {
                if (sourceFileStream != null)
                {
                    sourceFileStream.Close();
                }

                if (targetFileStream != null)
                {
                    targetFileStream.Close();
                }
            }

            return streamOk;
        }

        /// <summary>
        /// send a file to the browser
        /// </summary>
        /// <param name="response"></param>
        /// <param name="fileStream"></param>
        /// <param name="fileName"></param>
        /// <param name="endResponse"></param>
        public static void DownloadFile(
            HttpResponse response,
            Stream fileStream,
            string fileName,
            bool endResponse)
        {
            try
            {
                if (fileStream == null)
                    throw new Exception("file stream is null, operation aborted");

                response.ContentType = "application/octet-stream";
                response.AppendHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\";");

                if (fileStream.CanSeek)
                {
                    response.AppendHeader("Content-Length", fileStream.Length.ToString());
                }

                int bytesRead;
                var buffer = new byte[4096];
                while (((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0) && (response.IsClientConnected))
                {
                    response.OutputStream.Write(buffer, 0, bytesRead);
                    response.Flush();
                }

                if (endResponse)
                {
                    response.End();
                }
            }
            catch (ThreadAbortException)
            {
                // may happen if the connection is closed unexpectedly while streaming data (i.e.: if a long-polling connection is reseted); do nothing
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
            }
        }
    }
}