﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using NullGuard;

namespace Lern_API.Utilities
{
    public static class Configuration
    {
        public static IConfiguration Config { get; set; } = new ConfigurationBuilder().Build();

        private static string CamelToUpperSnake(string str) => string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToUpperInvariant();

        [return: AllowNull]
        public static T Get<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(nameof(key));

            var env = Environment.GetEnvironmentVariable(CamelToUpperSnake(key));

            if (!string.IsNullOrEmpty(env))
                return (T)Convert.ChangeType(env, typeof(T));

            return Config.GetValue<T>(key);
        }

        [return: AllowNull]
        public static IEnumerable<string> GetList(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(nameof(key));
            
            var env = Environment.GetEnvironmentVariable(CamelToUpperSnake(key));

            if (!string.IsNullOrEmpty(env))
                return env.Split(';');

            return Config.GetSection(key).GetChildren().Select(c => c.Value);
        }
    }
}
