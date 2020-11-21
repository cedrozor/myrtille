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
using System.IO;
using System.Threading;
using System.Web.UI;
using System.Web.Configuration;
using Myrtille.Helpers;
using Myrtille.Services.Contracts;
using System.Configuration;

namespace Myrtille.Web
{
    public partial class FileStorage : Page
    {
        private FileStorageClient _fileStorageClient = new FileStorageClient();
        private RemoteSession _remoteSession;

        /// <summary>
        /// page init
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Init(
            object sender,
            EventArgs e)
        {
            // retrieve the active remote session
            try
            {
                var stringTest = ConfigurationManager.AppSettings["SpecificFolderGUID"];
                if (Session[HttpSessionStateVariables.RemoteSession.ToString()] == null)
                    throw new NullReferenceException();

                _remoteSession = (RemoteSession)Session[HttpSessionStateVariables.RemoteSession.ToString()];

                // if a domain is specified, the roaming user profile is loaded from the Active Directory
                // file storage is synchronized with the user "My documents" folder (will use folder redirection if defined)
                // user credentials will be checked prior to any file operation
                // if possible, use SSL to communicate with the service
                if (_remoteSession.State == RemoteSessionState.Connected &&
                    Session.SessionID.Equals(_remoteSession.OwnerSessionID) &&
                    _remoteSession.HostType != HostType.SSH &&
                    _remoteSession.AllowFileTransfer &&
                    (_remoteSession.ServerAddress.ToLower() == "localhost" || _remoteSession.ServerAddress == "127.0.0.1" || _remoteSession.ServerAddress == "[::1]" || _remoteSession.ServerAddress == Request.Url.Host || !string.IsNullOrEmpty(_remoteSession.UserDomain)) &&
                    !string.IsNullOrEmpty(_remoteSession.UserName) && !string.IsNullOrEmpty(_remoteSession.UserPassword) &&
                    string.IsNullOrEmpty(_remoteSession.VMGuid))
                {
                    try
                    {
                        var files = _fileStorageClient.GetUserDocumentsFolderFiles(
                            _remoteSession.Id,
                            _remoteSession.UserDomain,
                            _remoteSession.UserName,
                            _remoteSession.UserPassword);

                        if (files.Count > 0)
                        {
                            fileToDownloadSelect.DataSource = files;
                            fileToDownloadSelect.DataBind();
                            downloadFileButton.Disabled = false;
                        }
                    }
                    catch (Exception exc)
                    {
                        System.Diagnostics.Trace.TraceError("Failed to retrieve the user documents ({0})", exc);
                    }
                }


                // if a domain is specified, the roaming user profile is loaded from the Active Directory
                // file storage is synchronized with the user "Specified GUID" folder (will use folder redirection if defined)
                // user credentials will be checked prior to any file operation
                // if possible, use SSL to communicate with the service
                if (_remoteSession.State == RemoteSessionState.Connected &&
                    Session.SessionID.Equals(_remoteSession.OwnerSessionID) &&
                    _remoteSession.HostType != HostType.SSH &&
                    _remoteSession.AllowFileTransfer &&
                    (_remoteSession.ServerAddress.ToLower() == "localhost" || _remoteSession.ServerAddress == "127.0.0.1" || _remoteSession.ServerAddress == "[::1]" || _remoteSession.ServerAddress == Request.Url.Host || !string.IsNullOrEmpty(_remoteSession.UserDomain)) &&
                    !string.IsNullOrEmpty(_remoteSession.UserName) && !string.IsNullOrEmpty(_remoteSession.UserPassword) &&
                    string.IsNullOrEmpty(_remoteSession.VMGuid) &&
                    ConfigurationManager.AppSettings["SpecificFolderGUID"] != null)
                {
                    try
                    {
                        var _folderGuid = ConfigurationManager.AppSettings["SpecificFolderGUID"];
                        System.Diagnostics.Trace.WriteLine(_folderGuid);
                        var files = _fileStorageClient.GetUserFolderFiles(
                            _remoteSession.Id,
                            _remoteSession.UserDomain,
                            _remoteSession.UserName,
                            _remoteSession.UserPassword,
                            _folderGuid
                            );

                        if (files.Count > 0)
                        {
                            specifiedFileToDownloadSelect.DataSource = files;
                            specifiedFileToDownloadSelect.DataBind();
                            specifiedDownloadFileButton.Disabled = false;
                        }
                    }
                    catch (Exception exc)
                    {
                        System.Diagnostics.Trace.TraceError("Failed to retrieve the user documents ({0})", exc);
                    }
                }

            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to retrieve the active remote session ({0})", exc);
            }
        }

        /// <summary>
        /// upload a file to the user "My documents" folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void UploadFileButtonClick(
            object sender,
            EventArgs e)
        {
            try
            {
                if (_fileStorageClient == null)
                    return;

                if (fileToUploadText.PostedFile == null)
                {
                    System.Diagnostics.Trace.TraceInformation("File to upload is missing");
                }
                else if (fileToUploadText.PostedFile.ContentLength == 0)
                {
                    System.Diagnostics.Trace.TraceInformation("File to upload is empty");
                }
                else
                {
                    _fileStorageClient.UploadFileToUserDocumentsFolder(
                        new UploadRequest
                        {
                            RemoteSessionId = _remoteSession.Id,
                            UserDomain = _remoteSession.UserDomain,
                            UserName = _remoteSession.UserName,
                            UserPassword = _remoteSession.UserPassword,
                            FileName = Path.GetFileName(fileToUploadText.PostedFile.FileName),
                            FileStream = fileToUploadText.PostedFile.InputStream
                        });

                    // reload the page to have the newly uploaded file available for download
                    Response.Redirect(Request.RawUrl + (Request.RawUrl.Contains("?") ? "&" : "?") + "upload=success");
                }
            }
            catch (ThreadAbortException)
            {
                // occurs because the response is ended after reloading the page
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to upload file ({0})", exc);
            }
        }

        /// <summary>
        /// download a file from the user "My documents" folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void DownloadFileButtonClick(
            object sender,
            EventArgs e)
        {
            try
            {
                if (_fileStorageClient == null)
                    return;

                if (!string.IsNullOrEmpty(fileToDownloadSelect.Value))
                {
                    var fileStream = _fileStorageClient.DownloadFileFromUserDocumentsFolder(
                        _remoteSession.Id,
                        _remoteSession.UserDomain,
                        _remoteSession.UserName,
                        _remoteSession.UserPassword,
                        fileToDownloadSelect.Value);

                    FileHelper.DownloadFile(Response, fileStream, fileToDownloadSelect.Value, true);
                }
            }
            catch (ThreadAbortException)
            {
                // occurs because the response is ended after sending the file content
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to download file ({0})", exc);
            }
        }


        /// <summary>
        /// download a file from the user Specified folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void SpecifiedDownloadFileButtonClick(
            object sender,
            EventArgs e)
        {
            try
            {
                if (_fileStorageClient == null)
                    return;

                if (!string.IsNullOrEmpty(specifiedFileToDownloadSelect.Value))
                {
                    var _folderGuid = ConfigurationManager.AppSettings["SpecificFolderGUID"];
                    var fileStream = _fileStorageClient.DownloadFileFromUserFolder(
                        _remoteSession.Id,
                        _remoteSession.UserDomain,
                        _remoteSession.UserName,
                        _remoteSession.UserPassword,
                        _folderGuid,
                        specifiedFileToDownloadSelect.Value);

                    FileHelper.DownloadFile(Response, fileStream, specifiedFileToDownloadSelect.Value, true);
                }
            }
            catch (ThreadAbortException)
            {
                // occurs because the response is ended after sending the file content
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to download file ({0})", exc);
            }
        }
    }
}