﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Apparator.Razor.TagHelpers
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            DebugMode.HandleDebugSwitch(ref args);

            var application = new TagHelperApplication();
            return application.Execute(args);
        }
    }
}
