﻿// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace egregore.Logging.LightningDb
{
    public sealed class LoggingException
    {
        public LoggingException(Exception exception)
        {
            Message = exception.Message;
            StackTrace = exception.StackTrace;
            HelpLink = exception.HelpLink;
            Source = exception.Source;
        }

        public LoggingException(LoggingDeserializeContext context)
        {
            Message = context.br.ReadNullableString();
            StackTrace = context.br.ReadNullableString();
            HelpLink = context.br.ReadNullableString();
            Source = context.br.ReadNullableString();
        }

        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string HelpLink { get; set; }
        public string Source { get; set; }

        public void Serialize(LoggingSerializeContext context)
        {
            context.bw.WriteNullableString(Message);
            context.bw.WriteNullableString(StackTrace);
            context.bw.WriteNullableString(HelpLink);
            context.bw.WriteNullableString(Source);
        }
    }
}