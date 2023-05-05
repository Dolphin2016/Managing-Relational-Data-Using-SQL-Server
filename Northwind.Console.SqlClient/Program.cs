using Microsoft.Data.SqlClient;
using System.Data;

SqlConnectionStringBuilder builder = new();

builder.InitialCatalog = "Northwind";
builder.MultipleActiveResultSets = true;
builder.Encrypt = true;
builder.TrustServerCertificate = true;
builder.ConnectTimeout = 10;

WriteLine("Connect to:");
WriteLine(" 1 - SQL Server on local machine");
WriteLine(" 2 - Azure SQL Database");
WriteLine(" 3 - Azure SQL Edge");
WriteLine();
Write("Press a key: ");

ConsoleKey key = ReadKey().Key;
WriteLine();
WriteLine();

if (key is ConsoleKey.D1 or ConsoleKey.NumPad1)
{
    builder.DataSource = "eugene-pc\\dev"; // Local SQL Server @".\net7book";
}
else if (key is ConsoleKey.D2 or ConsoleKey.NumPad2)
{
    // Azure SQL Database
    builder.DataSource = "tcp:apps-services-net7.database.windows.net,1433";
}
else if (key is ConsoleKey.D3 or ConsoleKey.NumPad3)
{
    // Azure SQL Edge
    builder.DataSource = "tcp:127.0.0.1,1433";
}
else
{
    WriteLine("No data source selected.");
    return;
}

WriteLine("Authenticate using:");
WriteLine(" 1 - Windows Integrated Security");
WriteLine(" 2 - SQL Login, for example, sa");
WriteLine();
Write("Press a key: ");

key = ReadKey().Key;
WriteLine();
WriteLine();

if (key is ConsoleKey.D1 or ConsoleKey.NumPad1)
{
    builder.IntegratedSecurity = true;
}
else if (key is ConsoleKey.D2 or ConsoleKey.NumPad2)
{
    builder.UserID = "sa"; // Azure SQL Edge

    Write("Enter your SQL Server password: ");
    string? password = ReadLine();
    if (string.IsNullOrWhiteSpace(password))
    {
        WriteLine("Password cannot be empty or null");
        return;
    }

    builder.Password = password;
    builder.PersistSecurityInfo = false;
}
else
{
    WriteLine("No authentication selected.");
    return;
}

SqlConnection connection = new(builder.ConnectionString);

WriteLine(connection.ConnectionString);
WriteLine();

connection.StateChange += Connection_StateChange;
connection.InfoMessage += Connection_InfoMessage;

try
{
    WriteLine("Opening connection. Please wait up to {0} seconds...", builder.ConnectTimeout);
    WriteLine();
    // Making the connection asynchronous
    await connection.OpenAsync();

    WriteLine($"SQL Server version: {connection.ServerVersion}");

    connection.StatisticsEnabled = true;
}
catch (SqlException ex)
{
    WriteLine($"SQL exception: {ex.Message}");
    return;
}

Write("Enter a unit price: ");
string? priceText = ReadLine();

if (!decimal.TryParse(priceText, out decimal price))
{
    WriteLine("You must enter a valid unit price.");
    return;
}

SqlCommand cmd = connection.CreateCommand();

cmd.CommandType = CommandType.Text;
cmd.CommandText = "SELECT ProductId, ProductName, UnitPrice FROM Products"
    + " WHERE UnitPrice > @price";

cmd.Parameters.AddWithValue("price", price);

// Making the command asynchronous
SqlDataReader r = await cmd.ExecuteReaderAsync();

WriteLine("----------------------------------------------------------");
WriteLine("| {0,5} | {1,-35} | {2,8} |", "Id", "Name", "Price");
WriteLine("----------------------------------------------------------");

// Making the loop asynchronous
while (await r.ReadAsync())
{
    WriteLine("| {0,5} | {1,-35} | {2,8:C} |",
        await r.GetFieldValueAsync<int>("ProductId"),
        await r.GetFieldValueAsync<string>("ProductName"),
        await r.GetFieldValueAsync<decimal>("UnitPrice"));
}

WriteLine("----------------------------------------------------------");

await r.CloseAsync();

await connection.CloseAsync();
