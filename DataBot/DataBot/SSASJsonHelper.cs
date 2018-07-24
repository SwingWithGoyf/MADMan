using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataBot
{
    public class SSASJsonHelper
    {
        public static DatabaseInfo GetDatabaseInfo(string inputJson)
        {
            DatabaseInfo database = JsonConvert.DeserializeObject<DatabaseInfo>(inputJson);
            return database;
        }
    }

    public class DatabaseInfo
    {
        [JsonProperty("Databases")]
        public DatabaseList DatabaseList { get; set; }
    }

    public class DatabaseList
    {
        [JsonProperty("Database")]
        public List<Database> databases { get; set; }
    }

    public class Database
    {

        [JsonProperty("Name")]
        public string DatabaseName { get; set; }

        [JsonProperty("Model")]
        public Model Model { get; set; }
    }

    public class Model
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("DataSources")]
        public DataSourceList DataSourceList { get; set; }

        [JsonProperty("SSASServer")]
        public string SSASServer { get; set; }

        [JsonProperty("Tables")]
        public TableList TableList { get; set; }

        [JsonProperty("Relationships")]
        public RelationshipList RelationshipList { get; set; }

        [JsonProperty("Users")]
        public List<string> Users { get; set; }
    }

    public class DataSourceList
    {
        [JsonProperty("DataSource")]
        public List<DataSource> dataSources { get; set; }
    }

    public class DataSource
    {
        [JsonProperty("ConnectionString")]
        public string ConnectionString { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }
    }

    public class RelationshipList
    {
        [JsonProperty("Relationship")]
        public List<Relationship> relationships { get; set; }
    }

    public class Relationship
    {
        [JsonProperty("Name")]
        public string RelationshipName { get; set; }

        [JsonProperty("PrimaryKeyTable")]
        public string PrimaryKeyTable { get; set; }

        [JsonProperty("PrimaryKeyTableColumn")]
        public string PrimaryKeyTableColumn { get; set; }

        [JsonProperty("ForeignKeyTable")]
        public string ForeignKeyTable { get; set; }

        [JsonProperty("ForeignKeyTableColumn")]
        public string ForeignKeyTableColumn { get; set; }

        [JsonProperty("OneDirectionCrossFilteringBehavior")]
        public bool OneDirectionCrossFilteringBehavior { get; set; }
    }

    public class TableList
    {
        [JsonProperty("Table")]
        public List<Table> tables { get; set; }
    }

    public class Table
    {
        [JsonProperty("Name")]
        public string TableName { get; set; }

        [JsonProperty("Description")]
        public string TableDescription { get; set; }

        [JsonProperty("DataSourceName")]
        public string DataSourceName { get; set; }

        [JsonProperty("Measures")]
        public MeasureList MeasureList { get; set; }

        [JsonProperty("Partitions")]
        public PartitionList PartitionList { get; set; }

    }

    public class PartitionList
    {
        [JsonProperty("Partition")]
        public List<Partition> partitions { get; set; }
    }

    public class Partition
    {
        [JsonProperty("Name")]
        public string PartitionName { get; set; }

        [JsonProperty("WhereClause")]
        public string WhereClause { get; set; }
    }

    public class MeasureList
    {
        [JsonProperty("Measure")]
        public List<Measure> measures { get; set; }
    }

    public class Measure
    {
        [JsonProperty("Name")]
        public string MeasureName { get; set; }

        [JsonProperty("DAX")]
        public string DAX { get; set; }
    }
}
