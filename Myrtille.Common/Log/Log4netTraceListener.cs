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

using System.Diagnostics;
using log4net;

namespace Myrtille.Log
{
    // this class abstracts the .net default logging mechanism by redirecting all System.Diagnostics traces to log4net
    // if needed, it can be done for any other log provider

    public class Log4netTraceListener : TraceListener
    {
        private readonly ILog _log;

        public Log4netTraceListener()
        {
            _log = LogManager.GetLogger("System.Diagnostics redirection");
        }

        public Log4netTraceListener(ILog log)
        {
            _log = log;
        }

        public override void Write(string message)
        {
            // trace source (process name); if needed
        }

        public override void WriteLine(string message)
        {
            if (_log != null && Filter != null)
            {
                switch (((Log4netTraceFilter)Filter).EventType)
                {
                    case TraceEventType.Information:
                        _log.Info(message);
                        break;

                    case TraceEventType.Warning:
                        _log.Warn(message);
                        break;

                    case TraceEventType.Error:
                        _log.Error(message);
                        break;
                }
            }
        }
    }
}