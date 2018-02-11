<%--
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
--%>

<%@ Page Language="C#" Inherits="Myrtille.Web.FileStorage" Codebehind="FileStorage.aspx.cs" AutoEventWireup="true" Culture="auto" UICulture="auto" %>
<%@ OutputCache Location="None" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
	
    <head>
        <title>Myrtille</title>
        <link rel="stylesheet" type="text/css" href="css/Default.css"/>
	</head>

    <body onload="onFileToUploadSuccess();">
        
        <form method="post" runat="server">
            
            <!-- upload/download file(s). only enabled if the connected server is localhost or if a domain is specified (so file(s) can be accessed within the rdp session) -->
            <div>
                <span id="fileStoragePopupDesc">Files into "My documents" folder</span><hr/>
                Upload file: <input type="file" runat="server" id="fileToUploadText" onchange="onFileToUploadChange(this);"/>
                <input type="button" runat="server" id="uploadFileButton" value="Upload" disabled="disabled" onserverclick="UploadFileButtonClick"/><br/>
                Download file: <select runat="server" id="fileToDownloadSelect" onchange="onFileToDownloadChange(this);"/>
                <input type="button" runat="server" id="downloadFileButton" value="Download" disabled="disabled" onserverclick="DownloadFileButtonClick"/><br/>
                <input type="button" id="closePopupButton" value="Close" onclick="parent.closePopup();"/>
            </div>
            
        </form>

		<script type="text/javascript" language="javascript" defer="defer">

            // upload file to user documents folder
            function onFileToUploadChange(fileToUploadText)
            {
                var uploadFileButton = document.getElementById('<%=uploadFileButton.ClientID%>');
                if (uploadFileButton != null)
                {
                    uploadFileButton.disabled = fileToUploadText.value == '';
                }
            }

            // download file from user documents folder
            function onFileToDownloadChange(fileToDownloadText)
            {
                var downloadFileButton = document.getElementById('<%=downloadFileButton.ClientID%>');
                if (downloadFileButton != null)
                {
                    downloadFileButton.disabled = fileToDownloadText.value == '';
                }
            }

            // upload file success
            function onFileToUploadSuccess()
            {
                var idx = window.location.search.indexOf('upload=success');
                if (idx != -1)
                {
                    alert('file was uploaded successfully');
                }
            }

		</script>

	</body>

</html>