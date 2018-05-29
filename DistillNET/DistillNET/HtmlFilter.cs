/*
 * Copyright © 2017 Jesse Nicholson
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Diagnostics;

namespace DistillNET
{
    public class HtmlFilter : Filter
    {
        /// <summary>
        /// Gets an array of all domains that this CSS selector rule applies to. In the event that
        /// this array is empty, the rule applies globally, to all domains.
        /// </summary>
        public string[] ApplicableDomains
        {
            get;
            private set;
        } = new string[0];

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
        /// <exception cref="ArgumentException">
        /// If compiled with TE_FILTERING_VERIFY_RULE_DATA, the cssSelector parameter will undergo
        /// checks to ensure that it is not null, empty or whitespace. If these checks are performed
        /// and any of those conditions is true, the constructor will throw this exception.
        /// </exception>
        internal HtmlFilter(string originalRule, string[] applicableDomains, string cssSelector, bool isException, short categoryId) : base(originalRule, isException, categoryId)
        {
            if(applicableDomains != null && applicableDomains.Length > 0)
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

        public override void TrimExcessData()
        {
            if(this.ApplicableDomains != null)
            {
                Array.Clear(this.ApplicableDomains, 0, this.ApplicableDomains.Length);
                this.ApplicableDomains = new string[0];
            }

            OriginalRule = string.Empty;
        }
    }
}