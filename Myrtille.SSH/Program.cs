/*
    Myrtille: A native HTML4/5 Remote Desktop and SSH Protocol client.

    Copyright(c) 2018 bigpjo/Olive Innovations

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
using System.Linq;
using System.Text;
using System.Threading;
using Myrtille.Helpers;
using Renci.SshNet;

namespace Myrtille.SSH
{
    class Program
    {
        private static PipeMessaging pipeMessaging;
        private static string LastKeyCode { get; set; }
        private static SshClient client;
        private static ShellStream shellStream;


        static void Main(string[] args)
        {
            string argOptionParamterSeperators = ":";

            foreach(string arg in args)
            {
                var argParts = arg.Trim().Split(argOptionParamterSeperators.ToCharArray(), 2);
                parseCommandLineArg(argParts[0].ToLower(), (argParts.Length > 1 ? argParts[1] : ""));
            }

            
            if (!ValidConfig) return;

            // Use a slight delay before continuing to ensure pipes are created by the myrtille.services
            Thread.Sleep(600);

            
            pipeMessaging = new PipeMessaging(RemoteSessionID);

            // Connect to myrtille input and update pipes
            if (pipeMessaging.CreateCommunicationPipes())
            {
                pipeMessaging.OnMessageReceivedEvent += PipeMessaging_OnMessageReceivedEvent;
                try
                {
                    // Read input from pipes
                    pipeMessaging.ReadInputPipes();
                }
                catch (Exception ex)
                {
                    if(ConsoleOutput)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                finally
                {
                    DisconnectSSHClient();
                }
            }
        }

        /// <summary>
        /// Handle messages from input pipe
        /// </summary>
        /// <param name="command"></param>
        /// <param name="data"></param>
        private static void PipeMessaging_OnMessageReceivedEvent(string command, string data)
        {
            switch((RemoteSessionCommand)RemoteSessionCommandMapping.FromPrefix[command])
            {
                case RemoteSessionCommand.RequestFullscreenUpdate:
                    WriteOutput(command, data);
                    CheckSSHClientState();
                    break;
                case RemoteSessionCommand.SendUserName:
                    WriteOutput(command, data);
                    UserName = data;
                    break;
                case RemoteSessionCommand.SendServerAddress:
                    WriteOutput(command, data);
                    ServerAddress = data;
                    break;
                case RemoteSessionCommand.SendUserPassword:
                    WriteOutput(command, "Credentials received, connecting to remote host");
                    ConnectSSHClient(data);
                    break;
                case RemoteSessionCommand.CloseRdpClient:
                    WriteOutput(command, "Disconnect from remote host");
                    pipeMessaging.ClosePipes();
                    break;
                case RemoteSessionCommand.SendKeyScancode:
                    WriteOutput(command, data);
                    HandleKeyboardInput(data,true);
                    break;
                case RemoteSessionCommand.SendKeyUnicode:
                    WriteOutput(command, data);
                    HandleKeyboardInput(data,false);
                    break;
            }
        }

        #region ssh client
        /// <summary>
        /// Send data to ssh client
        /// </summary>
        /// <param name="byteData"></param>
        private static void SendSSHClientData(byte[] byteData)
        {
            try
            {
                shellStream.Write(byteData, 0, byteData.Length);
                shellStream.Flush();

            }
            catch (Exception e)
            {
                pipeMessaging.ClosePipes();
                DisconnectSSHClient();
            }
        }

        /// <summary>
        /// Send data to ssh client
        /// </summary>
        /// <param name="data"></param>
        private static void SendSSHClientData(byte byteData)
        {
            try
            {
                shellStream.WriteByte(byteData);
                shellStream.Flush();
                
            }
            catch(Exception e)
            {
                pipeMessaging.ClosePipes();
                DisconnectSSHClient();
            }
        }

        /// <summary>
        /// Disconnect SSH client from Host
        /// </summary>
        private static void DisconnectSSHClient()
        {
            try
            {
                if (client?.IsConnected ?? false)
                {
                    client.Disconnect();
                }
            }
            catch(Exception e)
            {

            }
            finally
            {
                client?.Dispose();
                client = null;
            }
        }

        /// <summary>
        /// Connect SSH client to remote host
        /// </summary>
        /// <param name="password"></param>
        private static void ConnectSSHClient(string password)
        {
            try
            {
                var connectionInfo = new ConnectionInfo(ServerAddress, UserName, new PasswordAuthenticationMethod(UserName, password));
                connectionInfo.Encoding = Encoding.Unicode;

                client = new SshClient(connectionInfo);
                client.Connect();
                
                shellStream = client.CreateShellStream("xterm", Columns, Rows, Width, Height, 1024);
                shellStream.DataReceived += ShellStream_DataReceived;
            }catch(Exception e)
            {
                if (ConsoleOutput)
                    Console.WriteLine(e.Message);
                //TODO: logging here
            }
            finally
            {
                if(!client.IsConnected)
                {
                    pipeMessaging.ClosePipes();
                    client.Dispose();
                }
            }
        }

        /// <summary>
        /// Receive data from SSH client and send to update pipe for processing my web client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ShellStream_DataReceived(object sender, Renci.SshNet.Common.ShellDataEventArgs e)
        {
            pipeMessaging.SendUpdatePipeMessage(e.Data);
        }

        /// <summary>
        /// Check if ssh client is still connected, if not close pipes and exit program
        /// </summary>
        private static void CheckSSHClientState()
        {
            try
            {
                pipeMessaging.LastFullUpdate = DateTime.Now;
                if(!client.IsConnected)
                {
                    pipeMessaging.ClosePipes();

                }
            }catch(Exception e)
            {

            }
        }
        #endregion

        #region handle keyboard data
        /// <summary>
        /// Handle received keyboard input and send to ssh client
        /// </summary>
        /// <param name="keyCode"></param>
        private static void HandleKeyboardInput(string keyCode, bool isScanCode)
        {
            var keyMapping = keyCode.Split('-');
            if (keyMapping[1].Equals("0"))
            {
                int intKeyCode = int.Parse(keyMapping[0]);
                if (isScanCode && KeyboardIgnoreKeys.IgnoreKeys.Contains(intKeyCode))
                    return;

                if (intKeyCode > 1000 && intKeyCode < 2000)
                {
                    SendSSHClientData(new byte[] { 27, 91, (byte)(intKeyCode - 1000) });
                }
                else
                {
                    SendSSHClientData((byte)intKeyCode);
                }
            }
        }

        /// <summary>
        /// Write output to console window if required
        /// </summary>
        /// <param name="command"></param>
        /// <param name="data"></param>
        private static void WriteOutput(string command, string data)
        {
            if (ConsoleOutput) Console.WriteLine("CMD: {0}, DATA: {1}", command, data);
        }
        #endregion

        #region configuration
        private static string UserName { get; set; } // Username use to create SSH connection
        private static string ServerAddress { get; set; } //Host to establish SSH connection with
        private static string RemoteSessionID { get; set; } //Myrtille session ID used for pipe messaging
        private static bool LoggingEnabled { get; set; } //Myrtille logging parameter
        private static bool ConsoleOutput { get; set; } //Output comms to console window
        private static uint Height { get; set; } //Height of ssh termainal
        private static uint Width { get; set; } //Width of SSH terminal
        private static uint Columns { get { return (Width / 10); }} //Number of columns within the terminal window
        private static uint Rows { get{return (Height / 17);}} //Number of rows within the terminal window

        /// <summary>
        /// Indicate all command line parameters for correct operation have been received.
        /// </summary>
        private static bool ValidConfig
        {
            get
            {
                if (string.IsNullOrEmpty(RemoteSessionID)) return false;
                if (Height == 0) return false;
                if (Width == 0) return false;
                return true;
            }
        }

        /// <summary>
        /// Parse command line arguments
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="value"></param>
        private static void parseCommandLineArg(string arg, string value)
        {
            switch(arg)
            {
                case "/myrtille-sid":
                    RemoteSessionID = value.Trim();
                    break;
                case "/myrtille-window":
                    ConsoleOutput = true;
                    break;
                case "/myrtille-log":
                    LoggingEnabled = true;
                    break;
                case "/h":
                    Height = uint.Parse(value);
                    break;
                case "/w":
                    Width = uint.Parse(value);
                    break;
            }
        }
        #endregion
    }
}
