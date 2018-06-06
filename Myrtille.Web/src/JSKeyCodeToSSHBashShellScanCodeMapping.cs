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


    public static class JSKeyCodeToSSHBashShellScanCodeMapping
    {
        public static Hashtable MapTable { get; private set; }

        static JSKeyCodeToSSHBashShellScanCodeMapping()
        {

            MapTable = new Hashtable();

            MapTable.Add(65, 1);           // ctl a
            MapTable.Add(66, 2);           // ctl b
            MapTable.Add(67, 3);           // ctl c
            MapTable.Add(68, 4);           // ctl d
            MapTable.Add(69, 5);           // ctl e
            MapTable.Add(70, 6);           // ctl f
            MapTable.Add(71, 7);           // ctl g
            MapTable.Add(72, 8);           // ctl h
            MapTable.Add(73, 9);           // ctl i
            MapTable.Add(74, 10);           // ctl j
            MapTable.Add(75, 11);           // ctl k
            MapTable.Add(76, 12);           // ctl l
            MapTable.Add(77, 13);           // ctl m
            MapTable.Add(78, 14);           // ctl n
            MapTable.Add(79, 15);           // ctl o
            MapTable.Add(80, 16);           // ctl p
            MapTable.Add(81, 17);           // ctl q
            MapTable.Add(82, 18);           // ctl r
            MapTable.Add(83, 19);           // ctl s
            MapTable.Add(84, 20);           // ctl t
            MapTable.Add(85, 21);           // ctl u
            MapTable.Add(86, 22);           // ctl v
            MapTable.Add(87, 23);           // ctl w
            MapTable.Add(88, 24);           // ctl x
            MapTable.Add(89, 25);           // ctl y
            MapTable.Add(90, 26);           // ctl z

            MapTable.Add(163, 156);           // £

            MapTable.Add(172, 170);           // ¬

            MapTable.Add(112, 59);           // F1
            MapTable.Add(113, 60);           // F2
            MapTable.Add(114, 61);           // F3
            MapTable.Add(115, 62);           // F4
            MapTable.Add(116, 63);           // F5
            MapTable.Add(117, 64);           // F6
            MapTable.Add(118, 65);           // F7
            MapTable.Add(119, 66);           // F8
            MapTable.Add(120, 67);           // F9
            MapTable.Add(121, 68);           // F10
            MapTable.Add(122, 85);           // F11
            MapTable.Add(123, 86);           // F12

            MapTable.Add(37, 1068);         // left arrow
            MapTable.Add(38, 1065);         // up arrow
            MapTable.Add(39, 1067);         // right arrow
            MapTable.Add(40, 1066);         // down arrow

        }
    }
}