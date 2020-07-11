// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace egregore.Models
{
    public sealed class SignInViewModel
    {
        [Required, ReadOnly(true)]
        public string Challenge { get; set; }

        [Required]
        public string PublicKey { get; set; }

        [Required]
        public string Signature { get; set; }

        [ReadOnly(true)]
        public string ServerId { get; set; }
    }
}