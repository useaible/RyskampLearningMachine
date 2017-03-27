using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace MazeGameLib
{
    public class MazeRepo
    {
        private const string CONNECTION_STRING_NAME = "RyskampMazes";
        private const string TABLE = "Mazes";
        private const string DB = "RyskampMazes";

        private string connStr = null;
        internal string ConnStr
        {
            get
            {
                if (connStr == null)
                {
                    if (System.Configuration.ConfigurationManager.ConnectionStrings[CONNECTION_STRING_NAME] == null)
                    {
                        throw new Exception("Connection string for Maze missing from config file");
                    }

                    connStr = System.Configuration.ConfigurationManager.ConnectionStrings[CONNECTION_STRING_NAME].ConnectionString;
                }

                return connStr;
            }
        }

        public SqlConnection GetOpenConnection()
        {
            var conn = new SqlConnection(ConnStr);
            conn.Open();
            return conn;
        }

        public MazeInfo CreateMaze(MazeInfo maze)
        {
            var sql = $"INSERT INTO [{TABLE}] VALUES (@Name, @Metadata); SELECT SCOPE_IDENTITY();";

            using (var conn = GetOpenConnection())
            {
                using (var comm = new SqlCommand(sql, conn))
                {
                    comm.Parameters.Add(new SqlParameter("@Name", maze.Name));
                    comm.Parameters.Add(new SqlParameter("@Metadata", maze.Metadata));
                    maze.ID = Convert.ToInt32(comm.ExecuteScalar());
                }
            }

            return maze;
        }

        public void UpdateMaze(int id, MazeInfo maze)
        {
            var sql = $"UPDATE [{TABLE}] SET [Name] = @Name, [Metadata] = @Metadata WHERE [ID] = @ID";

            using (var conn = GetOpenConnection())
            {
                using (var comm = new SqlCommand(sql, conn))
                {
                    comm.Parameters.Add(new SqlParameter("@ID", id));
                    comm.Parameters.Add(new SqlParameter("@Name", maze.Name));
                    comm.Parameters.Add(new SqlParameter("@Metadata", maze.Metadata));
                    comm.ExecuteNonQuery();
                }
            }
        }

        public MazeInfo GetByID(int id)
        {
            MazeInfo retVal = null;
            var sql = $"SELECT [ID], [Name], [Metadata] FROM [{TABLE}] WHERE [ID] = @ID";

            using (var conn = GetOpenConnection())
            {
                using (var comm = new SqlCommand(sql, conn))
                {
                    comm.Parameters.Add(new SqlParameter("@ID", id));
                    using (var reader = comm.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            retVal = new MazeInfo();
                            retVal.ID = Convert.ToInt32(reader[0]);
                            retVal.Name = reader[1].ToString();
                            retVal.Metadata = reader[2].ToString();
                        }
                    }
                }
            }

            return retVal;
        }

        public IDictionary<int, string> GetIDNameDictionary()
        {
            IDictionary<int, string> retVal = new Dictionary<int, string>();
            var sql = $"SELECT [ID], [Name] FROM [{TABLE}] ORDER BY [Name] ASC";

            using (var conn = GetOpenConnection())
            {
                using (var comm = new SqlCommand(sql, conn))
                {
                    using (var reader = comm.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            retVal.Add(Convert.ToInt32(reader[0]), reader[1].ToString());
                        }
                    }
                }
            }

            return retVal;
        }

        public bool HasDuplicate(string name)
        {
            bool retVal = false;
            var sql = $"SELECT COUNT(*) FROM [{TABLE}] WHERE [Name] = @Name";

            using (var conn = GetOpenConnection())
            {
                using (var comm = new SqlCommand(sql, conn))
                {
                    comm.Parameters.Add(new SqlParameter("@Name", name));
                    int count = Convert.ToInt32(comm.ExecuteScalar());
                    retVal = count >= 1;
                }
            }

            return retVal;
        }

        public bool Delete(int id)
        {
            bool retVal = false;
            int result = -1;
            var sql = $"DELETE FROM [{TABLE}] WHERE [Id] = @Id";

            using (var conn = GetOpenConnection())
            {
                using (var comm = new SqlCommand(sql, conn))
                {
                    comm.Parameters.Add(new SqlParameter("@Id", id));
                    result = comm.ExecuteNonQuery();

                    if (result == 1)
                        retVal = true;
                    else
                        retVal = false;
                }
            }

            return retVal;
        }

        public int Count()
        {
            int retVal = 0;
            var sql = $"SELECT COUNT(*) FROM [{TABLE}]";

            using (var conn = GetOpenConnection())
            {
                using (var comm = new SqlCommand(sql, conn))
                {
                    retVal = Convert.ToInt32(comm.ExecuteScalar());
                }
            }

            return retVal;
        }

        public void CreateDBSchemaIfNotExist()
        {
            var createDbSql = new StringBuilder();
            createDbSql.AppendLine($"IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{DB}') BEGIN");
            createDbSql.AppendLine($"CREATE DATABASE [{DB}];");
            createDbSql.AppendLine("END");

            var createTableSql = new StringBuilder();
            createTableSql.AppendLine($"IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{TABLE}') BEGIN");
            createTableSql.AppendLine($"CREATE TABLE [{TABLE}] ( [ID] [int] not null identity primary key, [Name] [nvarchar](100) not null unique, [Metadata] [nvarchar](max) null );");
            createTableSql.AppendLine("END");

            using (var tableConnection = new SqlConnection(ConnStr))
            {
                using (var serverConnection = new SqlConnection($"Server={tableConnection.DataSource};Integrated Security=True;"))
                {
                    serverConnection.Open();
                    using (var comm = new SqlCommand(createDbSql.ToString(), serverConnection))
                    {
                        comm.ExecuteNonQuery();
                    }

                    tableConnection.Open();
                    using (var comm = new SqlCommand(createTableSql.ToString(), tableConnection))
                    {
                        comm.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
