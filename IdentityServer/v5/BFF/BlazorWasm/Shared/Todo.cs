// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Blazor.Shared
{
    public class ToDo
    {
        static int _nextId = 1;
        public static int NewId()
        {
            return _nextId++;
        }

        public int Id { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Name { get; set; }
        public string User { get; set; }
    }
}
