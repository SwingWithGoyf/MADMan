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

        public static void CreateNewMeasure(string measureName, string measureDescription, Dictionary<string, object> filterKeyValuePairs, DeviceType deviceType)
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
                    stbr.Append($"'{tableName}'[" + k.Key + "] = \"" + k.Value + "\"");
                    stbr.Append(" , ");
                }

                string str = stbr.ToString();
                str = str.TrimEnd();
                str = str.Remove(str.LastIndexOf(","));

                string newMadMeasureExpression = $"CALCULATE ( [Unique PCs],  DATESBETWEEN(Dates[Date ], MAX(Dates[Current Period Start]), MAX(Dates[Date ])), {str})";

                oasisProductEngagamentTable.Measures.Add(CreateMeasure(measureName, newMadMeasureExpression, measureDescription, @"[Measures]\PC Counting"));

                tabularDatabase.Model.SaveChanges();
            }
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
            }

            return dad;
        }
    }
}