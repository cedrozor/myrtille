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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public class FileStorageClient : ClientBase<IFileStorage>, IFileStorage
    {
        public List<string> GetUserDocumentsFolderFiles(int remoteSessionId, string userDomain, string userName, string userPassword)
        {
            try
            {
                return Channel.GetUserDocumentsFolderFiles(remoteSessionId, userDomain, userName, userPassword);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to list file(s) from user {0} documents folder, remote session {1} ({2})", userName, remoteSessionId, exc);
                throw;
            }
        }

        public void UploadFileToUserDocumentsFolder(UploadRequest uploadRequest)
        {
            try
            {
                Channel.UploadFileToUserDocumentsFolder(uploadRequest);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to upload file {0} to user {1} documents folder, remote session {2} ({3})", uploadRequest.FileName, uploadRequest.UserName, uploadRequest.RemoteSessionId, exc);
                throw;
            }
        }

        public Stream DownloadFileFromUserDocumentsFolder(int remoteSessionId, string userDomain, string userName, string userPassword, string fileName)
        {
            try
            {
                return Channel.DownloadFileFromUserDocumentsFolder(remoteSessionId, userDomain, userName, userPassword, fileName);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to download file {0} from user {1} documents folder, remote session {2} ({3})", fileName, userName, remoteSessionId, exc);
                throw;
            }
        }
    }
}