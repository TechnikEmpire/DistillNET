/*
 * Copyright © 2017 Jesse Nicholson
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using DistillNET.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DistillNET
{
    /// <summary>
    /// The FilterDbCollection class is responsible for parsing and storing rules, with associated
    /// category ID's, into a database. The class is also responsible for calling up collections of
    /// rules on the fly for any given, known domain. Called up rules are re-parsed at every lookup
    /// rather than serialized/deserialized, because the parser is much faster than such utilities
    /// such as protobuf.
    /// </summary>
    public class FilterDbCollection : IDisposable
    {
        /// <summary>
        /// Our rule parser.
        /// </summary>
        private AbpFormatRuleParser m_ruleParser;

        /// <summary>
        /// Our Sqlite connection.
        /// </summary>
        private SqliteConnection m_connection;

        /// <summary>
        /// The global key used to index non-domain specific filters.
        /// </summary>
        private readonly string m_globalKey;

        /// <summary>
        /// Memory cache.
        /// </summary>
        private MemoryCache m_cache;

        /// <summary>
        /// Mem cache options.
        /// </summary>
        private readonly MemoryCacheOptions m_cacheOptions;

        /// <summary>
        /// Constructs a new FilterDbCollection using an in-memory database.
        /// </summary>
        /// <param name="cacheOptions">
        /// User defined query caching options.
        /// </param>
        public FilterDbCollection(MemoryCacheOptions cacheOptions = null) : this(null, true, true, cacheOptions)
        {   
        }

        /// <summary>
        /// Constructs a new FilterDbCollection.
        /// </summary>
        /// <param name="dbAbsolutePath">
        /// The absolute path where the database exists or should be created.
        /// </param>
        /// <param name="overwrite">
        /// If true, and a file exists at the supplied absolute db path, it will be deleted first.
        /// Default is true.
        /// </param>
        /// <param name="useMemory">
        /// If true, the database will be created as a purely in-memory database.
        /// </param>
        /// <param name="cacheOptions">
        /// User defined query caching options.
        /// </param>
        public FilterDbCollection(string dbAbsolutePath, bool overwrite = true, bool useMemory = false, MemoryCacheOptions cacheOptions = null)
        {
            if(!useMemory && overwrite && File.Exists(dbAbsolutePath))
            {
                File.Delete(dbAbsolutePath);
            }

            if(cacheOptions == null)
            {
                cacheOptions = new MemoryCacheOptions
                {
                    ExpirationScanFrequency = TimeSpan.FromMinutes(10)
                };
            }

            m_cacheOptions = cacheOptions;

            bool isNew = !File.Exists(dbAbsolutePath);

            m_ruleParser = new AbpFormatRuleParser();

            if(useMemory)
            {
                var version = typeof(FilterDbCollection).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
                var rnd = new Random();
                var rndNum = rnd.Next();
                var generatedDbName = string.Format("{0} {1} - {2}", nameof(FilterDbCollection), version, rndNum);

                // "Data Source = :memory:; Cache = shared;"
                var cb = new SqliteConnectionStringBuilder
                {
                    Mode = SqliteOpenMode.Memory,
                    Cache = SqliteCacheMode.Shared,
                    DataSource = generatedDbName
                };
                m_connection = new SqliteConnection(cb.ToString());
            }
            else
            {
                // "Data Source={0};"
                var cb = new SqliteConnectionStringBuilder
                {
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Cache = SqliteCacheMode.Shared,
                    DataSource = dbAbsolutePath
                };
                m_connection = new SqliteConnection(cb.ToString());                
            }

            //m_connection. = SQLiteConnectionFlags.UseConnectionPool | SQLiteConnectionFlags.NoConvertSettings | SQLiteConnectionFlags.NoVerifyTypeAffinity;            
            m_connection.Open();

            ConfigureDatabase();

            CreateTables();

            m_globalKey = "global";
        }

        /// <summary>
        /// Configures the database page size, cache size for optimal performance according to our
        /// needs.
        /// </summary>
        private void ConfigureDatabase()
        {
            using(var cmd = m_connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA cache_size=-65536;";
                cmd.ExecuteNonQuery();

                /*
                cmd.CommandText = "PRAGMA page_size=65536;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA soft_heap_limit=131072;";
                cmd.ExecuteNonQuery();
                 */

                cmd.CommandText = "PRAGMA synchronous=OFF;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA journal_mode=OFF;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA locking_mode=NORMAL;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA temp_store=2;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA ignore_check_constraints=TRUE;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA cell_size_check=FALSE;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA cache_spill=FALSE;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA automatic_index=FALSE;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA busy_timeout=20000;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA secure_delete=FALSE;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA shrink_memory;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = string.Format("PRAGMA threads={0};", Environment.ProcessorCount);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Creates needed tables on the source database if they do not exist.
        /// </summary>
        private void CreateTables()
        {
            using(var tsx = m_connection.BeginTransaction())
            using (var command = m_connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE IF NOT EXISTS UrlFiltersIndex (Domains VARCHAR(255), CategoryId INT16, IsWhitelist BOOL, Source TEXT)";
                command.ExecuteNonQuery();

                tsx.Commit();
            }
        }

        /// <summary>
        /// Creates needed indexes on the database if they do not exist.
        /// </summary>
        private void CreatedIndexes()
        {
            using(var command = m_connection.CreateCommand())
            {
                command.CommandText = "CREATE INDEX IF NOT EXISTS domain_index ON UrlFiltersIndex (Domains)";
                command.ExecuteNonQuery();

                command.CommandText = "CREATE INDEX IF NOT EXISTS whitelist_index ON UrlFiltersIndex (IsWhitelist)";
                command.ExecuteNonQuery();

                command.CommandText = "CREATE INDEX IF NOT EXISTS dual_index ON UrlFiltersIndex (Domains, IsWhitelist)";
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Finalize the database for read only access. This presently builds indexes if they do not
        /// exist, and is meant to be called AFTER all bulk insertions are complete. Note that
        /// calling this command does not rebuild the Sqlite connection to enforce read-only mode.
        /// Write access is still possible after calling.
        /// </summary>
        public void FinalizeForRead()
        {
            CreatedIndexes();

            using (var cmd = m_connection.CreateCommand())
            {
                // Put the database in read-only mode.
                cmd.CommandText = "PRAGMA query_only=TRUE;";
                cmd.ExecuteNonQuery();
            }
        }

        private void RecreateCache()
        {
            if (m_cache != null)
            {   
                m_cache.Dispose();
            }

            m_cache = new MemoryCache(m_cacheOptions);
        }

        /// <summary>
        /// Parses the supplied list of rules and stores them in the assigned database for retrieval,
        /// indexed by the rule's domain names.
        /// </summary>
        /// <param name="rawRuleStrings">
        /// The raw filter strings.
        /// </param>
        /// <param name="categoryId">
        /// The category ID that each of the supplied filters is deemeded to belong to.
        /// </param>
        /// <returns>
        /// A tuple where the first item is the total number of rules successfully parsed and stored,
        /// and the second item is the total number of rules that failed to be parsed and stored.
        /// Failed rules are an indication of improperly formatted rules.
        /// </returns>
        public Tuple<int, int> ParseStoreRules(string[] rawRuleStrings, short categoryId)
        {
            RecreateCache();

            int loaded = 0, failed = 0;

            using(var transaction = m_connection.BeginTransaction())
            {
                using(var cmd = m_connection.CreateCommand())
                {   
                    cmd.CommandText = "INSERT INTO UrlFiltersIndex VALUES ($domain, $categoryId, $isWhitelist, $source)";
                    var domainParam = new SqliteParameter("$domain", DbType.String);
                    var categoryIdParam = new SqliteParameter("$categoryId", DbType.Int16);
                    var isWhitelistParam = new SqliteParameter("$isWhitelist", DbType.Boolean);
                    var sourceParam = new SqliteParameter("$source", DbType.String);
                    cmd.Parameters.Add(domainParam);
                    cmd.Parameters.Add(categoryIdParam);
                    cmd.Parameters.Add(isWhitelistParam);
                    cmd.Parameters.Add(sourceParam);

                    cmd.Prepare();

                    var len = rawRuleStrings.Length;                    
                    for(int i = 0; i < len; ++i)
                    {
                        rawRuleStrings[i] = rawRuleStrings[i].TrimQuick();

                        if (!(m_ruleParser.ParseAbpFormattedRule(rawRuleStrings[i], categoryId) is UrlFilter filter))
                        {
                            ++failed;
                            continue;
                        }

                        ++loaded;

                        if(filter.ApplicableDomains.Count > 0)
                        {
                            foreach(var dmn in filter.ApplicableDomains)
                            {   
                                cmd.Parameters[0].Value = dmn;
                                cmd.Parameters[1].Value = categoryId;
                                cmd.Parameters[2].Value = filter.IsException;
                                cmd.Parameters[3].Value = rawRuleStrings[i];
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            cmd.Parameters[0].Value = m_globalKey;
                            cmd.Parameters[1].Value = categoryId;
                            cmd.Parameters[2].Value = filter.IsException;
                            cmd.Parameters[3].Value = rawRuleStrings[i];
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                transaction.Commit();
            }

            return new Tuple<int, int>(loaded, failed);
        }

        /// <summary>
        /// Parses the supplied list of rules and stores them in the assigned database for retrieval,
        /// indexed by the rule's domain names.
        /// </summary>
        /// <param name="rawRulesStream">
        /// The stream from which to read raw rules as lines.
        /// </param>
        /// <param name="categoryId">
        /// The category ID that each of the supplied filters is deemeded to belong to.
        /// </param>
        /// <returns>
        /// A tuple where the first item is the total number of rules successfully parsed and stored,
        /// and the second item is the total number of rules that failed to be parsed and stored.
        /// Failed rules are an indication of improperly formatted rules.
        /// </returns>
        public Tuple<int, int> ParseStoreRulesFromStream(Stream rawRulesStream, short categoryId)
        {
            RecreateCache();

            int loaded = 0, failed = 0;

            using(var transaction = m_connection.BeginTransaction())
            {
                using(var cmd = m_connection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO UrlFiltersIndex VALUES ($domain, $categoryId, $isWhitelist, $source)";
                    var domainParam = new SqliteParameter("$domain", DbType.String);
                    var categoryIdParam = new SqliteParameter("$categoryId", DbType.Int16);
                    var isWhitelistParam = new SqliteParameter("$isWhitelist", DbType.Boolean);
                    var sourceParam = new SqliteParameter("$source", DbType.String);
                    cmd.Parameters.Add(domainParam);
                    cmd.Parameters.Add(categoryIdParam);
                    cmd.Parameters.Add(isWhitelistParam);
                    cmd.Parameters.Add(sourceParam);

                    cmd.Prepare();

                    string line = null;
                    using(var sw = new StreamReader(rawRulesStream))
                    while((line = sw.ReadLine()) != null)
                    {
                        line = line.TrimQuick();

                            if (!(m_ruleParser.ParseAbpFormattedRule(line, categoryId) is UrlFilter filter))
                            {
                                ++failed;
                                continue;
                            }

                            ++loaded;

                        if(filter.ApplicableDomains.Count > 0)
                        {
                            foreach(var dmn in filter.ApplicableDomains)
                            {
                                cmd.Parameters[0].Value = dmn;
                                cmd.Parameters[1].Value = categoryId;
                                cmd.Parameters[2].Value = filter.IsException;
                                cmd.Parameters[3].Value = line;
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            cmd.Parameters[0].Value = m_globalKey;
                            cmd.Parameters[1].Value = categoryId;
                            cmd.Parameters[2].Value = filter.IsException;
                            cmd.Parameters[3].Value = line;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                transaction.Commit();
            }

            return new Tuple<int, int>(loaded, failed);
        }

        /// <summary>
        /// Gets all blacklisting filters for the supplied domain.
        /// </summary>
        /// <param name="domain">
        /// The domain for which all filters should be loaded. Default is global, meaning that
        /// filters not anchored to any particular domain will be loaded.
        /// </param>
        /// <returns>
        /// A list of all compiled blacklisting URL filters for the given domain.
        /// </returns>
        public List<UrlFilter> GetFiltersForDomain(string domain = "global")
        {
            return GetFiltersForDomain(domain, false);
        }

        /// <summary>
        /// Gets all whitelisting filters for the supplied domain.
        /// </summary>
        /// <param name="domain">
        /// The domain for which all filters should be loaded. Default is global, meaning that
        /// filters not anchored to any particular domain will be loaded.
        /// </param>
        /// <returns>
        /// A list of all compiled whitelisting URL filters for the given domain.
        /// </returns>
        public List<UrlFilter> GetWhitelistFiltersForDomain(string domain = "global")
        {
            return GetFiltersForDomain(domain, true);
        }

        /// <summary>
        /// Gets a list of either all whitelist or all blacklist filters for the given domain.
        /// </summary>
        /// <param name="domain">
        /// The domain for which to retrieve all whitelist or blacklist filters.
        /// </param>
        /// <param name="isWhitelist">
        /// Whether or not to get whitelist filters. If false, blacklist filters will be selected.
        /// </param>
        /// <returns>
        /// A list of either all whitelist or all blacklist filters for the given domain.
        /// </returns>
        private List<UrlFilter> GetFiltersForDomain(string domain, bool isWhitelist)
        {
            var cacheKey = new Tuple<string, bool>(domain, isWhitelist);


            if (m_cache.TryGetValue(cacheKey, out List<UrlFilter> retVal))
            {
                return retVal;
            }

            retVal = new List<UrlFilter>();

            var allPossibleVariations = GetAllPossibleSubdomains(domain);
            
            using(var myConn = new SqliteConnection(m_connection.ConnectionString))
            {
                myConn.Open();

                using(var cmd = myConn.CreateCommand())
                {
                    switch(isWhitelist)
                    {
                        case true:
                        {
                            cmd.CommandText = @"SELECT * from UrlFiltersIndex where Domains = $domainId AND IsWhitelist = 1";
                        }
                        break;

                        default:
                        {
                            cmd.CommandText = @"SELECT * from UrlFiltersIndex where Domains = $domainId AND IsWhitelist = 0";
                        }
                        break;
                    }

                    var domainParam = new SqliteParameter("$domainId", System.Data.DbType.String);
                    cmd.Parameters.Add(domainParam);

                    cmd.Prepare();

                    foreach (var sub in allPossibleVariations)
                    {
                        cmd.Parameters[0].Value = sub;

                        using(var reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                short catId = reader.GetInt16(1);
                                retVal.Add((UrlFilter)m_ruleParser.ParseAbpFormattedRule(reader.GetString(3), catId));
                            }
                        }
                    }
                }
            }

            m_cache.Set(cacheKey, retVal);

            return retVal;
        }

        private List<string> GetAllPossibleSubdomains(string inputDomain)
        {
            var retVal = new List<string>() { inputDomain };
            int subPos = inputDomain.IndexOfQuick('.');

            while(subPos != -1)
            {
                inputDomain = inputDomain.Substring(subPos + 1);
                retVal.Add(inputDomain);
                subPos = inputDomain.IndexOfQuick('.');
            }

            return retVal;
        }

        public List<Filter> GetFiltersForRequest(Uri requestString, string referer = "")
        {
            return null;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if(!disposedValue)
            {
                if(disposing)
                {
                    if(m_connection != null)
                    {
                        m_connection.Close();
                        m_connection = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FilterDbCollection() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}