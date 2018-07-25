using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AnalysisServices;
using Microsoft.AnalysisServices.Tabular;
using System.IO;
using Microsoft.AnalysisServices.AdomdClient;
using System.Data.OleDb;

namespace DataBot
{
    public enum DeviceType
    {
        Hololens,
        Oasis
    }

    public static class SSASTabularModel
    {
        const string ProviderString = "Provider=SQLNCLI11;";

        private static Microsoft.AnalysisServices.Tabular.Partition CreatePartition(Microsoft.AnalysisServices.Tabular.Database database, string name, string dataSourceName, string query)
        {
            Microsoft.AnalysisServices.Tabular.Partition partition = new Microsoft.AnalysisServices.Tabular.Partition();
            partition.Name = name;
            partition.Source = new QueryPartitionSource()
            {
                DataSource = database.Model.DataSources.Where(s => s.Name == dataSourceName).FirstOrDefault(),
                Query = query,
            };

            return partition;
        }

        private static Microsoft.AnalysisServices.Tabular.Database CreateDatabase(string databaseName)
        {
            var database = new Microsoft.AnalysisServices.Tabular.Database()
            {
                Name = databaseName,
                ID = databaseName,
                CompatibilityLevel = 1200,
                StorageEngineUsed = StorageEngineUsed.TabularMetadata,
            };

            return database;
        }

        private static Microsoft.AnalysisServices.Tabular.Model CreateModel(string modelName, string modelDescription)
        {
            var model = new Microsoft.AnalysisServices.Tabular.Model()
            {
                Name = modelName,
                Description = modelDescription
            };

            return model;
        }

        private static Microsoft.AnalysisServices.Tabular.ProviderDataSource CreateDataSource(string dataSourceName, string connectionString)
        {
            var dataSource = new Microsoft.AnalysisServices.Tabular.ProviderDataSource()
            {
                Name = dataSourceName,
                Description = $"A data source definition for {dataSourceName}",
                ConnectionString = connectionString,
                ImpersonationMode = Microsoft.AnalysisServices.Tabular.ImpersonationMode.ImpersonateServiceAccount
            };

            return dataSource;
        }

        private static Microsoft.AnalysisServices.Tabular.Measure CreateMeasure(string measureName, string daxExpression, string description = null, string displayFolder = null)
        {
            var measure = new Microsoft.AnalysisServices.Tabular.Measure()
            {
                Name = measureName,
                Expression = daxExpression,
                Description = description,
                DisplayFolder = displayFolder

            };

            return measure;
        }

        private static Microsoft.AnalysisServices.Tabular.ModelRole CreateRole(Microsoft.AnalysisServices.Tabular.Database database, string roleName, List<string> readrefreshUsers)
        {
            Microsoft.AnalysisServices.Tabular.ModelRole role = new ModelRole()
            {
                Name = roleName,
                ModelPermission = ModelPermission.ReadRefresh,
                Description = roleName
            };

            foreach (string user in readrefreshUsers)
            {
                WindowsModelRoleMember member = new Microsoft.AnalysisServices.Tabular.WindowsModelRoleMember()
                {
                    MemberName = user
                };

                role.Members.Add(member);
            }

            return role;
        }

        private static Microsoft.AnalysisServices.Tabular.Table CreateTable(string tableName, string tableDescription)
        {
            var table = new Microsoft.AnalysisServices.Tabular.Table()
            {
                Name = tableName,
                Description = tableDescription
            };

            return table;
        }

        private static Microsoft.AnalysisServices.Tabular.Relationship CreateRelationship(Microsoft.AnalysisServices.Tabular.Database database, string relationName, Column fromColumn, Column toColumn, CrossFilteringBehavior filterDirection)
        {
            var relationship = new Microsoft.AnalysisServices.Tabular.SingleColumnRelationship()
            {
                Name = relationName,
                FromColumn = fromColumn,
                ToColumn = toColumn,
                FromCardinality = RelationshipEndCardinality.Many,
                ToCardinality = RelationshipEndCardinality.One,
                CrossFilteringBehavior = filterDirection
            };

            return relationship;
        }

        private static string GetStringForGroupBy(string tableName, Dictionary<string, object> filterKeyValuePairs)
        {
            StringBuilder stbr = new StringBuilder();
            foreach (var k in filterKeyValuePairs)
            {
                if (k.Key == "Is Mainstream")
                {
                    stbr.Append($"'{tableName}'[" + k.Key + "] = " + k.Value);
                }
                else
                {
                    stbr.Append($"'{tableName}'[" + k.Key + "] = \"" + k.Value + "\"");
                }
                stbr.Append(" && ");
            }

            string str = stbr.ToString();
            str = str.TrimEnd();
            str = str.Remove(str.LastIndexOf("&&"));

            return $"FILTER('{tableName}', {str})";
        }

        public static void CreateNewMadMeasure(string measureName, string measureDescription, Dictionary<string, object> filterKeyValuePairs, DeviceType deviceType)
        {
            var algtelPassword = KeyVaultUtil.GetSecretInPlaintext(KeyVaultUtil.SharedAccountName);

            string userId = KeyVaultUtil.SharedAccountName + "@microsoft.com";

            string ssasServer = "asazure://centralus.asazure.windows.net/datapipelinesaas";

            string ConnectionString = $"Password={algtelPassword};Persist Security Info=True;User ID={userId};Data Source = " + ssasServer + ";";

            using (Microsoft.AnalysisServices.Tabular.Server server = new Microsoft.AnalysisServices.Tabular.Server())
            {
                server.Connect(ConnectionString);

                string databaseName = deviceType == DeviceType.Hololens ? "HoloLensHackathon" : "OasisHackathon";

                Microsoft.AnalysisServices.Tabular.Database tabularDatabase = null;

                tabularDatabase = server.Databases.FindByName(databaseName);

                string tableName = deviceType == DeviceType.Hololens ? "HoloLens Product Engagement" : "Oasis Product Engagement";

                var oasisProductEngagamentTable = tabularDatabase.Model.Tables.Find(tableName);

                var newMadMeasure = oasisProductEngagamentTable.Measures.Where(e => e.Name == measureName).FirstOrDefault();

                if (newMadMeasure != null)
                {
                    oasisProductEngagamentTable.Measures.Remove(newMadMeasure);
                }

                StringBuilder stbr = new StringBuilder();
                foreach (var k in filterKeyValuePairs)
                {
                    if (k.Key == "Is Mainstream")
                    {
                        stbr.Append($"'{tableName}'[" + k.Key + "] = " + k.Value);
                    }
                    else
                    {
                        stbr.Append($"'{tableName}'[" + k.Key + "] = \"" + k.Value + "\"");
                    }

                    stbr.Append(" , ");
                }

                string str = stbr.ToString();
                str = str.TrimEnd();
                str = str.Remove(str.LastIndexOf(","));

                string oldMadMeasureName = deviceType == DeviceType.Hololens ? "MAD (R28)" : "PC MAD (R28)";

                var oldMadMeasure = oasisProductEngagamentTable.Measures.Where(e => e.Name == oldMadMeasureName).FirstOrDefault();

                string newMadMeasureExpression = $"{oldMadMeasure.Expression.Remove(oldMadMeasure.Expression.LastIndexOf(")"))}, {str})";

                string displayFolder = deviceType == DeviceType.Hololens ? @"[Measures]\Device Count" : @"[Measures]\PC Counting";

                oasisProductEngagamentTable.Measures.Add(CreateMeasure(measureName, newMadMeasureExpression, measureDescription, displayFolder));

                tabularDatabase.Model.SaveChanges();
            }
        }

        public static void CreateNewDadMeasure(string measureName, string measureDescription, Dictionary<string, object> filterKeyValuePairs, DeviceType deviceType)
        {
            var algtelPassword = KeyVaultUtil.GetSecretInPlaintext(KeyVaultUtil.SharedAccountName);

            string userId = KeyVaultUtil.SharedAccountName + "@microsoft.com";

            string ssasServer = "asazure://centralus.asazure.windows.net/datapipelinesaas";

            string ConnectionString = $"Password={algtelPassword};Persist Security Info=True;User ID={userId};Data Source = " + ssasServer + ";";

            using (Microsoft.AnalysisServices.Tabular.Server server = new Microsoft.AnalysisServices.Tabular.Server())
            {
                server.Connect(ConnectionString);

                string databaseName = deviceType == DeviceType.Hololens ? "HoloLensHackathon" : "OasisHackathon";

                Microsoft.AnalysisServices.Tabular.Database tabularDatabase = null;

                tabularDatabase = server.Databases.FindByName(databaseName);

                string tableName = deviceType == DeviceType.Hololens ? "HoloLens Product Engagement" : "Oasis Product Engagement";

                var oasisProductEngagementTable = tabularDatabase.Model.Tables.Find(tableName);

                var newDadMeasure = oasisProductEngagementTable.Measures.Where(e => e.Name == measureName).FirstOrDefault();

                if (newDadMeasure != null)
                {
                    oasisProductEngagementTable.Measures.Remove(newDadMeasure);
                }

                StringBuilder stbr = new StringBuilder();
                foreach (var k in filterKeyValuePairs)
                {
                    if (k.Key == "Is Mainstream")
                    {
                        stbr.Append($"'{tableName}'[" + k.Key + "] = " + k.Value);
                    }
                    else
                    {
                        stbr.Append($"'{tableName}'[" + k.Key + "] = \"" + k.Value + "\"");
                    }

                    stbr.Append(" , ");
                }

                string str = stbr.ToString();
                str = str.TrimEnd();
                str = str.Remove(str.LastIndexOf(","));

                string oldDadMeasureName = deviceType == DeviceType.Hololens ? "DAD (R28)" : "PC DAD (R28)";

                var oldDadMeasure = oasisProductEngagementTable.Measures.Where(e => e.Name == oldDadMeasureName).FirstOrDefault();

                string newDadMeasureExpression = string.Empty;

                if (deviceType == DeviceType.Hololens)
                {
                    newDadMeasureExpression = GetLatestDadExpression(oldDadMeasure.Expression, filterKeyValuePairs, tableName);
                }
                else
                {
                    string oldDadMeasureExpression = oldDadMeasure.Expression;
                    oldDadMeasureExpression = oldDadMeasureExpression.Substring(oldDadMeasureExpression.IndexOf("AVERAGEX"));
                    oldDadMeasureExpression = oldDadMeasureExpression.Remove(oldDadMeasureExpression.LastIndexOf(")"));

                    newDadMeasureExpression = GetLatestDadExpression(oldDadMeasureExpression, filterKeyValuePairs, tableName);
                }

                string displayFolder = deviceType == DeviceType.Hololens ? @"[Measures]\Device Count" : @"[Measures]\PC Counting";

                oasisProductEngagementTable.Measures.Add(CreateMeasure(measureName, newDadMeasureExpression, measureDescription, displayFolder));

                tabularDatabase.Model.SaveChanges();
            }
        }

        private static string GetLatestDadExpression(string oldExpression, Dictionary<string, object> dict, string tableName)
        {
            var indexToReplace = oldExpression.LastIndexOf(",");
            string s = oldExpression.Substring(indexToReplace + 1);
            s = s.Remove(s.LastIndexOf(")"));

            string newQuery = $", CALCULATE({s}, ";

            StringBuilder stbr = new StringBuilder();
            foreach (var k in dict)
            {
                if (k.Key == "Is Mainstream")
                {
                    stbr.Append($"'{tableName}'[" + k.Key + "] = " + k.Value);
                }
                else
                {
                    stbr.Append($"'{tableName}'[" + k.Key + "] = \"" + k.Value + "\"");
                }
                stbr.Append(" , ");
            }

            string str = stbr.ToString();
            str = str.TrimEnd();
            str = str.Remove(str.LastIndexOf(","));

            newQuery += str + "))";

            return oldExpression.Substring(0, indexToReplace) + newQuery;
        }


        public static double ExecuteQuery(string queryString, string userId, string password, string tableName, string ssasServer, Dictionary<string, object> kvp, string databaseName)
        {
            double madDad = 0;
            string connectionString = $"Provider=MSOLAP;Data Source={ssasServer};Initial Catalog={databaseName};User ID = {userId};Password = {password};Persist Security Info=True; Impersonation Level=Impersonate;";

            if (!kvp.Any())
            {
                queryString = $"EVALUATE SUMMARIZE('{tableName}', \"Measure name\"," + queryString + ")";
            }
            else
            {
                StringBuilder stbr = new StringBuilder();
                foreach (var k in kvp)
                {
                    if (k.Key == "Is Mainstream")
                    {
                        stbr.Append($"'{tableName}'[" + k.Key + "] = " + k.Value);
                    }
                    else
                    {
                        stbr.Append($"'{tableName}'[" + k.Key + "] = \"" + k.Value + "\"");
                    }
                    stbr.Append(" && ");
                }

                string str = stbr.ToString();
                str = str.TrimEnd();
                str = str.Remove(str.LastIndexOf("&&"));

                queryString = $"EVALUATE SUMMARIZE(FILTER('{tableName}', {str}), \"Measure name\"," + queryString + ")";
            }

            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();
                using (var command = new OleDbCommand(queryString, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            madDad = double.Parse(reader[0].ToString());
                        }
                    }
                }
            }

            return madDad;
        }

        public static SortedDictionary<string, double> ExecuteGroupByMad(List<string> slicerList, Dictionary<string, object> filterList, DeviceType deviceType)
        {
            SortedDictionary<string, double> madDictionary = new SortedDictionary<string, double>();

            var algtelPassword = KeyVaultUtil.GetSecretInPlaintext(KeyVaultUtil.SharedAccountName);

            string userId = KeyVaultUtil.SharedAccountName + "@microsoft.com";

            string ssasServer = "asazure://centralus.asazure.windows.net/datapipelinesaas";

            string ConnectionString = $"Password={algtelPassword};Persist Security Info=True;User ID={userId};Data Source = " + ssasServer + ";";

            using (Microsoft.AnalysisServices.Tabular.Server server = new Microsoft.AnalysisServices.Tabular.Server())
            {
                server.Connect(ConnectionString);

                string databaseName = deviceType == DeviceType.Hololens ? "HoloLensHackathon" : "OasisHackathon";

                Microsoft.AnalysisServices.Tabular.Database tabularDatabase = null;

                tabularDatabase = server.Databases.FindByName(databaseName);

                string tableName = deviceType == DeviceType.Hololens ? "HoloLens Product Engagement" : "Oasis Product Engagement";

                var oasisProductEngagamentTable = tabularDatabase.Model.Tables.Find(tableName);

                string measureName = deviceType == DeviceType.Hololens ? "MAD (R28)" : "PC MAD (R28)";

                var madMeasure = oasisProductEngagamentTable.Measures.Where(e => e.Name == measureName).FirstOrDefault();

                StringBuilder stbr = new StringBuilder();

                foreach (var slicer in slicerList)
                {
                    stbr.Append($"'{tableName}'[" + slicer + "]");
                    stbr.Append(" , ");
                }

                string str = stbr.ToString();
                str = str.TrimEnd();
                str = str.Remove(str.LastIndexOf(","));

                string newTableName = (filterList.Count > 0) ? GetStringForGroupBy(tableName, filterList) : $"'{tableName}'";

                string queryString = $"EVALUATE SUMMARIZE({newTableName}, {str}, \"Group by measure\"," + madMeasure.Expression + ")";

                string msolapConnectionString = $"Provider=MSOLAP;Data Source={ssasServer};Initial Catalog={databaseName};User ID = {userId};Password = {algtelPassword};Persist Security Info=True; Impersonation Level=Impersonate;";

                using (var connection = new OleDbConnection(msolapConnectionString))
                {
                    connection.Open();
                    using (var command = new OleDbCommand(queryString, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            int columns = reader.FieldCount;

                            while (reader.Read())
                            {
                                string keyString = string.Empty;
                                for (int i = 0; i < columns - 1; i++)
                                {
                                    keyString += reader[i].ToString() + ", ";
                                }

                                keyString = keyString.TrimEnd();
                                keyString = keyString.Remove(keyString.LastIndexOf(","));

                                madDictionary.Add(keyString, double.Parse(reader[columns - 1].ToString()));
                            }
                        }
                    }
                }
            }

            return madDictionary;
        }

        public static SortedDictionary<string, double> ExecuteGroupByDad(List<string> slicerList, Dictionary<string, object> filterList, DeviceType deviceType)
        {
            SortedDictionary<string, double> dadDictionary = new SortedDictionary<string, double>();

            var algtelPassword = KeyVaultUtil.GetSecretInPlaintext(KeyVaultUtil.SharedAccountName);

            string userId = KeyVaultUtil.SharedAccountName + "@microsoft.com";

            string ssasServer = "asazure://centralus.asazure.windows.net/datapipelinesaas";

            string ConnectionString = $"Password={algtelPassword};Persist Security Info=True;User ID={userId};Data Source = " + ssasServer + ";";

            using (Microsoft.AnalysisServices.Tabular.Server server = new Microsoft.AnalysisServices.Tabular.Server())
            {
                server.Connect(ConnectionString);

                string databaseName = deviceType == DeviceType.Hololens ? "HoloLensHackathon" : "OasisHackathon";

                Microsoft.AnalysisServices.Tabular.Database tabularDatabase = null;

                tabularDatabase = server.Databases.FindByName(databaseName);

                string tableName = deviceType == DeviceType.Hololens ? "HoloLens Product Engagement" : "Oasis Product Engagement";

                var oasisProductEngagamentTable = tabularDatabase.Model.Tables.Find(tableName);

                string measureName = deviceType == DeviceType.Hololens ? "DAD (R28)" : "PC DAD (R28)";

                var dadMeasure = oasisProductEngagamentTable.Measures.Where(e => e.Name == measureName).FirstOrDefault();

                StringBuilder stbr = new StringBuilder();

                foreach (var slicer in slicerList)
                {
                    stbr.Append($"'{tableName}'[" + slicer + "]");
                    stbr.Append(" , ");
                }

                string str = stbr.ToString();
                str = str.TrimEnd();
                str = str.Remove(str.LastIndexOf(","));

                string newTableName = (filterList.Count > 0) ? GetStringForGroupBy(tableName, filterList) : $"'{tableName}'";

                string queryString = $"EVALUATE SUMMARIZE({newTableName}, {str}, \"Group by measure\"," + dadMeasure.Expression + ")";

                string msolapConnectionString = $"Provider=MSOLAP;Data Source={ssasServer};Initial Catalog={databaseName};User ID = {userId};Password = {algtelPassword};Persist Security Info=True; Impersonation Level=Impersonate;";

                using (var connection = new OleDbConnection(msolapConnectionString))
                {
                    connection.Open();
                    using (var command = new OleDbCommand(queryString, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            int columns = reader.FieldCount;

                            while (reader.Read())
                            {
                                string keyString = string.Empty;
                                for (int i = 0; i < columns - 1; i++)
                                {
                                    keyString += reader[i].ToString() + ", ";
                                }

                                keyString = keyString.TrimEnd();
                                keyString = keyString.Remove(keyString.LastIndexOf(","));

                                double value = double.Parse(reader[columns - 1].ToString());
                                value = Math.Round(value, 0);
                                dadDictionary.Add(keyString, value);
                            }
                        }
                    }
                }
            }

            return dadDictionary;
        }

        public static double GetMadNumber(Dictionary<string, object> kvp, DeviceType deviceType)
        {
            var algtelPassword = KeyVaultUtil.GetSecretInPlaintext(KeyVaultUtil.SharedAccountName);

            string userId = KeyVaultUtil.SharedAccountName + "@microsoft.com";

            string ssasServer = "asazure://centralus.asazure.windows.net/datapipelinesaas";

            string ConnectionString = $"Password={algtelPassword};Persist Security Info=True;User ID={userId};Data Source = " + ssasServer + ";";
            double mad = 0;

            using (Microsoft.AnalysisServices.Tabular.Server server = new Microsoft.AnalysisServices.Tabular.Server())
            {
                server.Connect(ConnectionString);

                string databaseName = deviceType == DeviceType.Hololens ? "HoloLensHackathon" : "OasisHackathon";

                Microsoft.AnalysisServices.Tabular.Database tabularDatabase = null;

                tabularDatabase = server.Databases.FindByName(databaseName);

                string tableName = deviceType == DeviceType.Hololens ? "HoloLens Product Engagement" : "Oasis Product Engagement";

                var oasisProductEngagamentTable = tabularDatabase.Model.Tables.Find(tableName);

                string measureName = deviceType == DeviceType.Hololens ? "MAD (R28)" : "PC MAD (R28)";

                var madMeasure = oasisProductEngagamentTable.Measures.Where(e => e.Name == measureName).FirstOrDefault();

                Console.WriteLine(madMeasure.Expression);

                mad = ExecuteQuery(madMeasure.Expression, userId, algtelPassword, oasisProductEngagamentTable.Name, ssasServer, kvp, databaseName);
            }

            return mad;
        }

        public static double GetDadNumber(Dictionary<string, object> kvp, DeviceType deviceType)
        {
            var algtelPassword = KeyVaultUtil.GetSecretInPlaintext(KeyVaultUtil.SharedAccountName);

            string userId = KeyVaultUtil.SharedAccountName + "@microsoft.com";

            string ssasServer = "asazure://centralus.asazure.windows.net/datapipelinesaas";

            string ConnectionString = $"Password={algtelPassword};Persist Security Info=True;User ID={userId};Data Source = " + ssasServer + ";";
            double dad = 0;

            using (Microsoft.AnalysisServices.Tabular.Server server = new Microsoft.AnalysisServices.Tabular.Server())
            {
                server.Connect(ConnectionString);

                string databaseName = deviceType == DeviceType.Hololens ? "HoloLensHackathon" : "OasisHackathon";

                Microsoft.AnalysisServices.Tabular.Database tabularDatabase = null;

                tabularDatabase = server.Databases.FindByName(databaseName);

                string tableName = deviceType == DeviceType.Hololens ? "HoloLens Product Engagement" : "Oasis Product Engagement";

                var oasisProductEngagamentTable = tabularDatabase.Model.Tables.Find(tableName);

                string measureName = deviceType == DeviceType.Hololens ? "DAD (R28)" : "PC DAD (R28)";

                var dadMeasure = oasisProductEngagamentTable.Measures.Where(e => e.Name == measureName).FirstOrDefault();

                Console.WriteLine(dadMeasure.Expression);

                dad = ExecuteQuery(dadMeasure.Expression, userId, algtelPassword, oasisProductEngagamentTable.Name, ssasServer, kvp, databaseName);
                dad = Math.Round(dad, 0);
            }

            return dad;
        }
    }
}