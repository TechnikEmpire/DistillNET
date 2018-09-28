/*
 * Copyright © 2018 Jesse Nicholson
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace DistillNET.Extensions
{
    internal static class MemoryExtensions
    {
        /// <summary>
        /// Determines if this ReadOnlyMemory begins with the same character sequence as the supplied string.
        /// </summary>
        /// <param name="str">
        /// This string.
        /// </param>
        /// <param name="other">
        /// The ReadOnlyMemory to compare the beginning of this
        /// </param>
        /// <returns>
        /// </returns>
        /// <remarks>
        /// This method is used rather than the built in .net equivalent for the sake of speed. The
        /// built in method invokes various internationalization methods inside the .net framework
        /// that simply are not necessary for our purposes.
        /// </remarks>
        public static bool StartsWithQuick(this ReadOnlyMemory<char> str, ReadOnlyMemory<char> other)
        {
            return str.Span.StartsWithQuick(other.Span);
        }

        /// <summary>
        /// Gets the next position of a character that would indicate the end of an address or domain
        /// anchor string, if any.
        /// </summary>
        /// <param name="str">
        /// This string.
        /// </param>
        /// <param name="startIndex">
        /// The index to begin searching at.
        /// </param>
        /// <returns>
        /// A positive value indicating the index of a matched character in the event that one is
        /// found, a negative number in the event that no such character is found.
        /// </returns>
        public static int IndexOfAnchorEnd(this ReadOnlyMemory<char> str, int startIndex = 0)
        {
            return str.Span.IndexOfAnchorEnd(startIndex);
        }

        public static int IndexOfQuick(this ReadOnlyMemory<char> str, char what, int startIndex = 0)
        {
            return str.Span.IndexOfQuick(what, startIndex);
        }

        public static int IndexOfQuickICase(this ReadOnlyMemory<char> str, char what, int startIndex = 0)
        {
            return str.Span.IndexOfQuickICase(what, startIndex);
        }

        public static int IndexOfQuick(this ReadOnlyMemory<char> str, ReadOnlyMemory<char> what, int startIndex = 0)
        {
            return str.Span.IndexOfQuick(what.Span, startIndex);
        }

        public static int IndexOfQuickICase(this ReadOnlyMemory<char> str, ReadOnlyMemory<char> what, int startIndex = 0)
        {
            return str.Span.IndexOfQuickICase(what.Span, startIndex);
        }

        public static bool EqualsAt(this ReadOnlyMemory<char> str, ReadOnlyMemory<char> what, int index)
        {
            return str.Span.EqualsAt(what.Span, index);
        }

        public static bool EqualsAtICase(this ReadOnlyMemory<char> str, ReadOnlyMemory<char> what, int index)
        {
            return str.Span.EqualsAtICase(what.Span, index);
        }

        public static ReadOnlyMemory<char> TrimQuick(this ReadOnlyMemory<char> str)
        {
            bool trimming = true;
            while (trimming && str.Length > 0)
            {
                if (char.IsWhiteSpace(str.Span[0]))
                {
                    str = str.Slice(1);
                    continue;
                }

                if (char.IsWhiteSpace(str.Span[str.Length - 1]))
                {
                    str = str.Slice(0, str.Length - 1);
                    continue;
                }

                trimming = false;
            }
            return str;
        }

        public static int LastIndexOfQuick(this ReadOnlyMemory<char> str, ReadOnlyMemory<char> what)
        {
            return str.Span.LastIndexOfQuick(what.Span);
        }

        public static int LastIndexOfQuickICase(this ReadOnlyMemory<char> str, ReadOnlyMemory<char> what)
        {
            return str.Span.LastIndexOfQuickICase(what.Span);
        }

        public static List<ReadOnlyMemory<char>> Split(this ReadOnlyMemory<char> str, ReadOnlyMemory<char> what)
        {
            var retVal = new List<ReadOnlyMemory<char>>();

            var firstIndex = str.IndexOfQuick(what, 0);

            if (firstIndex > -1)
            {
                do
                {
                    retVal.Add(str.Slice(0, firstIndex));
                    str = str.Slice(firstIndex + what.Length);
                    firstIndex = str.IndexOfQuick(what, 0);
                } while (firstIndex > -1);

                if (str.Length > 0)
                {
                    retVal.Add(str);
                }
            }
            else
            {
                retVal.Add(str);
            }

            return retVal;
        }
    }
}
