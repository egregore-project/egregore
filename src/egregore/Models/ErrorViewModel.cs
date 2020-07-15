// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace egregore.Models
{
    public sealed class ErrorViewModel
    {
        public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);
        
        public string RequestId { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}