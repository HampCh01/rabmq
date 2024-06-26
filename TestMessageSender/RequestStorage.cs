using TestMessageSender.Models;
using C3MSFramework.DataAccess;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json;

namespace TestMessageSender;

public class OpenRequests
{
    private readonly string SQL_DB = Environment.GetEnvironmentVariable("SQL_DB") ?? string.Empty;
    private readonly string SQL_CHKPASS = Environment.GetEnvironmentVariable("SQL_CHKPASS") ?? string.Empty;
    private readonly string SQL_APPNAME = Environment.GetEnvironmentVariable("SQL_APPNAME") ?? string.Empty;

    public Enumeration.Environment env = Enumeration.Environment.Test;

    private string GetConnString(){
        return C3MSFramework.Framework.GetConnectionString(SQL_APPNAME, env, SQL_DB, SQL_CHKPASS);
    }

    private string query = File.ReadAllText("Scripts/SELECT-OpenRecords.sql");

    public List<OpenRequestModel> GetOpenRecords()
    {

        using (SqlConnection connection = new SqlConnection(GetConnString()))
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    List<OpenRequestModel> requests = new();
                    while (reader.Read())
                    {
                        OpenRequestModel req = new OpenRequestModel
                        {
                            LeadListRequestID = Convert.ToInt32(reader["LeadListRequestID"]),
                            RequestID = Convert.ToInt32(reader["RequestID"]),
                            CourtID = Convert.ToInt32(reader["CourtID"]),
                            FileTypeID = reader["FileTypeID"].ToString(),
                            ReqContent = JsonSerializer.Deserialize<LTRecordModel>(reader["ReqContent"].ToString())
                        };
                        requests.Add(req);
                    }
                    return requests;
                }
            }
        }
    }
}
