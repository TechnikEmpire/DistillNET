# DistillNET
DistillNET is a library for matching and filtering HTTP requests and HTML response content using the Adblock Plus Filter format. Note that while the CSS selector rule parser and object are complete, they are not implemented in anything, because an HTML parser backend is needed. This is a future goal.

DistillNET is available on [Nuget](https://www.nuget.org/packages/DistillNET/).


DistillNET was designed to be fast, considering every other factor last. An example of this is the fact that DistillNET will create multiple inserts into its database for the same rule, if more than one domain is attached to the rule. To be clear, the rule:

 - `example.com/stuff$domain=one.com|two.com` 

will create two entries in its database. One for `one.com` and another for `two.com`. This way, either domain would trigger the re-creation of this rule. Disk space is wasted as a trade off to avoid a complex (AKA slow) database indexing structure that would otherwise preserve space.

As of version 1.4.6, DistillNET uses an internal memory cache for database lookups that is configurable at construction time with an optional argument. The default value will cache looksups for 10 minutes. This has improved lookup performance several hundred fold. See benchmark output below.

Benchmark output on an `i7-6700 @ 3.4GHZ`:  

```bash
Testing Parser Speed
About To Parse 61081 Filters From Easylist
Parsed 61081 Easylist filters in 108 msec, averaging 0.00176814394001408 msec per filter.

Testing Parser Speed
About To Parse 1454513 Filters From Adult Domains
Parsed 1454513 Adult Domains filters in 2102 msec, averaging 0.00144515724507103 msec per filter.

Testing Parse And Store To DB Speed
Parsed And Stored 1484236 filters in 11400 msec, averaging 0.00768071923871945 msec per filter.

Testing Rule Lookup By Domain From DB
Looked up and reconstructed 7778000 filters from DB in 28 msec, averaging 0.028 msec per lookup and 3.59989714579583E-06 msec per filter lookup and reconstruction.

Roughly Benchmarking Filter Matching Speed
Filter matching loosely benchmarked at 0.7133 microseconds per check.
Press any key to exit...
```
#### Before Version 1.4.6
```bash
Testing Rule Lookup By Domain From DB
Looked up and reconstructed 7778000 filters from DB in 9801 msec, averaging 9.801 msec per lookup and 
0.00126009256878375 msec per filter lookup and reconstruction.
```

The addition of configurable memory caching in 1.4.6 has improved lookup performance by ~426x.

Note that the filter matching benchmark needs to be upgraded to act more "in the wild", doing things such as handling random data. 

In summary, filter matching is clocking in on my hardware at under a microsecond. The parser can chew through rules in about a microsecond each, meaning 1M rules can be produced per second. The goal here is to have complex URL filtering at next to zero cost.

### Why U No Serialize/Deserialize?
As mentioned above, the parsing is extremely fast. During development I did several tests using serialization instead of continuous parsing, and found that every solution was much slower than the parser itself. Protobuf-net, ZeroFormatter, MessagePack etc all were slower, mostly much slower.

### Future Goals:
 - Either change API to allow recall of CSS selector filter objects, or implement filtering with them internally.
