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
using System.IO;
using System.Web;
using System.Web.UI;

namespace Myrtille.Web
{
    public partial class GetCursor : Page
    {
        /// <summary>
        /// retrieve the mouse cursor from the rdp session and send it to the browser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(
            object sender,
            EventArgs e)
        {
            // if cookies are enabled, the http session id is added to the http request headers; otherwise, it's added to the http request url
            // in both cases, the given http session is automatically bound to the current http context

            RemoteSession remoteSession = null;

            try
            {
                if (HttpContext.Current.Session[HttpSessionStateVariables.RemoteSession.ToString()] == null)
                    throw new NullReferenceException();

                // retrieve the remote session for the current http session
                remoteSession = (RemoteSession)HttpContext.Current.Session[HttpSessionStateVariables.RemoteSession.ToString()];
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to retrieve the remote session for the http session {0}, ({1})", HttpContext.Current.Session.SessionID, exc);
                return;
            }

            try
            {
                // retrieve params
                var imgIdx = int.Parse(HttpContext.Current.Request.QueryString["imgIdx"]);

                // retrieve image data
                var img = remoteSession.Manager.GetCachedUpdate(imgIdx);
                var imgData = img != null ? img.Data : null;
                if (imgData != null && imgData.Length > 0)
                {
                    CreateCursorFromImage(img.Width, img.Height, img.PosX, img.PosY, imgData, Response.OutputStream);
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to get mouse cursor, remote session {0} ({1})", remoteSession.Id, exc);
            }
        }

        // create a cursor (CUR) from an image (BMP or PNG); https://en.wikipedia.org/wiki/ICO_%28file_format%29
        private static void CreateCursorFromImage(int width, int height, int xHotSpot, int yHotSpot, byte[] data, Stream output)
        {
            try
            {
                var writer = new BinaryWriter(output);

                // 0-1 Reserved. Must always be 0.
                writer.Write((byte)0);
                writer.Write((byte)0);

                // 2-3 Specifies image type: 1 for icon (.ICO) image, 2 for cursor (.CUR) image. Other values are invalid.
                writer.Write((short)2);

                // 4-5 Specifies number of images in the file.
                writer.Write((short)1);

                // image #1

                // 0 Specifies image width in pixels. Can be any number between 0 and 255. Value 0 means image width is 256 pixels.
                writer.Write((byte)width);

                // 1 Specifies image height in pixels. Can be any number between 0 and 255. Value 0 means image height is 256 pixels.
                writer.Write((byte)height);

                // 2 Specifies number of colors in the color palette. Should be 0 if the image does not use a color palette.
                writer.Write((byte)0);

                // 3 Reserved. Should be 0.
                writer.Write((byte)0);

                // 4-5 In ICO format: Specifies color planes.Should be 0 or 1.
                // 4-5 In CUR format: Specifies the horizontal coordinates of the hotspot in number of pixels from the left.
                writer.Write((short)xHotSpot);

                // 6-7 In ICO format: Specifies bits per pixel.
                // 6-7 In CUR format: Specifies the vertical coordinates of the hotspot in number of pixels from the top.
                writer.Write((short)yHotSpot);

                // 8-11 Specifies the size of the image's data in bytes
                writer.Write((int)data.Length);

                // 12-15 Specifies the offset of BMP or PNG data from the beginning of the ICO/CUR file
                writer.Write((int)(6 + 16));

                // image data
                writer.Write(data);

                writer.Flush();
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to convert image to cursor with exception {0}", exc);
                throw;
            }
        }
    }
}