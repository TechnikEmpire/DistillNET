/*
 * Copyright © 2017 Jesse Nicholson
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;

namespace DistillNET
{
    /// <summary>
    /// The HtmlFilter is a class that represents a CSS selector that can be removed or whitelisted
    /// in source HTML.
    /// </summary>
    public class HtmlFilter : Filter
    {
        /// <summary>
        /// Gets a list of all referers that this HTML filter rule applies to. In the event that this
        /// array is empty, the referer field on requests will not be checked.
        /// </summary>
        public List<string> ApplicableReferers
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a list of all referers that this HTML filter rule applies to. In the event that this
        /// array is empty, the referer field on requests will not be checked.
        /// </summary>
        public List<string> ExceptReferers
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a list of all domains that this HTML filter rule applies to. In the event that this
        /// array is empty, the rule applies globally, to all domains.
        /// </summary>
        public List<string> ApplicableDomains
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a list of all domains that this HTML filter should not be applied to. In the event
        /// that this array is empty, the rule applies either globally, or exclusively to the list of
        /// applicable domains, if that property is not empty.
        /// </summary>
        public List<string> ExceptionDomains
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the raw CSS selector rule string.
        /// </summary>
        public string CssSelector
        {
            get;
            private set;
        } = string.Empty;

        /// <summary>
        /// Constructs a new CSS selector
        /// </summary>
        /// <param name="originalRule">
        /// The original rule string.
        /// </param>
        /// <param name="applicableDomains">
        /// All domains that this rule is to apply to. Can be null or empty, either if which implies
        /// that this rule applies globally.
        /// </param>
        /// <param name="cssSelector">
        /// The raw CSS selector string. Cannot be null, whitespace or empty under certain
        /// conditional compilation flags. See exception reference for more details.
        /// </param>
        /// <param name="isException">
        /// Whether or not this CSS selector rule is an exception. Exception rules protect matching
        /// content from being filtered out.
        /// </param>
        /// <param name="categoryId">
        /// The category ID to assign to this rule.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If compiled with TE_FILTERING_VERIFY_RULE_DATA, the cssSelector parameter will undergo
        /// checks to ensure that it is not null, empty or whitespace. If these checks are performed
        /// and any of those conditions is true, the constructor will throw this exception.
        /// </exception>
        internal HtmlFilter(string originalRule, List<string> applicableDomains, string cssSelector, bool isException, short categoryId) : base(originalRule, isException, categoryId)
        {
            if (applicableDomains != null && applicableDomains.Count > 0)
            {
                ApplicableDomains = applicableDomains;
            }

#if TE_FILTERING_VERIFY_RULE_DATA
            Debug.Assert(!string.IsNullOrEmpty(cssSelector) && !string.IsNullOrWhiteSpace(cssSelector), "Css selector cannot be null, empty or whitespace.");
            if(string.IsNullOrEmpty(cssSelector) || string.IsNullOrWhiteSpace(cssSelector))
            {
                throw new ArgumentException("Css selector cannot be null, empty or whitespace.", nameof(cssSelector));
            }
#endif

            CssSelector = cssSelector;
        }

        /// <summary>
        /// Removes verbose data from the constructed instance.
        /// </summary>
        public override void TrimExcessData()
        {
            ApplicableDomains.Clear();
            ExceptionDomains.Clear();

            OriginalRule = string.Empty;
        }
    }
}