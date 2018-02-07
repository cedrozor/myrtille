/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2017 Cedric Coste/Paul Oliver

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
using System.Threading.Tasks;

namespace Myrtille.Common.Interfaces
{
    public interface IMultifactorAuthenticationAdapter
    {
        /// <summary>
        /// This is the label to be displayed on the login screen
        /// </summary>
        string PromptLabel { get; }

        /// <summary>
        /// Validate credentials using the adapter
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        bool Authenticate(string username, string password, string clientIP = null);

        /// <summary>
        /// link to MFA provider page
        /// </summary>
        string ProviderURL { get; }
    }
}
