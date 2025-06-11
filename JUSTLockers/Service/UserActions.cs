using MySqlConnector;

namespace JUSTLockers.Services;

public class UserActions 
{
    private readonly IConfiguration _configuration;

    public UserActions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Login(int id , string password)
    {
        try
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = @"
                SELECT 'Admin' AS role FROM Admins WHERE id = @ID AND password = @Password
                UNION
                SELECT 'Supervisor' AS role FROM Supervisors WHERE id = @ID AND password = @Password
                UNION
                SELECT 'Student' AS role FROM Students WHERE id = @ID AND password = @Password
                LIMIT 1"; 

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ID", id); 
                    command.Parameters.AddWithValue("@Password", password); 

                    connection.Open();
                    var result = command.ExecuteScalar();

                    return result != null ? result.ToString() : null;
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error for debugging
            Console.WriteLine($"Database error: {ex.Message}");
            return null;
        }
    }
}