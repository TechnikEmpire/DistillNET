# DistillNET
DistillNET is a library for matching and filtering HTTP requests and HTML response content using the Adblock Plus Filter format. Note that while the CSS selector rule parser and object are complete, they are not implemented in anything, because an HTML parser backend is needed. This is a future goal.

DistillNET was designed to be fast, considering ever other factor last. An example of this is the fact that DistillNET will create multiple inserts into its database for the same rule, if more than one domain is attached to the rule. To be clear, the rule:

 - `example.com/stuff$domain=one.com|two.com` 

will create two entries in its database. One for `one.com` and another for `two.com`. This way, either domain would trigger the recreation of this rule. Disk space is wasted as a trade off to avoid complex (AKA slow) database indexing structure that would otherwise preserve space.

Sample output of the test application (which presently only tests speed) on an `i7-6700 @ 3.4GHZ`:  

```bash

Testing Parser Speed
About To Parse 61081 Filters From Easylist
Parsed 61081 Easylist filters in 105 msec, averaging 0.00171902883056924 msec per filter.

Testing Parser Speed
About To Parse 1454513 Filters From Adult Domains
Parsed 1454513 Adult Domains filters in 1590 msec, averaging 0.00109314939089578 msec per filter.

Testing Parse And Store To DB Speed
Parsed And Stored 1484236 filters in 8349 msec, averaging 0.00562511622140953 msec per filter.

Testing Rule Lookup By Domain From DB
Looked up and reconstructed 7778000 filters from DB in 9801 msec, averaging 9.801 msec per lookup and 
0.00126009256878375 msec per filter lookup and reconstruction.

Roughly Benchmarking Filter Matching Speed
Filter matching loosely benchmarked at 0.7029 microseconds per check.
Press any key to exit...

```

Note that the filter matching benchmark needs to be upgraded to act more "in the wild", doing things such as handling random data. 

In summary, filter matching is clocking in on my hardware at under a microsecond. The parser can chew through rules in about a microsecond, meaning 1M rules can be produced per second. The goal here is to have complex URL filtering at next to zero cost.

Future Goals:
 - Migrate to Sqlite.NET to gain a cross platform and cross-device backend.  
 - Either change API to allow recall of CSS selector filter objects, or implement filtering with them internally.
