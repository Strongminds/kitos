﻿using System;
using System.Data.SqlClient;
using Infrastructure.DataAccess;

namespace Tools.Test.Database.Model.Tasks
{
    public class DropDatabaseTask : DatabaseTask
    {
        private readonly string _connectionString;

        public DropDatabaseTask(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override bool Execute(KitosContext context)
        {
            //Create a new connection string without initial catalog so that db can be dropped
            var connectionStringBuilder = new SqlConnectionStringBuilder(_connectionString);
            var dbName = connectionStringBuilder.InitialCatalog;
            connectionStringBuilder.Remove("Initial Catalog");

            using var connection = new SqlConnection(connectionStringBuilder.ConnectionString);
            try
            {
                connection.Open();
                using var sqlCommand = connection.CreateCommand();
                var sqlToDropDb =
                    $"if DB_ID('{dbName}') IS NOT NULL " +
                    "BEGIN " +
                        $"alter database [{dbName}] set single_user with rollback immediate " +
                        $"drop database [{dbName}] " +
                    "END ";

                sqlCommand.CommandText = sqlToDropDb;
                sqlCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                connection.Close();
            }

            return true;
        }
    }
}
