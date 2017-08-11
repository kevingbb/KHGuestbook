using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Data.SqlClient;

namespace sqltest
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            // Gather environment variables
            string sqlserver_name = Environment.GetEnvironmentVariable("SQLSERVER");
            string sql_db = Environment.GetEnvironmentVariable("SQL_DB");
            string sql_id = Environment.GetEnvironmentVariable("SQL_ID");
            string sql_pwd = Environment.GetEnvironmentVariable("SQL_PWD");

            // Build connection string
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = sqlserver_name;
            builder.UserID = sql_id;
            builder.Password = sql_pwd;
            builder.InitialCatalog = sql_db;

            app.Run(async (context) =>
            {
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    await context.Response.WriteAsync("<!DOCTYPE html><html><head><style>table {font-family: arial, sans-serif;border-collapse: collapse;width: 100%;}td, th {border: 1px solid #dddddd;text-align: left;padding: 8px;}tr:nth-child(even) {background-color: #dddddd;}</style></head><body>");
                    connection.Open();
                    String sql = "select * from guestlog order by entrydate DESC";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            await context.Response.WriteAsync("<h1>ASP.NET Core Guestbook v1</h1><table>");
                            await context.Response.WriteAsync("<tr><th>Date</th><th>Name</th><th>Phone</th><th>Sentiment</th><th>Message</th></tr>");
                            while (reader.Read())
                            {
                                String datetime = Convert.ToString(reader.GetDateTime(0));
                                String name = reader.GetString(1);
                                String email = reader.GetString(2);
                                String message = reader.GetString(3);
                                String sentiment = reader.GetString(4);
    
                                await context.Response.WriteAsync("<tr><td>" + datetime + "</td>");
                                await context.Response.WriteAsync("<td>" + name + "</td>");
                                await context.Response.WriteAsync("<td>" + email + "</td>");
                                await context.Response.WriteAsync("<td>" + sentiment + "</td>");
                                await context.Response.WriteAsync("<td>" + message + "</td></tr>");
                            }
                        }
                        await context.Response.WriteAsync("</table></body></html>");
                    }
                }
            });
        }
    }
}
