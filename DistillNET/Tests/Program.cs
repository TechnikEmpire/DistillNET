/*
 * Copyright © 2017 Jesse Nicholson
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DistillNET
{
    internal class Program
    {
        private static void TestDomainWideException()
        {
            FilterDbCollection col = new FilterDbCollection();

            //"@@$third-party,referer=~pinterest.com"

            col.ParseStoreRules(new[] { "@@$referer=pinterest.com" }, 1);
            col.FinalizeForRead();
         
            var headersShouldMatch = new NameValueCollection(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Requested-With", "XmlHttpRequest" },
                { "Content-Type", "script" },
                { "Referer", "https://www.pinterest.com" },
            };

            var headersShouldnt = new NameValueCollection(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Requested-With", "XmlHttpRequest" },
                { "Content-Type", "script" },
                { "Referer", "https://www.silsly.com" },
            };

            var uri = new Uri("http://silly.com/stoopid/url&=b1");

            var allRules = col.GetWhitelistFiltersForDomain();

            foreach(var wlr in allRules)
            {
                Console.WriteLine("Inc R: {0}", string.Join(", ", wlr.ApplicableReferers));
                Console.WriteLine("Exc R: {0}", string.Join(", ", wlr.ExceptReferers));

                Console.WriteLine(wlr.IsMatch(uri, headersShouldMatch));
                Console.WriteLine(wlr.IsMatch(uri, headersShouldnt));
            }

            Console.ReadKey();
        }

        private static void Main(string[] args)
        {
            var parser = new AbpFormatRuleParser();

            string easylistPath = AppDomain.CurrentDomain.BaseDirectory + "easylist.txt";
            string adultDomainsPath = AppDomain.CurrentDomain.BaseDirectory + "adult_domains.txt";

            var easylistLines = File.ReadAllLines(easylistPath);
            var adultLines = File.ReadAllLines(adultDomainsPath);

            var sw = new Stopwatch();

            Console.WriteLine("Testing Parser Speed");
            Console.WriteLine("About To Parse {0} Filters From Easylist", easylistLines.Length);

            var compiledFilters = new List<Filter>(easylistLines.Length);

            sw.Start();
            foreach(var entry in easylistLines)
            {
                compiledFilters.Add(parser.ParseAbpFormattedRule(entry, 1));
            }
            sw.Stop();

            Console.WriteLine("Parsed {0} Easylist filters in {1} msec, averaging {2} msec per filter.", compiledFilters.Count, sw.ElapsedMilliseconds, sw.ElapsedMilliseconds / (double)compiledFilters.Count);

            compiledFilters = new List<Filter>(adultDomainsPath.Length);

            Console.WriteLine();
            Console.WriteLine("Testing Parser Speed");
            Console.WriteLine("About To Parse {0} Filters From Adult Domains", adultLines.Length);

            sw.Restart();
            foreach(var entry in adultLines)
            {
                compiledFilters.Add(parser.ParseAbpFormattedRule(entry, 1));
            }
            sw.Stop();

            Console.WriteLine("Parsed {0} Adult Domains filters in {1} msec, averaging {2} msec per filter.", compiledFilters.Count, sw.ElapsedMilliseconds, sw.ElapsedMilliseconds / (double)compiledFilters.Count);

            Array.Clear(adultLines, 0, adultLines.Length);
            Array.Clear(easylistLines, 0, easylistLines.Length);
            adultLines = null;
            easylistLines = null;
            System.GC.Collect();

            Console.WriteLine();
            Console.WriteLine("Testing Parse And Store To DB Speed");

            //var dbOutPath = AppDomain.CurrentDomain.BaseDirectory + "Test.db";
            //var filterCollection = new FilterDbCollection(dbOutPath);
            var filterCollection = new FilterDbCollection(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rules.db"), true, true);

            var adultFileStream = File.OpenRead(adultDomainsPath);
            var easylistFileStream = File.OpenRead(easylistPath);

            sw.Restart();
            var adultResult = filterCollection.ParseStoreRulesFromStream(adultFileStream, 1);
            var easyListResult = filterCollection.ParseStoreRulesFromStream(easylistFileStream, 2);

            // Ensure that we build the index AFTER we're all done our inserts.
            filterCollection.FinalizeForRead();
            sw.Stop();

            Console.WriteLine("Parsed And Stored {0} filters in {1} msec, averaging {2} msec per filter.", adultResult.Item1 + easyListResult.Item1, sw.ElapsedMilliseconds, sw.ElapsedMilliseconds / (double)(adultResult.Item1 + easyListResult.Item1));
            
            Console.WriteLine();
            Console.WriteLine("Testing Rule Lookup By Domain From DB");            
            int loadedFilters = 0;
            sw.Restart();
            for(int i = 0; i < 1000; ++i)
            {
                loadedFilters += filterCollection.GetFiltersForDomain().Count();
            }
            sw.Stop();

            Console.WriteLine("Looked up and reconstructed {0} filters from DB in {1} msec, averaging {2} msec per lookup and {3} msec per filter lookup and reconstruction.", loadedFilters, sw.ElapsedMilliseconds, sw.ElapsedMilliseconds / (double)(1000), sw.ElapsedMilliseconds / (double)(loadedFilters));

            Console.WriteLine();
            TestFilterMatching();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void TestFilterMatching()
        {
            // XXX TODO - This is the basis to begin a good benchmark, but is
            // not one itself. Our speeds will be skewed by caching and such,
            // so to really bench this, we need more random, randomly ordered
            // "in-the-wild" kind of stuff.
            var rp = new AbpFormatRuleParser();
            var filter = rp.ParseAbpFormattedRule("||silly.com^stoopid^url^*1$xmlhttprequest,script,~third-party", 1) as UrlFilter;
            var headers = new NameValueCollection(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Requested-With", "XmlHttpRequest" },
                { "Content-Type", "script" },
            };

            var uri = new Uri("http://silly.com/stoopid/url&=b1");

            double d = 10000000;
            var results = new List<bool>((int)d);
            var sw2 = new Stopwatch();            
            Console.WriteLine("Roughly Benchmarking Filter Matching Speed");
            sw2.Start();
            for(int i = 0; i < d; ++i)
            {
                results.Add(filter.IsMatch(uri, headers));
            }
            sw2.Stop();

            // Should be less than a microsecond.
            Console.WriteLine("Filter matching loosely benchmarked at {0} microseconds per check.", ((sw2.ElapsedMilliseconds * 1000) / d));            
        }
    }
}