using System.Collections;

/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2018 Olive Innovations Ltd

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

namespace Myrtille.Web
{


    public static class JSKeyCodeToSSHBashShellUniCodeMapping
    {
        public static Hashtable MapTable { get; private set; }

        static JSKeyCodeToSSHBashShellUniCodeMapping()
        {

            MapTable = new Hashtable();

            MapTable.Add(163, 156);           // £

            MapTable.Add(172, 170);           // ¬

        }
    }
}