// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace egregore.Ontology
{
    public sealed class ProgramKeyFileService : IKeyFileService
    {
        public string GetKeyFilePath() => Program.keyFilePath;
        public FileStream GetKeyFileStream() => Program.keyFileStream;
    }
}