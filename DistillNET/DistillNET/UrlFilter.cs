/*
 * Copyright © 2017 Jesse Nicholson
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using DistillNET.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace DistillNET
{
    /// <summary>
    /// The UrlFilter class is responsible for matching against URIs using a format based on the
    /// Adblock Plus Filter format.
    /// </summary>
    public class UrlFilter : Filter
    {
        /// <summary>
        /// All possible filtering options, with the exception of the Domains option, because it is
        /// not a simple enum type.
        /// </summary>
        /// <remarks>
        /// See https://adblockplus.org/filters#options. 
        /// </remarks>
        [Flags]
        public enum UrlFilterOptions : long
        {
            /// <summary>
            /// No options specified. First entry so it will be default. 
            /// </summary>
            None = (1L << 0),

            /// <summary>
            /// The rule should apply to external scripts loaded via HTML tags. 
            /// </summary>
            Script = (1L << 1),

            /// <summary>
            /// Exception for external scripts loaded via HTML tags. 
            /// </summary>
            ExceptScript = (1L << 2),

            /// <summary>
            /// The rule should apply to images. 
            /// </summary>
            Image = (1L << 3),

            /// <summary>
            /// Exception for images. 
            /// </summary>
            ExceptImage = (1L << 4),

            /// <summary>
            /// The rule should apply to style sheets. 
            /// </summary>
            StyleSheet = (1L << 5),

            /// <summary>
            /// Exception for style sheets. 
            /// </summary>
            ExceptStyleSheet = (1L << 6),

            /// <summary>
            /// The rule should apply to objects, meaning requests originating from browser plugins.
            /// This isn't really possible to properly identify being outside of a browser simply
            /// analyzing requests.
            /// </summary>
            Object = (1L << 7),

            /// <summary>
            /// Exception for requests originating from browser plugins. 
            /// </summary>
            ExceptObject = (1L << 8),

            /// <summary>
            /// Applies to requests originating from popups. 
            /// </summary>
            PopUp = (1L << 9),

            /// <summary>
            /// Exception for requests originating from popups. 
            /// </summary>
            ExceptPopUp = (1L << 10),

            /// <summary>
            /// Applies to requests originating from a domain that is not the same as the domain that
            /// this current request points to. Basically, the referer field is not the same as the
            /// host field.
            /// </summary>
            ThirdParty = (1L << 11),

            /// <summary>
            /// Exception for requests originating from a domain that is not the same as the domain
            /// that this current request points to. Basically, the referer field is not the same as
            /// the host field.
            /// </summary>
            ExceptThirdParty = (1L << 12),

            /// <summary>
            /// The rule should apply to XML Http request. 
            /// </summary>
            XmlHttpRequest = (1L << 13),

            /// <summary>
            /// Exception for XML Http requests. 
            /// </summary>
            ExceptXmlHttpRequest = (1L << 14),

            /// <summary>
            /// The rule should apply to websocket requests. 
            /// </summary>
            Websocket = (1L << 15),

            /// <summary>
            /// The rule should apply to requests initiated by browser plugins. Not quite sure how
            /// this is supposed to be different from the Object option.
            /// </summary>
            ObjectSubrequest = (1L << 16),

            /// <summary>
            /// Exception for requests initiated by a browser plugin. 
            /// </summary>
            ExceptObjectSubrequest = (1L << 17),

            /// <summary>
            /// The rule should apply to embedded HTML documents. 
            /// </summary>
            Subdocument = (1L << 18),

            /// <summary>
            /// Exception for embedded HTML documents. 
            /// </summary>
            ExceptSubdocument = (1L << 19),

            /// <summary>
            /// The rule should apply to the current page itself. TLDR version of the documentation
            /// is that basically, filtering on the page matching this filter should be entirely disabled.
            /// </summary>
            Document = (1L << 20),

            /// <summary>
            /// Exception for the current page. Given the limited documentation, I'd guess that the
            /// presence of this option simply implies that filtering should be applied as normal,
            /// which is essentially the same thing as not applying this option at all.
            /// </summary>
            ExceptDocument = (1L << 21),

            /// <summary>
            /// The rule should apply to the current page itself. The limited documentation seems to
            /// imply that the presence of this rule means that no element hiding (aka CSS selector
            /// rules) should be applied to the current page at all, only exception CSS selectors.
            /// However, URL filters should be allowed to run.
            /// </summary>
            ElemHide = (1L << 22),

            /// <summary>
            /// Exception for the current page. Given the limited documentation, I'd guess that the
            /// presence of this rule simply means that element hiding rules should be applied as
            /// normal, which is essentially the same thing as not applying this option at all.
            /// </summary>
            ExceptElemHide = (1L << 23),

            /// <summary>
            /// The documentation simply says types of request not defined by other rules. Given that
            /// the above rules apply to virtually ever aspect of filtering content in both URLS and
            /// within HTTP payloads, and that this rule is not otherwise defined, I'll simply treat
            /// this rule as a "reserved" type that does nothing.
            /// </summary>
            Other = (1L << 24),

            /// <summary>
            /// Exception to nothing. See comments on Other option. 
            /// </summary>
            ExceptOther = (1L << 25),

            /// <summary>
            /// Not mentioned in main documentation page and not bothering to find it. This rule is ignored. 
            /// </summary>
            [Obsolete("This option is ignored.")]
            Media = (1L << 26),

            /// <summary>
            /// Not mentioned in main documentation page and not bothering to find it. This rule is ignored. 
            /// </summary>
            [Obsolete("This option is ignored.")]
            ExceptMedia = (1L << 27),

            /// <summary>
            /// Not mentioned in main documentation page and not bothering to find it. This rule is ignored. 
            /// </summary>
            [Obsolete("This option is ignored.")]
            Font = (1L << 28),

            /// <summary>
            /// Not mentioned in main documentation page and not bothering to find it. This rule is ignored. 
            /// </summary>
            [Obsolete("This option is ignored.")]
            ExceptFont = (1L << 29),

            /// <summary>
            /// The rule should be applied in a case-sensitive fashion. 
            /// </summary>
            MatchCase = (1L << 30),

            /// <summary>
            /// The documentation states that this rule overrides another rule that is not explained,
            /// or not referenced correctly. Since doing a CTRL+F on the quoted reference yielded no
            /// results, this option will be ignored.
            /// </summary>
            [Obsolete("This option is ignored.")]
            Collapse = (1L << 31),

            /// <summary>
            /// This option will be ignored. See remarks on Collapse option. 
            /// </summary>
            [Obsolete("This option is ignored.")]
            ExceptCollapse = (1L << 32),

            /// <summary>
            /// The rule should modify the request headers of a matching request to include the DNT
            /// header, if it is not already specified.
            /// </summary>
            DoNotTrack = (1L << 33),

            /// <summary>
            /// The rule, as the documentation describes, should mean disabling generic element
            /// hiding, meaning any element hide rule that is not bound to a domain. This option will
            /// be ignored for now.
            /// </summary>
            [Obsolete("This option is ignored.")]
            GenericHide = (1L << 34),

            /// <summary>
            /// The rule, as the documentation describes, should mean that URL filtering rules that
            /// are not bound to a domain should be disabled on this page. This option will be
            /// ignored for now.
            /// </summary>
            [Obsolete("This option is ignored.")]
            GenericBlock = (1L << 35),

            /// <summary>
            /// The rule should apply to requests started by a hyperlink ping or
            /// navigator.sendBeacon(). This option will be ignored for two reasons. First, the
            /// hyperlink ping is unsupported by everyone but Chrome. Second, it is unknown whether
            /// or not it's possible to identify such request by simply inspecting the HTTP headers.
            /// If it is possible, then this will be revisited.
            ///
            /// Note also that I can't find a single use of this option in EasyList. Probably not
            /// worth investigating at all.
            /// </summary>
            [Obsolete("This option is ignored.")]
            Ping = (1L << 36)
        }

        /// <summary>
        /// The base class for any fragment that must match against a URI. 
        /// </summary>
        public class UrlFilteringRuleFragment
        {
            /// <summary>
            /// Gets whether or not the supplied URI matches this rule segment.
            /// </summary>
            /// <param name="source">
            /// The URI.
            /// </param>
            /// <param name="lastPosition">
            /// The current position in scan.
            /// </param>
            /// <returns>
            /// </returns>
            public virtual int IsMatch(Uri source, int lastPosition)
            {
                return -1;
            }
        }

        /// <summary>
        /// The WildcardFragment class is responsible for matching at minimum one single character of
        /// any kind, starting from a specific position.
        /// </summary>
        public class WildcardFragment : UrlFilteringRuleFragment
        {
            /// <summary>
            /// Gets whether or not the supplied URI matches this rule segment.
            /// </summary>
            /// <param name="source">
            /// The URI.
            /// </param>
            /// <param name="lastPosition">
            /// The current position in scan.
            /// </param>
            /// <returns>
            /// </returns>
            public override int IsMatch(Uri source, int lastPosition)
            {
                var stepOne = lastPosition + 1;
                if(stepOne <= source.AbsoluteUri.Length)
                {
                    return stepOne;
                }

                return -1;
            }
        }

        /// <summary>
        /// The SeparatorFragment class is responsible for matching one out of a specific, limited
        /// set of characters which are deemed to separate portions of a URI, starting from a given position.
        /// </summary>
        public class SeparatorFragment : UrlFilteringRuleFragment
        {
            /// <summary>
            /// An array of characters that qualify as separator characters.
            /// </summary>
            public static readonly char[] SeparatorChars = new[] { '/', ':', '?', '=', '&' };

            /// <summary>
            /// Gets whether or not the supplied URI matches this rule segment.
            /// </summary>
            /// <param name="source">
            /// The URI.
            /// </param>
            /// <param name="lastPosition">
            /// The current position in scan.
            /// </param>
            /// <returns>
            /// </returns>
            public override int IsMatch(Uri source, int lastPosition)
            {
                if(lastPosition > source.AbsoluteUri.Length)
                {
                    return -1;
                }

                return source.AbsoluteUri.IndexOfAny(SeparatorChars, lastPosition) + 1;
            }
        }

        /// <summary>
        /// Base class for any string matching fragment. Defines the property to indicate whether
        /// case insensitive or case sensitive matching is required.
        /// </summary>
        public class StringFragment : UrlFilteringRuleFragment
        {
            /// <summary>
            /// Gets whether or not this rule ignores case during comparison.
            /// </summary>
            public bool ICase
            {
                get;
                private set;
            } = true;

            /// <summary>
            /// Constructs a new instance.
            /// </summary>
            /// <param name="iCase">
            /// Whether or not this rule ignores case during comparison.
            /// </param>
            public StringFragment(bool iCase)
            {
                ICase = iCase;
            }

            /// <summary>
            /// Constructs a new instance.
            /// </summary>
            public StringFragment()
            {
            }
        }

        /// <summary>
        /// This class represents a requirement for an exact match on an address. Here, address is
        /// defined as the full request URI.
        /// </summary>
        public class AnchoredAddressFragment : StringFragment
        {
            /// <summary>
            /// Gets the request segment to be matched.
            /// </summary>
            public string Request
            {
                get;
                private set;
            } = string.Empty;

            /// <summary>
            /// Constructs a new instance.
            /// </summary>
            /// <param name="request">
            /// The request segment to match.
            /// </param>
            /// <param name="iCase">
            /// Whether or not matching will ignore case.
            /// </param>
            public AnchoredAddressFragment(string request, bool iCase) : base(iCase)
            {
                Request = request;
            }

            /// <summary>
            /// Constructs a new instance.
            /// </summary>
            public AnchoredAddressFragment()
            {
            }

            /// <summary>
            /// Gets whether or not the supplied URI matches this rule segment.
            /// </summary>
            /// <param name="source">
            /// The URI.
            /// </param>
            /// <param name="lastPosition">
            /// The current position in scan.
            /// </param>
            /// <returns>
            /// </returns>
            public override int IsMatch(Uri source, int lastPosition)
            {
                // Anchored stuff like this always starts at position zero.
                if(lastPosition > 0)
                {
                    return -1;
                }

                if(Request.Length > source.AbsoluteUri.Length)
                {
                    return -1;
                }

                if (ICase)
                {
                    if (source.AbsoluteUri.Substring(0, Request.Length).EqualsAtICase(Request, 0))
                    {
                        return Request.Length;
                    }
                }
                else
                {
                    if (source.AbsoluteUri.Substring(0, Request.Length).EqualsAt(Request, 0))
                    {
                        return Request.Length;
                    }

                }

                return -1;
            }
        }

        /// <summary>
        /// The AnchoredDomainFragment class is responsible for determining if a specific URI has a
        /// domain fully or partially matching its content.
        /// </summary>
        public class AnchoredDomainFragment : UrlFilteringRuleFragment
        {
            /// <summary>
            /// Gets the domain that should be matched.
            /// </summary>
            public string Domain
            {
                get;
                private set;
            } = string.Empty;

            /// <summary>
            /// Constructs a new instance.
            /// </summary>
            /// <param name="domain">
            /// The domain that should be matched.
            /// </param>
            public AnchoredDomainFragment(string domain)
            {
                Domain = domain;
            }

            /// <summary>
            /// Constructs a new instance.
            /// </summary>
            public AnchoredDomainFragment()
            {
            }

            /// <summary>
            /// Gets whether or not the supplied URI matches this rule segment.
            /// </summary>
            /// <param name="source">
            /// The URI.
            /// </param>
            /// <param name="lastPosition">
            /// The current position in scan.
            /// </param>
            /// <returns>
            /// </returns>
            public override int IsMatch(Uri source, int lastPosition)
            {
                if(Domain.Length > source.Host.Length)
                {
                    return -1;
                }

                if(Domain.EqualsAt(source.Host.Substring(source.Host.Length - Domain.Length), 0))
                {
                    // Why + 3? Because of "://". The scheme doesn't include this.
                    return source.Scheme.Length + 3 + source.Host.Length;
                }

                return -1;
            }
        }

        /// <summary>
        /// The StringLiteralFragment class is responsible for matching a string literal within a URI
        /// after a specific position in the URI's source string.
        /// </summary>
        public class StringLiteralFragment : StringFragment
        {
            /// <summary>
            /// Gets the value to be matched.
            /// </summary>
            public string Value
            {
                get;
                private set;
            } = string.Empty;

            /// <summary>
            /// Constructs a new instance.
            /// </summary>
            /// <param name="value">
            /// The value to be matched.
            /// </param>
            /// <param name="iCase">
            /// Whether or not case should be ignored during matching.
            /// </param>
            public StringLiteralFragment(string value, bool iCase) : base(iCase)
            {
                Value = value;
            }

            /// <summary>
            /// Constructs a new instance.
            /// </summary>
            public StringLiteralFragment()
            {
            }

            /// <summary>
            /// Gets whether or not the supplied URI matches this rule segment.
            /// </summary>
            /// <param name="source">
            /// The URI.
            /// </param>
            /// <param name="lastPosition">
            /// The current position in scan.
            /// </param>
            /// <returns>
            /// </returns>
            public override int IsMatch(Uri source, int lastPosition)
            {
                if(lastPosition > source.AbsoluteUri.Length)
                {
                    return -1;
                }

                if(lastPosition + Value.Length > source.AbsoluteUri.Length)
                {
                    return -1;
                }

                var matchIndex = source.AbsoluteUri.IndexOf(Value, lastPosition, ICase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

                if(matchIndex != -1)
                {
                    return matchIndex + Value.Length;
                }

                return -1;
            }
        }

        /// <summary>
        /// Gets a list of all referers that this URL filter rule applies to. In the event that
        /// this array is empty, the referer field on requests will not be checked.
        /// </summary>
        public List<string> ApplicableReferers
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a list of all referers that this URL filter rule applies to. In the event that
        /// this array is empty, the referer field on requests will not be checked.
        /// </summary>
        public List<string> ExceptReferers
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a list of all domains that this URL filter rule applies to. In the event that
        /// this array is empty, the rule applies globally, to all domains.
        /// </summary>
        public List<string> ApplicableDomains
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a list of all domains that this URL filter should not be applied to. In the event
        /// that this array is empty, the rule applies either globally, or exclusively to the list of
        /// applicable domains, if that property is not empty.
        /// </summary>
        public List<string> ExceptionDomains
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets all options that apply to this filtering rule. 
        /// </summary>
        public long OptionsLong
        {
            get
            {
                return (long)Options;
            }
            set
            {
                Options = (UrlFilterOptions)value;
            }
        }

        /// <summary>
        /// Gets all options that apply to this filtering rule. 
        /// </summary>
        public UrlFilterOptions Options
        {
            get;
            private set;
        } = UrlFilterOptions.None;

        /// <summary>
        /// The UrlFilteringRuleFragment parts that make up this filter. 
        /// </summary>
        public List<UrlFilteringRuleFragment> Parts
        {
            get;
            private set;
        } = new List<UrlFilteringRuleFragment>();

        /// <summary>
        /// Constructs a new UrlFilter instance. Note that UrlFilter objects, nor any Filter object,
        /// are meant to be constructed outside of the AbpFormatRuleParser class.
        /// </summary>
        /// <param name="originalRule">
        /// The original rule string used to build this filter.
        /// </param>
        /// <param name="parts">
        /// The UrlFilterFragment parts that make up this filter.
        /// </param>
        /// <param name="options">
        /// The filter options.
        /// </param>
        /// <param name="applicableDomains">
        /// Domains that the filter should be applied to.
        /// </param>
        /// <param name="exceptionDomains">
        /// Domains that the filter should not be applied to.
        /// </param>
        /// <param name="applicableReferers">
        /// Referers that the filter rule should be applied to.
        /// </param>
        /// <param name="exceptionReferers">
        /// Referers that the filter rule should not be applied to.
        /// </param>
        /// <param name="isException">
        /// Whether or not the filter is an exception, that is to say, a whitelisting filter.
        /// </param>
        /// <param name="categoryId">
        /// The category ID of the category this filter belongs to.
        /// </param>
        internal UrlFilter(string originalRule, List<UrlFilteringRuleFragment> parts, UrlFilterOptions options, List<string> applicableDomains, List<string> exceptionDomains, List<string> applicableReferers, List<string> exceptionReferers, bool isException, short categoryId) : base(originalRule, isException, categoryId)
        {
            Parts = parts;
            Options = options;

            ApplicableDomains = applicableDomains;
            ExceptionDomains = exceptionDomains;

            ApplicableReferers = applicableReferers;
            ExceptReferers = exceptionReferers;
        }

        /// <summary>
        /// Determines whether or not this filter is a match for the supplied HTTP request/response. 
        /// </summary>
        /// <param name="uri">
        /// The URI to check against for a match. 
        /// </param>
        /// <param name="rawHeaders">
        /// The headers for the request/response we're checking to see if we match against. These may
        /// modify the capability to match depending on their content, such as content-type, etc.
        /// </param>
        /// <returns>
        /// True if this filter is a positive match against the supplied URI, false otherwise. 
        /// </returns>
        public bool IsMatch(Uri uri, NameValueCollection rawHeaders)
        {
            // Make sure that the headers match up with our options.
            if(this.Options != UrlFilterOptions.None)
            {
                string headerVal = null;
                long xmlHttpRequestBits = ((OptionsLong & (long)UrlFilterOptions.ExceptXmlHttpRequest) | (OptionsLong & (long)UrlFilterOptions.XmlHttpRequest));
                if((headerVal = rawHeaders.Get("X-Requested-With")) != null)
                {
                    if(headerVal.EqualsAtICase("XMLHttpRequest", 0))
                    {
                        xmlHttpRequestBits &= ~(long)UrlFilterOptions.XmlHttpRequest;
                    }
                    else
                    {
                        xmlHttpRequestBits &= ~(long)UrlFilterOptions.ExceptXmlHttpRequest;
                    }
                }

                if(xmlHttpRequestBits != 0)
                {
                    // XML HttpRequest bits were not cleared, meaning that one of those options was
                    // not satisifed.
                    return false;
                }

                long thirdPartyBits = ((OptionsLong & (long)UrlFilterOptions.ThirdParty) | (OptionsLong & (long)UrlFilterOptions.ExceptThirdParty));
                if((headerVal = rawHeaders.Get("Referer")) != null)
                {

                    if (Uri.TryCreate(headerVal, UriKind.RelativeOrAbsolute, out Uri refererUri))
                    {
                        string hostWithoutWww = refererUri.Host;

                        if (hostWithoutWww.StartsWithQuick("www."))
                        {
                            hostWithoutWww = hostWithoutWww.Substring(4);
                        }

                        if (hostWithoutWww.EqualsAtICase(uri.Host, 0))
                        {
                            thirdPartyBits &= ~(long)UrlFilterOptions.ExceptThirdParty;
                        }
                        else
                        {
                            thirdPartyBits &= ~(long)UrlFilterOptions.ThirdParty;
                        }


                        if (ApplicableDomains.Count > 0 && !ApplicableDomains.Contains(hostWithoutWww))
                        {
                            return false;
                        }

                        if (ExceptionDomains.Count > 0 && ExceptionDomains.Contains(hostWithoutWww))
                        {
                            return false;
                        }

                        // While we have the referer field, let's go ahead and check if we have
                        // referer options and if we do or don't have a match.
                        //
                        // This is a shortcut. We unfortunately need to also execute this code also
                        // when there are no options.
                        if (ApplicableReferers.Count > 0 && !ApplicableReferers.Contains(hostWithoutWww))
                        {
                            return false;
                        }

                        if (ExceptReferers.Count > 0 && ExceptReferers.Contains(hostWithoutWww))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // The "Referer" field can be omitted when it's a brand new browser request/fresh
                    // browser context, meaning that this request is not third party in the least
                    // bit. So, we clear this bit in this special case.
                    thirdPartyBits &= ~(long)UrlFilterOptions.ExceptThirdParty;
                }

                if(thirdPartyBits != 0)
                {
                    // Third party bits not cleared, meaning that one of those options was not satisifed.
                    return false;
                }

                long contentTypeBits = ((OptionsLong & (long)UrlFilterOptions.Image) | (OptionsLong & (long)UrlFilterOptions.Script) | (OptionsLong & (long)UrlFilterOptions.StyleSheet) | (OptionsLong & (long)UrlFilterOptions.ExceptImage) | (OptionsLong & (long)UrlFilterOptions.ExceptScript) | (OptionsLong & (long)UrlFilterOptions.ExceptStyleSheet));

                if((headerVal = rawHeaders.Get("Content-Type")) != null)
                {
                    if(headerVal.IndexOfQuick("script") != -1)
                    {
                        contentTypeBits &= ~(long)UrlFilterOptions.Script;
                    }
                    else
                    {
                        contentTypeBits &= ~(long)UrlFilterOptions.ExceptScript;

                        if(headerVal.IndexOfQuick("image") != -1)
                        {
                            contentTypeBits &= ~(long)UrlFilterOptions.Image;
                        }
                        else
                        {
                            contentTypeBits &= ~(long)UrlFilterOptions.ExceptImage;

                            if(headerVal.IndexOfQuick("css") != -1)
                            {
                                contentTypeBits &= ~(long)UrlFilterOptions.StyleSheet;
                            }
                            else
                            {
                                contentTypeBits &= ~(long)UrlFilterOptions.ExceptStyleSheet;
                            }
                        }
                    }
                }

                if(contentTypeBits != 0)
                {
                    // XML HttpRequest bits were not cleared, meaning that one of those options was
                    // not satisifed.
                    return false;
                }
            }
            else
            {
                if(ApplicableReferers.Count > 0 || ExceptReferers.Count > 0)
                {
                    string headerVal = null;
                    if((headerVal = rawHeaders.Get("Referer")) != null)
                    {

                        if (Uri.TryCreate(headerVal, UriKind.RelativeOrAbsolute, out Uri refererUri))
                        {
                            string hostWithoutWww = refererUri.Host;

                            if (hostWithoutWww.StartsWithQuick("www."))
                            {
                                hostWithoutWww = hostWithoutWww.Substring(4);
                            }

                            if (ApplicableReferers.Count > 0 && !ApplicableReferers.Contains(hostWithoutWww))
                            {
                                return false;
                            }

                            if (ExceptReferers.Count > 0 && ExceptReferers.Contains(hostWithoutWww))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            if (ApplicableDomains.Count > 0 || ExceptionDomains.Count > 0)
            {
                string hostWithoutWww = uri.Host;

                if (hostWithoutWww.StartsWithQuick("www."))
                {
                    hostWithoutWww = hostWithoutWww.Substring(4);
                }

                if (ApplicableDomains.Count > 0 && !ApplicableDomains.Contains(hostWithoutWww))
                {
                    return false;
                }

                if (ExceptionDomains.Count > 0 && ExceptionDomains.Contains(hostWithoutWww))
                {
                    return false;
                }
            }

            int matchIndex = 0;
            foreach(var part in Parts)
            {
                matchIndex = part.IsMatch(uri, matchIndex);

                if(matchIndex == -1)
                {
                    return false;
                }
            }

            // If all parts were found, then this match was a success.
            return true;
        }

        /// <summary>
        /// Clears data from the filter that may not be needed if externalized elsewhere. 
        /// </summary>
        public override void TrimExcessData()
        {
            OriginalRule = string.Empty;
        }
    }
}