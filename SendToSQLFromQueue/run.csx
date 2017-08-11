using System;
using System.Data.SqlClient;

public static void Run(string myQueueItem, TraceWriter log)
{
    log.Info($"C# Queue trigger function processed: {myQueueItem}");

    // parse queue message
    char[] delimiterChars = { '|' };
    string[] words = myQueueItem.Split(delimiterChars);
    var smsFrom = words[0].Trim();
    var smsBody = words[1].Trim();
    var smsName = "anonymous";
    var smsDate = DateTime.Now.ToString("MM\\/dd\\/yyyy h\\:mm tt");
    var score = words[2].Trim();

    // toggle output
    var writeToSQLAzure = Environment.GetEnvironmentVariable("TOGGLE_SQL_AZURE");
    var writeToK8SSQL = Environment.GetEnvironmentVariable("TOGGLE_K8S_SQL");

    if (writeToSQLAzure == "Y")
        {
            // write values to SQL Azure
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = Environment.GetEnvironmentVariable("SQL_AZURE_IP");
            builder.UserID = Environment.GetEnvironmentVariable("SQL_AZURE_ID");
            builder.Password = Environment.GetEnvironmentVariable("SQL_AZURE_PWD");
            builder.InitialCatalog = Environment.GetEnvironmentVariable("SQL_AZURE_DB");

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();
                String sql = "INSERT INTO guestlog VALUES ('" + smsDate + "', '" + smsName + "', '" + smsFrom + "', '" + smsBody + "', '" + score + "');";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    var rows = command.ExecuteNonQuery();
                    log.Info($"{rows} rows were inserted into SQL Azure");
                }
            }
        }

    if (writeToK8SSQL == "Y")
        {
            // write values to SQL on Linux on Kubernetes
            SqlConnectionStringBuilder builder2 = new SqlConnectionStringBuilder();
            builder2.DataSource = Environment.GetEnvironmentVariable("K8S_SQL_IP");
            builder2.UserID = Environment.GetEnvironmentVariable("K8S_SQL_ID");
            builder2.Password = Environment.GetEnvironmentVariable("K8S_SQL_PWD");
            builder2.InitialCatalog = Environment.GetEnvironmentVariable("K8S_SQL_DB");

            using (SqlConnection connection2 = new SqlConnection(builder2.ConnectionString))
            {
                connection2.Open();
                String sql = "INSERT INTO guestlog VALUES ('" + smsDate + "', '" + smsName + "', '" + smsFrom + "', '" + smsBody + "', '" + score + "');";
                using (SqlCommand command2 = new SqlCommand(sql, connection2))
                {
                    var rows2 = command2.ExecuteNonQuery();
                    log.Info($"{rows2} rows were inserted into SQL on Linux (K8S)");
                }
            }
        }
}