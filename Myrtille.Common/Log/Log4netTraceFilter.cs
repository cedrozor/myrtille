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

namespace Myrtille.Log
{
    public class Log4netTraceFilter : TraceFilter
    {
        private readonly SourceLevels _traceLevel;
        public TraceEventType EventType { get; set; }

        public Log4netTraceFilter(SourceLevels level)
        {
            _traceLevel = level;
        }

        public override bool ShouldTrace(TraceEventCache cache, string source, TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data)
        {
            // set the filtered event type
            EventType = eventType;

            // System.Diagnostics.Trace only provides Information, Warning or Error messages; ignoring other event types
            var shouldTrace = false;
            switch (_traceLevel)
            {
                case SourceLevels.Information:
                    shouldTrace = eventType == TraceEventType.Information || eventType == TraceEventType.Warning || eventType == TraceEventType.Error;
                    break;

                case SourceLevels.Warning:
                    shouldTrace = eventType == TraceEventType.Warning || eventType == TraceEventType.Error;
                    break;

                case SourceLevels.Error:
                    shouldTrace = eventType == TraceEventType.Error;
                    break;
            }
            return shouldTrace;
        }
    }
}