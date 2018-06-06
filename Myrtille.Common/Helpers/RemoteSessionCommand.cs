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

using System.Collections;

namespace Myrtille.Helpers
{
    public enum RemoteSessionCommand
    {
        // connection
        SendServerAddress = 0,
        SendUserDomain = 1,
        SendUserName = 2,
        SendUserPassword = 3,
        SendStartProgram = 4,
        ConnectRdpClient = 5,

        // browser
        SendBrowserResize = 6,

        // keyboard
        SendKeyUnicode = 7,
        SendKeyScancode = 8,

        // mouse
        SendMouseMove = 9,
        SendMouseLeftButton = 10,
        SendMouseMiddleButton = 11,
        SendMouseRightButton = 12,
        SendMouseWheelUp = 13,
        SendMouseWheelDown = 14,

        // control
        SetStatMode = 15,
        SetDebugMode = 16,
        SetCompatibilityMode = 17,
        SetScaleDisplay = 18,
        SetImageEncoding = 19,
        SetImageQuality = 20,
        SetImageQuantity = 21,
        RequestFullscreenUpdate = 22,
        RequestRemoteClipboard = 23,
        CloseRdpClient = 24
    }

    /*
    prefixes (3 chars) are used to serialize commands with strings instead of numbers
    they make it easier to read log traces to find out which commands are issued
    they must match the prefixes used client side
    */
    public static class RemoteSessionCommandMapping
    {
        public static Hashtable FromPrefix { get; private set; }
        public static Hashtable ToPrefix { get; private set; }

        static RemoteSessionCommandMapping()
        {
            FromPrefix = new Hashtable();
            FromPrefix["SRV"] = RemoteSessionCommand.SendServerAddress;
            FromPrefix["DOM"] = RemoteSessionCommand.SendUserDomain;
            FromPrefix["USR"] = RemoteSessionCommand.SendUserName;
            FromPrefix["PWD"] = RemoteSessionCommand.SendUserPassword;
            FromPrefix["PRG"] = RemoteSessionCommand.SendStartProgram;
            FromPrefix["CON"] = RemoteSessionCommand.ConnectRdpClient;
            FromPrefix["RSZ"] = RemoteSessionCommand.SendBrowserResize;
            FromPrefix["KUC"] = RemoteSessionCommand.SendKeyUnicode;
            FromPrefix["KSC"] = RemoteSessionCommand.SendKeyScancode;
            FromPrefix["MMO"] = RemoteSessionCommand.SendMouseMove;
            FromPrefix["MLB"] = RemoteSessionCommand.SendMouseLeftButton;
            FromPrefix["MMB"] = RemoteSessionCommand.SendMouseMiddleButton;
            FromPrefix["MRB"] = RemoteSessionCommand.SendMouseRightButton;
            FromPrefix["MWU"] = RemoteSessionCommand.SendMouseWheelUp;
            FromPrefix["MWD"] = RemoteSessionCommand.SendMouseWheelDown;
            FromPrefix["STA"] = RemoteSessionCommand.SetStatMode;
            FromPrefix["DBG"] = RemoteSessionCommand.SetDebugMode;
            FromPrefix["CMP"] = RemoteSessionCommand.SetCompatibilityMode;
            FromPrefix["SCA"] = RemoteSessionCommand.SetScaleDisplay;
            FromPrefix["ECD"] = RemoteSessionCommand.SetImageEncoding;
            FromPrefix["QLT"] = RemoteSessionCommand.SetImageQuality;
            FromPrefix["QNT"] = RemoteSessionCommand.SetImageQuantity;
            FromPrefix["FSU"] = RemoteSessionCommand.RequestFullscreenUpdate;
            FromPrefix["CLP"] = RemoteSessionCommand.RequestRemoteClipboard;
            FromPrefix["CLO"] = RemoteSessionCommand.CloseRdpClient;

            ToPrefix = new Hashtable();
            ToPrefix[RemoteSessionCommand.SendServerAddress] = "SRV";
            ToPrefix[RemoteSessionCommand.SendUserDomain] = "DOM";
            ToPrefix[RemoteSessionCommand.SendUserName] = "USR";
            ToPrefix[RemoteSessionCommand.SendUserPassword] = "PWD";
            ToPrefix[RemoteSessionCommand.SendStartProgram] = "PRG";
            ToPrefix[RemoteSessionCommand.ConnectRdpClient] = "CON";
            ToPrefix[RemoteSessionCommand.SendBrowserResize] = "RSZ";
            ToPrefix[RemoteSessionCommand.SendKeyUnicode] = "KUC";
            ToPrefix[RemoteSessionCommand.SendKeyScancode] = "KSC";
            ToPrefix[RemoteSessionCommand.SendMouseMove] = "MMO";
            ToPrefix[RemoteSessionCommand.SendMouseLeftButton] = "MLB";
            ToPrefix[RemoteSessionCommand.SendMouseMiddleButton] = "MMB";
            ToPrefix[RemoteSessionCommand.SendMouseRightButton] = "MRB";
            ToPrefix[RemoteSessionCommand.SendMouseWheelUp] = "MWU";
            ToPrefix[RemoteSessionCommand.SendMouseWheelDown] = "MWD";
            ToPrefix[RemoteSessionCommand.SetStatMode] = "STA";
            ToPrefix[RemoteSessionCommand.SetDebugMode] = "DBG";
            ToPrefix[RemoteSessionCommand.SetCompatibilityMode] = "CMP";
            ToPrefix[RemoteSessionCommand.SetScaleDisplay] = "SCA";
            ToPrefix[RemoteSessionCommand.SetImageEncoding] = "ECD";
            ToPrefix[RemoteSessionCommand.SetImageQuality] = "QLT";
            ToPrefix[RemoteSessionCommand.SetImageQuantity] = "QNT";
            ToPrefix[RemoteSessionCommand.RequestFullscreenUpdate] = "FSU";
            ToPrefix[RemoteSessionCommand.RequestRemoteClipboard] = "CLP";
            ToPrefix[RemoteSessionCommand.CloseRdpClient] = "CLO";
        }
    }
}