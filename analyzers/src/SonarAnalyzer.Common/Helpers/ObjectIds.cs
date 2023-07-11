using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace SonarAnalyzer.Helpers
{
    public static class ObjectIds
    {
        private static readonly object Locker = new object();
        public static readonly ObjectIDGenerator Generator = new();

        public static long Log<T>(T obj) =>
            Log(obj, obj.ToString());

        public static long Log<T>(T obj, string identifier) =>
            Log(identifier, Generator.GetId(obj, out _));

        public static long Log<T>(T identifier, long id)
        {
            lock (Locker)
            {
                File.AppendAllText(@"E:\Logging.txt", $"Identifier: {identifier}; Id: {id}{Environment.NewLine}");
            }
            return id;
        }
    }
}
