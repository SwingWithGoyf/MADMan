using Microsoft.AnalysisServices.Tabular;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Globalization;

namespace DataBot
{
    public static class DatabaseHelper
    {
        public static Dictionary<string, Microsoft.AnalysisServices.Tabular.DataType> GetMappedColumnDataTypesForaTable(string connectionString, string tableName)
        {
            string query = @"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = '" + tableName + "'";

            Dictionary<string, DataType> mapDataTypes = new Dictionary<string, DataType>();

            SqlCommand command = null;

            using (var connection = new SqlConnection(
             connectionString
             ))
            {

                command = new SqlCommand(query, connection);
                try
                {
                    command.Connection.Open();
                    command.CommandTimeout = 0;
                    using (SqlDataReader oReader = command.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            string columnName = oReader["COLUMN_NAME"].ToString();
                            string dataType = oReader["DATA_TYPE"].ToString();
                            DataType mappedDataType = MapDataType(dataType);
                            mapDataTypes.Add(columnName, mappedDataType);
                        }
                    }
                }
                finally
                {
                    command.Connection.Close();
                }
            }

            return mapDataTypes;
        }

        private static Microsoft.AnalysisServices.Tabular.DataType MapDataType(string dataType)
        {
            switch (dataType)
            {
                case "bigint":
                    return DataType.Int64;
                case "binary":
                    return DataType.Binary;
                case "bit":
                    return DataType.Boolean;
                case "char":
                    return DataType.String;
                case "date":
                case "datetime":
                case "datetime2":
                    return DataType.DateTime;
                case "decimal":
                    return DataType.Decimal;
                case "float":
                    return DataType.Double;
                case "int":
                    return DataType.Int64;
                case "tinyint":
                    return DataType.Int64;
                case "money":
                    return DataType.Decimal;
                case "nchar":
                    return DataType.String;
                case "ntext":
                    return DataType.String;
                case "numeric":
                    return DataType.Decimal;
                case "nvarchar":
                    return DataType.String;
                case "smalldatetime":
                    return DataType.DateTime;
                case "smallint":
                    return DataType.Int64;
                case "smallmoney":
                    return DataType.Decimal;
                case "text":
                    return DataType.String;
                case "uniqueidentifier":
                    return DataType.String;
                case "varchar":
                    return DataType.String;
                default:
                    return DataType.Unknown;
            }
        }
    }
}