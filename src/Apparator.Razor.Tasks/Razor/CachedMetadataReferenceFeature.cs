// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class CachedMetadataReferenceFeature : IMetadataReferenceFeature
    {
        private readonly MetadataReference[] _references;

        public CachedMetadataReferenceFeature(IEnumerable<string> references)
        {
            _references = references.Select(r => MetadataReference.CreateFromFile(r)).ToArray();
        }

        /// <remarks>
        /// Invoking <see cref="RazorReferenceManager.CompilationReferences"/> ensures that compilation
        /// references are lazily evaluated.
        /// </remarks>
        public IReadOnlyList<MetadataReference> References => _references;

        public RazorEngine Engine { get; set; }
    }
}