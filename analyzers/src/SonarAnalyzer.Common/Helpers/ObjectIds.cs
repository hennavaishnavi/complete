using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace SonarAnalyzer.Helpers
{
    public static class ObjectIds
    {
        private static readonly BlockingCollection<string> Strings;
        public static readonly ObjectIDGenerator Generator = new();

        static ObjectIds()
        {
            Strings = new();
            var enumerator = Strings.GetConsumingEnumerable();
            Task.Factory.StartNew(() =>
            {
                foreach (var s in enumerator)
                {
                    File.AppendAllText(@"E:\Logging.txt", s);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public static long Log<T>(T obj) =>
            Log(obj, obj.ToString());

        public static long Log<T>(T obj, string identifier) =>
            Log(identifier, Generator.GetId(obj, out _));

        public static long Log<T>(T identifier, long id)
        {
            Strings.Add($"Identifier: {identifier}; Id: {id}{Environment.NewLine}");
            return id;
        }
    }
}
