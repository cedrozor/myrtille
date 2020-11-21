/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2020 Cedric Coste

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
using System.IO;
using System.ServiceModel;

namespace Myrtille.Services.Contracts
{
    [MessageContract]
    public class UploadRequest
    {
        [MessageHeader(MustUnderstand = true)]
        public Guid RemoteSessionId { get; set; }

        [MessageHeader(MustUnderstand = true)]
        public string UserDomain { get; set; }

        [MessageHeader(MustUnderstand = true)]
        public string UserName { get; set; }

        [MessageHeader(MustUnderstand = true)]
        public string UserPassword { get; set; }

        [MessageHeader(MustUnderstand = true)]
        public string FileName { get; set; }
     
        [MessageBodyMember(Order = 1)]
        public Stream FileStream { get; set; }
    }

    [ServiceContract]
    public interface IFileStorage
    {
        /// <summary>
        /// list the file(s) into the given user documents folder
        /// </summary>
        [OperationContract]
        List<string> GetUserDocumentsFolderFiles(
            Guid remoteSessionId,
            string userDomain,
            string userName,
            string userPassword);

        /// <summary>
        /// upload a file to the given user documents folder
        /// </summary>
        [OperationContract]
        void UploadFileToUserDocumentsFolder(
            UploadRequest uploadRequest);

        /// <summary>
        /// download a file from the given user documents folder
        /// </summary>
        [OperationContract]
        Stream DownloadFileFromUserDocumentsFolder(
            Guid remoteSessionId,
            string userDomain,
            string userName,
            string userPassword,
            string fileName);

        /// list the file(s) into the given user specified folder
        /// </summary>
        [OperationContract]
        List<string> GetUserFolderFiles(Guid remoteSessionId, string userDomain, string userName, string userPassword, string folderGuid);

        /// <summary>
        /// download a file from the given user specified folder
        /// </summary>
        [OperationContract]
        Stream DownloadFileFromUserFolder(Guid remoteSessionId, string userDomain, string userName, string userPassword, string folderGuid, string fileName);
    }
}