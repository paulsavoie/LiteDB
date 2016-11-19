﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Manage ConnectionString to connect and create databases. Connection string are NameValue using Name1=Value1; Name2=Value2
    /// </summary>
    public class ConnectionString
    {
        public string Filename { get; private set; }

        public bool Journal { get; private set; }

        public string Password { get; private set; }

        public int CacheSize { get; private set; }

        public TimeSpan Timeout { get; private set; }

        public bool AutoCommit { get; private set; }

        public bool ReadOnly { get; private set; }

        public long InitialSize { get; private set; }

        public long LimitSize { get; private set; }

        public byte Log { get; private set; }

        public ConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("connectionString");

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // create a dictionary from string name=value collection
            if (connectionString.Contains("="))
            {
                values.ParseKeyValue(connectionString);
            }
            else
            {
                values["filename"] = connectionString;
            }

            // setting values to properties
            this.Filename = GetValue(values, "filename", "");
            this.Journal = GetValue(values, "journal", true);
            this.Password = GetValue<string>(values, "password", null);
            this.CacheSize = GetValue(values, "cache size", 5000);
            this.Timeout = GetValue(values, "timeout", TimeSpan.FromMinutes(1));
            this.AutoCommit = GetValue(values, "auto commit", true);
            this.ReadOnly = GetValue(values, "read only", true);
            this.InitialSize = GetFileSize(GetValue(values, "initial size", BasePage.PAGE_SIZE.ToString()));
            this.LimitSize = GetFileSize(GetValue(values, "limit size", long.MaxValue.ToString()));
            this.Log = GetValue<byte>(values, "log", 0);
        }

        /// <summary>
        /// Get value from _values and convert if exists
        /// </summary>
        private T GetValue<T>(Dictionary<string, string> values, string key, T defaultValue)
        {
            try
            {
                string value;

                if (values.TryGetValue(key, out value))
                {
                    if (typeof(T) == typeof(TimeSpan))
                    {
                        return (T)(object)TimeSpan.Parse(value);
                    }
                    else
                    {
                        return (T)Convert.ChangeType(values[key], typeof(T));
                    }
                }
                else
                {
                    return defaultValue;
                }
            }
            catch (Exception)
            {
                throw new LiteException("Invalid connection string value type for " + key);
            }
        }

        /// <summary>
        /// Get a value from a key converted in file size format: "1gb", "10 mb", "80000"
        /// </summary>
        public long GetFileSize(string size)
        {
            var match = Regex.Match(size, @"^(\d+)\s*([tgmk])?(b|byte|bytes)?$", RegexOptions.IgnoreCase);

            if (!match.Success) return 0;

            var num = Convert.ToInt64(match.Groups[1].Value);

            switch (match.Groups[2].Value.ToLower())
            {
                case "t": return num * 1024L * 1024L * 1024L * 1024L;
                case "g": return num * 1024L * 1024L * 1024L;
                case "m": return num * 1024L * 1024L;
                case "k": return num * 1024L;
                case "": return num;
            }

            return 0;
        }
    }
}