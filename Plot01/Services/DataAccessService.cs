using System;
using System.Collections.Generic;
using System.Data;
using Oracle.DataAccess.Client;

namespace BatchPlot.Services
{
    public class DataAccessService
    {
        private readonly string _connectionString;

        public DataAccessService(string connectionString)
        {
            _connectionString = connectionString;
        }

        //public void IterateOverReader(string query, IEnumerable<KeyValuePair<string, object>> parameters, Action<IDataReader> mapReader)
        //{
        //    using (var connection = new OracleConnection(_connectionString))
        //    using (var command = new OracleCommand(query, connection))
        //    {
        //        if (parameters != null && parameters.Count() > 0)
        //        {
        //            parameters.ToList().ForEach(x => command.Parameters.AddWithValue(x.Key, x.Value ?? DBNull.Value));
        //        }
        //        command.Connection.Open();
        //        var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
        //        while (reader.Read())
        //        {
        //            mapReader(reader);
        //        }
        //    }
        //}

        //public IEnumerable<T> IterateOverReader<T>(string query, Func<IDataReader, T> mapReader)
        //{
        //    return IterateOverReader<T>(query, null, mapReader);
        //}

        public IEnumerable<T> IterateOverReader<T>(string query, Func<IDataReader, T> mapReader)
        {
            var list = new List<T>();
            using (var connection = new OracleConnection(_connectionString))
            using (var command = new OracleCommand(query, connection))
            {
                command.Connection.Open();
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                while (reader.Read())
                {
                    list.Add(mapReader(reader));
                }
            }
            return list;
        }

        public int ExecuteCommand(string query)
        {
            using (var connection = new OracleConnection(_connectionString))
            using (var command = new OracleCommand(query, connection))
            {
                command.Connection.Open();
                command.CommandTimeout = 600;
                return command.ExecuteNonQuery();
            }
        }

        //public int ExecuteCommand(string query, IEnumerable<KeyValuePair<string, object>> parameters)
        //{
        //    using (var connection = new OracleConnection(_connectionString))
        //    using (var command = new OracleCommand(query, connection))
        //    {
        //        if (parameters != null && parameters.Count() > 0)
        //        {
        //            parameters.ToList().ForEach(x => command.Parameters.AddWithValue(x.Key, x.Value ?? DBNull.Value));
        //        }
        //        command.Connection.Open();
        //        command.CommandTimeout = 600;
        //        return command.ExecuteNonQuery();
        //    }
        //}

        //public T ExecuteCommand<T>(string query, IEnumerable<KeyValuePair<string, object>> parameters)
        //{
        //        using (var connection = new OracleConnection(_connectionString))
        //        using (var command = new OracleCommand(query, connection))
        //        {
        //            if (parameters != null && parameters.Count() > 0)
        //            {
        //                parameters.ToList().ForEach(x => command.Parameters.AddWithValue(x.Key, x.Value ?? DBNull.Value));
        //            }
        //            command.Connection.Open();
        //            command.CommandTimeout = 600;
        //            return (T)command.ExecuteScalar();
        //        }
        //}

        //public void ClearDatabaseCaches()
        //{
        //    ExecuteCommand("alter system flush buffer_cache;");
        //    ExecuteCommand("alter system flush shared_pool;");
        //}
    }
}
