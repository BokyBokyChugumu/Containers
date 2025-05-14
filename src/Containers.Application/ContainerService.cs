
using Containers.Models;
using Microsoft.Data.SqlClient;

namespace Containers.Application;

public class ContainerService : IContainerService
{
    private string _connectionString;

    public ContainerService(string connectionString)
    {
        _connectionString = connectionString;
    }
    public IEnumerable<Container> GetAllContainers( )
    {
        List<Container> containers = [];
        const string queryString = "SELECT * FROM Containers";

        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            SqlCommand command = new SqlCommand(queryString, connection);
            
            connection.Open();
            SqlDataReader reader = command.ExecuteReader();

            try
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var containerRow = new Container
                        {
                            ID = reader.GetString(0),
                            ContainerTypeID = reader.GetString(1),
                            isHazardious = reader.GetBoolean(2),
                            Name = reader.GetString(3)
                        };
                        containers.Add(containerRow);
                    }
                }

            }
            finally
            {
                reader.Close();
            }
        }
        return containers;
    }

    public bool AddContainer(Container container)
    {
        const string insertString = "INSERT INTO Containers (ContainerTypeId, IsHasrdious, Name) Values (@ContainerTypeId, @IsHasrdious, @Name)";
        
        int countRowsAdded = -1;
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            SqlCommand command = new SqlCommand(insertString, connection);
            command.Parameters.AddWithValue("@ContainerTypeId", container.ContainerTypeID);
            command.Parameters.AddWithValue("@IsHasrdious", container.isHazardious);
            command.Parameters.AddWithValue("@Name", container.Name);
            
            connection.Open();
            countRowsAdded = command.ExecuteNonQuery();
        }
        return countRowsAdded != -1;
    }
    
}