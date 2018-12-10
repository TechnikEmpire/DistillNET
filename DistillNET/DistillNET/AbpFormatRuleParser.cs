/*
 * Copyright © 2017 Jesse Nicholson
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

#define USE_REFERER

using DistillNET.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DistillNET
{
    /// <summary>
    /// </summary>
    public class AbpFormatRuleParser
    {
        /// <summary>
        /// Delimiters used for splitting URL filtering options.
        /// </summary>
        private static readonly char[] s_optionsDelim = new[] { ',' };

        /// <summary>
        /// Delimiters used for splitting domains specified in the URL filtering options "domains".
        /// </summary>
        private static readonly char[] s_domainsDelim = new[] { '|' };

        internal class OptionsDictComparer : IEqualityComparer<string>
        {
            bool IEqualityComparer<string>.Equals(string x, string y)
            {
                if(x.Length != y.Length)
                {
                    return false;
                }

                if(x[0] != y[0])
                {
                    return false;
                }

                var lastInd = x.Length - 1;
                if(x[lastInd] != y[lastInd])
                {
                    return false;
                }

                for(int i = 1; i < lastInd; ++i)
                {
                    if(x[i] != y[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            int IEqualityComparer<string>.GetHashCode(string obj)
            {
                return obj.GetHashCode();
            }
        }

        /// <summary>
        /// Map of all possible option strings to their proper enum values.
        /// </summary>
        private static Dictionary<string, UrlFilter.UrlFilterOptions> s_optionsMap = new Dictionary<string, UrlFilter.UrlFilterOptions>(new OptionsDictComparer())
        {
            { "script", UrlFilter.UrlFilterOptions.Script },
            { "~script", UrlFilter.UrlFilterOptions.ExceptScript },
            { "image", UrlFilter.UrlFilterOptions.Image },
            { "~image", UrlFilter.UrlFilterOptions.ExceptImage },
            { "stylesheet", UrlFilter.UrlFilterOptions.StyleSheet },
            { "~stylesheet", UrlFilter.UrlFilterOptions.ExceptStyleSheet },
            { "object", UrlFilter.UrlFilterOptions.Object },
            { "~object", UrlFilter.UrlFilterOptions.ExceptObject },
            { "popup", UrlFilter.UrlFilterOptions.PopUp },
            { "~popup", UrlFilter.UrlFilterOptions.ExceptPopUp },
            { "third-party", UrlFilter.UrlFilterOptions.ThirdParty },
            { "~third-party", UrlFilter.UrlFilterOptions.ExceptThirdParty },
            { "xmlhttprequest", UrlFilter.UrlFilterOptions.XmlHttpRequest },
            { "~xmlhttprequest", UrlFilter.UrlFilterOptions.ExceptXmlHttpRequest },
            { "websocket", UrlFilter.UrlFilterOptions.Websocket },
            { "object-subrequest", UrlFilter.UrlFilterOptions.ObjectSubrequest },
            { "~object-subrequest", UrlFilter.UrlFilterOptions.ExceptObjectSubrequest },
            { "subdocument", UrlFilter.UrlFilterOptions.Subdocument },
            { "~subdocument", UrlFilter.UrlFilterOptions.ExceptSubdocument },
            { "document", UrlFilter.UrlFilterOptions.Document },
            { "~document", UrlFilter.UrlFilterOptions.ExceptDocument },
            { "elemhide", UrlFilter.UrlFilterOptions.ElemHide },
            { "~elemhide", UrlFilter.UrlFilterOptions.ExceptElemHide },
            { "other", UrlFilter.UrlFilterOptions.Other },
            { "~other", UrlFilter.UrlFilterOptions.ExceptOther },
            #pragma warning disable CS0618 // Type or member is obsolete
            { "media", UrlFilter.UrlFilterOptions.Media },
            { "~media", UrlFilter.UrlFilterOptions.ExceptMedia },
            { "font", UrlFilter.UrlFilterOptions.Font },
            { "~font", UrlFilter.UrlFilterOptions.ExceptFont },
            { "matchcase", UrlFilter.UrlFilterOptions.MatchCase },
            { "collapse", UrlFilter.UrlFilterOptions.Collapse },
            { "~collapse", UrlFilter.UrlFilterOptions.ExceptCollapse },
            { "donottrack", UrlFilter.UrlFilterOptions.DoNotTrack },
            { "generichide", UrlFilter.UrlFilterOptions.GenericHide },
            { "genericblock", UrlFilter.UrlFilterOptions.GenericBlock },
            { "ping", UrlFilter.UrlFilterOptions.Ping }
            #pragma warning restore CS0618 // Type or member is obsolete
        };

        /// <summary>
        /// These characters are used to determine where the end of an anchored address or domain is,
        /// so we can split the request route away and such.
        /// </summary>
        private static readonly char[] s_anchoredEndIndicators = new[] { '/', ':', '?', '=', '&', '*', '^' };

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="rule">
        /// The rule string.
        /// </param>
        /// <param name="categoryId">
        /// The category ID to assign to this rule.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The parser operates expecting, when it encounters key characters in the rule format, a
        /// certain number of characters to follow these keys rule characters. The parser will plough
        /// full steam ahead without doing string boundary checks. It will catch out of range
        /// exceptions in these cases however, and then rethrow the exception as an
        /// ArgumentException, because this implies that the supplied rule is malformed. The parser
        /// does not attempt any other error handling.
        /// </exception>
        public Filter ParseAbpFormattedRule(string rule, short categoryId)
        {
            // If we find a '$' character in this rule, and this rules turns out not to be a CSS
            // selector, then this is where our rule options start. However, CSS selectors can and
            // will have this character present, and in this context this character has a whole
            // different meaning specific to CSS selectors.
            var optionsStart = rule.LastIndexOfQuick("$");
            var hasOptions = optionsStart != -1;

            // We'll assume that this is a URL filtering with this initial isException value,
            // however, we'll need to revise this assumption if the rule turns out to the be a CSS
            // selector rule. In the case of a CSS selector rule, exceptions are marked by placing an
            // @ symbol in between the two leading CSS selector pound, aka hashtag, characters.
            var isException = rule.StartsWithQuick("@@");

            // Give the above explanation of the CSS selector rule formats, and how there's a
            // significant difference between exception and standard rules, we need to check for both
            // standard and exception rules here if we come up negative on the first check (standard
            // rule).
            var cssSelectorStart = rule.LastIndexOfQuick("##");
            var isCssSelector = cssSelectorStart != -1;
            if(!isCssSelector)
            {
                cssSelectorStart = rule.LastIndexOfQuick("#@");
                if(cssSelectorStart != -1)
                {
                    isCssSelector = true;
                    isException = true;
                }
            }

            // Once we've concretely decided that this rule is in fact a CSS selector rule, we need
            // to reevaluate our assumption about whether or not the rule is an exception rule. Up
            // until this point, this was determined under the assumption that the rule is a URL
            // filtering rule. We search here for the telltale characters unique to a CSS selector
            // exception rule.
            if(isCssSelector)
            {
                try
                {
                    return ParseCssSelector(rule, cssSelectorStart, isException, categoryId);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new ArgumentException("Out of range exception while parsing CSS selector rule. Rule must be malformed.", nameof(rule));
                }
            }

            try
            {   
                return ParseUrlFilter(rule, optionsStart, hasOptions, isException, categoryId);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentException("Out of range exception while parsing filter rule. Rule must be malformed.", nameof(rule));
            }
        }

        /// <summary>
        /// Parses the supplied rule string as a CSS selector rule.
        /// </summary>
        /// <param name="rule">
        /// The raw rule string.
        /// </param>
        /// <param name="selectorStartOffset">
        /// The position where the CSS selector rule starts.
        /// </param>
        /// <param name="isException">
        /// Whether or not this rule is an exception rule. It is obviously assumed that this has been
        /// predetermined externally and this will not be determined internally.
        /// </param>
        /// <param name="categoryId">
        /// The category ID to assign to this rule.
        /// </param>
        /// <returns>
        /// A css selector filtering class instance built from the parsed rule.
        /// </returns>
        private Filter ParseCssSelector(string rule, int selectorStartOffset, bool isException, short categoryId)
        {
            var originalRuleCopy = rule;

            List<string> applicableDomains = null;

            if(selectorStartOffset > 0)
            {
                // Whenever the start of the selector rule is not the start of the string, this
                // indicates that it's a domain-specific CSS selector rule. As such, one or more
                // domains, separated by a comma character, will preceed the actual CSS selector
                // rule.
                applicableDomains = rule.Substring(0, selectorStartOffset).Split(s_optionsDelim, StringSplitOptions.None).ToList();
            }

            // If it's an exception, we need to cut off three characters from the start of the CSS
            // selector rule because of the extra '@' character. If not an exception, we need to cut
            // off only the first two '##' characters.
            rule = rule.Substring(isException ? selectorStartOffset + 3 : selectorStartOffset + 2);

            return new HtmlFilter(originalRuleCopy, applicableDomains, rule, isException, categoryId);
        }

        private Filter ParseUrlFilter(string rule, int optionsStartOffset, bool hasOptions, bool isException, short categoryId)
        {
            string originalRuleCopy = rule;

            string[] allOptions = null;
            List<string> applicableReferers = new List<string>();
            List<string> exceptReferers = new List<string>();

            List<string> applicableDomains = new List<string>();
            List<string> exceptionDomains = new List<string>();

            // Trim off the leading "@@" chracters if it's an exception.
            if(isException)
            {   
                rule = rule.Substring(2);
                // Adjust start offset.
                optionsStartOffset -= 2;
            }

            if(hasOptions)
            {
                // Split off our options, skipping the '$' char.
                allOptions = rule.Substring(optionsStartOffset + 1).Split(s_optionsDelim, StringSplitOptions.None);

                // Then strip the options off the end of our rule string.
                rule = rule.Substring(0, optionsStartOffset);
            }

            var enumOptions = UrlFilter.UrlFilterOptions.None;

            if(allOptions != null)
            {   
                string domainsOption = null;

                string refererOption = null;

                var allOptLen = allOptions.Length;
                for(int i = 0; i < allOptLen; ++i)
                {
                    if(allOptions[i].Length > 7 && allOptions[i][0] == 'd' && allOptions[i][6] == '=')
                    {
                        domainsOption = allOptions[i];
                        allOptions[i] = string.Empty;

                        if(refererOption != null)
                        {
                            // No sense in scanning further when everything we could possibly need
                            // has been captured out of this loop.
                            break;
                        }

                        continue;
                    }

#if USE_REFERER
                    if(allOptions[i].Length > 7 && allOptions[i][0] == 'r' && allOptions[i][7] == '=')
                    {
                        refererOption = allOptions[i];
                        allOptions[i] = string.Empty;

                        if(domainsOption != null)
                        {
                            // No sense in scanning further when everything we could possibly need
                            // has been captured out of this loop.
                            break;
                        }

                        continue;
                    }
#endif
                }

                if(domainsOption != null)
                {
                    // If we got a domains option, split it out of the main options collection, as
                    // this is the only type of option that's special aka has to parsed differently.
                    // Differentiate simply by string length for speed, but do ordinal compare when
                    // lengths are equal. 

                    // Trim off the "domains=" part, then split by the domains delimiter, which is a
                    // pipe.
                    domainsOption = domainsOption.Substring(7);
                    var rawDomains = domainsOption.Split(s_domainsDelim, StringSplitOptions.None);

                    // Get applicable and exception domains. Exception domains in the list start with tilde,
                    // applicable domains don't. Applicable here meaning that the rule should apply to such
                    // a domain.

                    var domainsLen = rawDomains.Length;
                    for(int i = 0; i < domainsLen; ++i)
                    {
                        switch(rawDomains[i][0])
                        {
                            case '~':
                            {
                                exceptionDomains.Add(rawDomains[i].Substring(1));
                            }
                            break;

                            default:
                            {
                                applicableDomains.Add(rawDomains[i]);
                            }
                            break;
                        }
                    }
                }

#if USE_REFERER
                if(refererOption != null)
                {   
                    // If we got a referers option, split it out of the main options collection, as
                    // this is the only type of option that's special aka has to parsed differently.
                    // Differentiate simply by string length for speed, but do ordinal compare when
                    // lengths are equal. 

                    // Trim off the "referer=" part, then split by the domains delimiter, which is a
                    // pipe.
                    refererOption = refererOption.Substring(8);
                    var rawReferers = refererOption.Split(s_domainsDelim, StringSplitOptions.None);

                    // Get applicable and exception referers. Exception referers in the list start with tilde,
                    // applicable referers don't. Applicable here meaning that the rule should apply to such
                    // a domain.

                    var referersLen = rawReferers.Length;
                    for(int i = 0; i < referersLen; ++i)
                    {
                        switch(rawReferers[i][0])
                        {
                            case '~':
                            {
                                exceptReferers.Add(rawReferers[i].Substring(1));
                            }
                            break;

                            default:
                            {
                                applicableReferers.Add(rawReferers[i]);
                            }
                            break;
                        }
                    }
                }

#endif


                // Parse out the rest of the options.
                foreach (var opt in allOptions)
                {
                    if (s_optionsMap.TryGetValue(opt, out UrlFilter.UrlFilterOptions asOpt))
                    {
                        enumOptions |= asOpt;
                        enumOptions &= ~UrlFilter.UrlFilterOptions.None;
                    }
                }
            }

            var compiledParts = new List<UrlFilter.UrlFilteringRuleFragment>();

            bool isAnchoredDomain = rule.StartsWithQuick("||");
            bool isAnchoredAddress = false;
            string anchoredDomain = string.Empty;
            string anchoredAddress = string.Empty;

            bool ruleIsGreater = rule.Length > 0;

            if(isAnchoredDomain)
            {
                rule = rule.Substring(2);

                if(rule.Length == 0)
                {
                    // Invalid rule.
                    return null;
                }

                var nextSpecial = rule.IndexOfAnchorEnd();
                if(nextSpecial != -1)
                {
                    anchoredDomain = rule.Substring(0, nextSpecial);
                    rule = rule.Substring(nextSpecial);
                }
                else
                {
                    anchoredDomain = rule;
                    rule = string.Empty;
                    ruleIsGreater = false;
                }

                applicableDomains.Add(anchoredDomain);

                compiledParts.Add(new UrlFilter.AnchoredDomainFragment(anchoredDomain));
            }
            else
            {
                // This may be an options-only rule...
                if(ruleIsGreater)
                {
                    isAnchoredAddress = rule[0] == '|';

                    if(isAnchoredAddress)
                    {
                        rule = rule.Substring(1);

                        var endOfAnchoredAddressIndex = rule.IndexOfQuick('|');

                        switch(endOfAnchoredAddressIndex)
                        {
                            case -1:
                            {
                                var nextSpecial = rule.IndexOfAnchorEnd();

                                switch(nextSpecial)
                                {
                                    case -1:
                                    {
                                        anchoredAddress = rule;
                                        rule = string.Empty;
                                    }
                                    break;

                                    default:
                                    {
                                        anchoredAddress = rule.Substring(0, nextSpecial);
                                        rule = rule.Substring(nextSpecial);
                                    }
                                    break;
                                }
                            }
                            break;

                            default:
                            {
                                anchoredAddress = rule.Substring(0, endOfAnchoredAddressIndex);
                                rule = rule.Substring(endOfAnchoredAddressIndex);

                                rule = rule.Substring(1);
                            }
                            break;
                        }

                        if (Uri.TryCreate(anchoredAddress, UriKind.Absolute, out Uri parsedUri))
                        {
                            applicableDomains.Add(parsedUri.Host);
                        }

                        compiledParts.Add(new UrlFilter.AnchoredAddressFragment(anchoredAddress, enumOptions.HasFlag(UrlFilter.UrlFilterOptions.MatchCase)));
                    }
                }
            }

            var allLen = rule.Length;
            var lastCol = 0;
            for(int i = 0; i < allLen; ++i)
            {
                switch(rule[i])
                {
                    case '*':
                    {
                        if(lastCol < i)
                        {
                            compiledParts.Add(new UrlFilter.StringLiteralFragment(rule.Substring(lastCol, i - lastCol), enumOptions.HasFlag(UrlFilter.UrlFilterOptions.MatchCase)));
                        }

                        compiledParts.Add(new UrlFilter.WildcardFragment());

                        lastCol = i + 1;
                    }
                    break;

                    case '^':
                    {
                        if(lastCol < i)
                        {
                            compiledParts.Add(new UrlFilter.StringLiteralFragment(rule.Substring(lastCol, i - lastCol), enumOptions.HasFlag(UrlFilter.UrlFilterOptions.MatchCase)));
                        }

                        compiledParts.Add(new UrlFilter.SeparatorFragment());

                        lastCol = i + 1;
                    }
                    break;

                    default:
                    {
                        continue;
                    }
                }
            }

            if(lastCol < allLen)
            {
                compiledParts.Add(new UrlFilter.StringLiteralFragment(rule.Substring(lastCol), enumOptions.HasFlag(UrlFilter.UrlFilterOptions.MatchCase)));
            }

            return new UrlFilter(originalRuleCopy, compiledParts, enumOptions, applicableDomains, exceptionDomains, applicableReferers, exceptReferers, isException, categoryId);
        }
    }
}