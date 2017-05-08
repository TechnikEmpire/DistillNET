/*
 * Copyright © 2017 Jesse Nicholson
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace DistillNET
{
    /// <summary>
    /// Base class for all HTTP related content filter classes.
    /// </summary>
    public abstract class Filter
    {
        /// <summary>
        /// Gets whether or not this rule is an exception rule. Exception rules protect matching
        /// content from being filtered out.
        /// </summary>
        public bool IsException
        {
            get;
            protected set;
        } = false;

        /// <summary>
        /// Gets the original rule string.
        /// </summary>
        public string OriginalRule
        {
            get;
            protected set;
        } = string.Empty;

        /// <summary>
        /// Gets the category ID to which the filter belongs.
        /// </summary>
        public short CategoryId
        {
            get;
            protected set;
        } = 1;

        /// <summary>
        /// Constructs a new instance of a content filter class.
        /// </summary>
        /// <param name="originalRule">
        /// The original rule string. Handy for debugging.
        /// </param>
        /// <param name="isException">
        /// Indicates whether or not this rule is an exception rule. Exception rules protect matching
        /// content from being filtered out.
        /// </param>
        /// <param name="categoryId">
        /// The category ID to which this filter belongs.
        /// </param>
        internal Filter(string originalRule, bool isException, short categoryId)
        {
            OriginalRule = originalRule;
            IsException = isException;
            CategoryId = categoryId;
        }

        /// <summary>
        /// This function should release all resources that are not essential to any ongoing internal
        /// function of a filter class. For example, once all filters have been sorted and stored
        /// based on their applicable domains, it's no longer necessary for loaded rules to keep an
        /// independent copy of the domains that they apply to. This would be managed externally, so
        /// no need to keep around in memory. Note however that filter classes should generally not
        /// be serialized after this method has been called. This would cause data loss, obviously.
        ///
        /// The sole purpose of this function to release resources so many tens of thousands of rules
        /// can been kept in memory without unecessarily holding extra memory.
        /// </summary>
        public abstract void TrimExcessData();
    }
}