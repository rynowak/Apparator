// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Razor
{
    public sealed class CachingCompilationTagHelperFeature : RazorEngineFeatureBase, ITagHelperFeature
    {
        private ITagHelperDescriptorProvider[] _providers;
        private IMetadataReferenceFeature _referenceFeature;

        private object _lock;
        private bool _initialized;
        private IReadOnlyList<TagHelperDescriptor> _cache;

        public IReadOnlyList<TagHelperDescriptor> GetDescriptors()
        {
            return LazyInitializer.EnsureInitialized(ref _cache, ref _initialized, ref _lock, GetDescriptorsCore);
        }

        private IReadOnlyList<TagHelperDescriptor> GetDescriptorsCore()
        {
            var results = new List<TagHelperDescriptor>();

            var context = TagHelperDescriptorProviderContext.Create(results);
            var compilation = CSharpCompilation.Create("__TagHelpers", references: _referenceFeature.References);
            context.SetCompilation(compilation);

            for (var i = 0; i < _providers.Length; i++)
            {
                _providers[i].Execute(context);
            }

            return results;
        }

        protected override void OnInitialized()
        {
            _referenceFeature = Engine.Features.OfType<IMetadataReferenceFeature>().FirstOrDefault();
            _providers = Engine.Features.OfType<ITagHelperDescriptorProvider>().OrderBy(f => f.Order).ToArray();
        }
    }
}