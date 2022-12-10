// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

using Microsoft.Data.Sqlite;

using ( var connection = new SqliteConnection("Data Source=:memory:"))
{
    connection.Open();
    var command = new SqliteCommand("select 1;", connection);
    long result = (long)command.ExecuteScalar()!;

    System.Console.WriteLine(result);
}