/*
 * Copyright © 2018 Jesse Nicholson
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;
using System.Runtime.CompilerServices;

namespace DistillNET.Extensions
{
    internal static class CharExtensions
    {
        private static readonly char[] _lower;
        private static readonly char[] _upper;

        static CharExtensions()
        {
            _lower = new char[char.MaxValue];
            _upper = new char[char.MaxValue];
            Enumerable.Range(0, char.MaxValue).Select(x =>
            {
                _lower[x] = char.ToLowerInvariant((char)x);
                _upper[x] = char.ToUpperInvariant((char)x);
                return x;
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ToUpperFast(this char c)
        {
            return _upper[c];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ToLowerFast(this char c)
        {
            return _lower[c];
        }
    }
}