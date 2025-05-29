using Dapper;
using JUSTLockers.Classes;
using MySqlConnector;
using System.Data;
namespace JUSTLockers.Services;

public class AdminService : IAdminService
{

    // private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConfiguration _configuration;

    public AdminService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> CheckEmployeeExists(int employeeId)
    {
        try
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM Employees WHERE id = @EmployeeId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EmployeeId", employeeId);

                    var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return count > 0;
                }
            }
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public async Task<(bool Success, string Message)> AddSupervisor(Supervisor supervisor)
    {
        var employeeExists = await SupervisorExists(supervisor.Id);
        if (employeeExists)
        {
            return (false, "Supervisor ID already exists.");
        }
        else
        if (!await CheckEmployeeExists(supervisor.Id))
        {
            return (false, "Employee not found.");
        }
        else
        if (supervisor.DepartmentName != null && supervisor.Location != null)
        {
            if (await IsDepartmentAssigned(supervisor.DepartmentName, supervisor.Location) > 0)
                return (false, "Department is already assigned to another supervisor.");

            else
                try
                {
                    string queryEmployee = @"SELECT id, name, email,password FROM Employees WHERE id = @Id";
                    string querySupervisor = @"INSERT INTO Supervisors 
                                        (id, name, email, supervised_department, location,password) 
                                        VALUES (@Id, @Name, @Email, @DepartmentName, @Location,@password)";

                    using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                    {
                        string pass;
                        await connection.OpenAsync();

                        using (var commandEmployee = new MySqlCommand(queryEmployee, connection))
                        {
                            commandEmployee.Parameters.AddWithValue("@Id", supervisor.Id);

                            using (var reader = await commandEmployee.ExecuteReaderAsync())
                            {
                                if (!await reader.ReadAsync())
                                {
                                    return (false, "Employee not found.");
                                }

                                supervisor.Name = reader.GetString("name");
                                supervisor.Email = reader.GetString("email");
                                pass = reader.GetString("password");
                            }
                        }

                        using (var commandSupervisor = new MySqlCommand(querySupervisor, connection))
                        {
                            commandSupervisor.Parameters.AddWithValue("@Id", supervisor.Id);
                            commandSupervisor.Parameters.AddWithValue("@Name", supervisor.Name);
                            commandSupervisor.Parameters.AddWithValue("@Email", supervisor.Email);
                            commandSupervisor.Parameters.AddWithValue("@DepartmentName",
                                string.IsNullOrEmpty(supervisor.DepartmentName) ? DBNull.Value : supervisor.DepartmentName);
                            commandSupervisor.Parameters.AddWithValue("@Location",
                                string.IsNullOrEmpty(supervisor.Location) ? DBNull.Value : supervisor.Location);
                            commandSupervisor.Parameters.AddWithValue("@password", pass);

                            int rowsAffected = await commandSupervisor.ExecuteNonQueryAsync();

                            return rowsAffected > 0
                                ? (true, "Supervisor added successfully!")
                                : (false, "Failed to add supervisor.");
                        }
                    }
                }
                catch (MySqlException ex) when (ex.Number == 1062)
                {
                    return (false, "Supervisor ID already exists.");
                }
                catch (Exception ex)
                {
                    return (false, $"Error adding supervisor: {ex.Message}");
                }

        }
        else
        {
            return (false, "Department and location cannot be null.");
        }
    }

    public async Task<bool> SupervisorExists(int supervisorId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = "SELECT COUNT(*) FROM Supervisors WHERE id = @Id";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", supervisorId);
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
        }
    }


    //emas
    public string AssignCabinet(Cabinet model)
    {
        try
        {
            string query = @"
        INSERT INTO Cabinets 
        (number_cab, wing, level, location, department_name, Capacity) 
        VALUES 
        (@NumberCab, @Wing, @Level, @Location, @DepartmentName, @Capacity)";


            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NumberCab", model.CabinetNumber);
                    command.Parameters.AddWithValue("@Wing", model.Wing);
                    command.Parameters.AddWithValue("@Level", model.Level);
                    command.Parameters.AddWithValue("@Location", model.Location);
                    command.Parameters.AddWithValue("@DepartmentName", model.Department);
                    command.Parameters.AddWithValue("@Capacity", model.Capacity);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return "Cabinet added successfully!";
                    }
                    else
                    {
                        return "Failed to add cabinet. Please try again.";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database error: {ex.Message}");
            // if (ex.Message.Contains("Duplicate entry") && ex.Message.Contains("key") && ex.Message.Contains("PRIMARY"))
            // {
            //     return "Cabinet Number already exists";
            // }
            return "Error adding cabinet: " + ex.Message;

        }
    }
    public async Task<string> AssignCovenant(int supervisorId, string departmentName, string location)
    {

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();


            // 2. Get supervisor's current details
            var supervisorQuery = "SELECT id, name FROM Supervisors WHERE id = @SupervisorId";
            string supervisorName = null;

            using (var supervisorCmd = new MySqlCommand(supervisorQuery, connection))
            {
                supervisorCmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                using (var reader = await supervisorCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        supervisorName = reader.IsDBNull("name") ? null : reader.GetString("name");
                    }
                    else
                    {
                        return "Supervisor does not exist.";
                    }
                }
            }

            var assignDepartment = await IsDepartmentAssigned(departmentName, location);
            if (assignDepartment != 0)
            {
                return $"Department is assigned to supervisor with id {assignDepartment}";
            }


            // 4. Update supervisor's covenant and location
            var updateQuery = @"UPDATE Supervisors 
                        SET supervised_department = @DepartmentName,
                            location = @Location
                        WHERE id = @SupervisorId";

            try
            {
                using (var updateCmd = new MySqlCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@DepartmentName", departmentName);
                    updateCmd.Parameters.AddWithValue("@Location", location);
                    updateCmd.Parameters.AddWithValue("@SupervisorId", supervisorId);

                    int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                    return rowsAffected > 0
                        ? $"Covenant assigned successfully. {supervisorName} is now responsible for {departmentName} at {location}."
                        : "Failed to assign covenant.";
                }
            }
            catch (Exception ex)
            {
                return $"Error assigning covenant: {ex.Message}";
            }
        }
    }
    public async Task<string> DeleteSupervisor(int supervisorId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {

            await connection.OpenAsync();
            try
            {
                // First check if supervisor exists
                var checkQuery = "SELECT COUNT(1) FROM Supervisors WHERE id = @SupervisorId";
                var exists = await connection.ExecuteScalarAsync<bool>(checkQuery, new { SupervisorId = supervisorId });

                if (!exists)
                {
                    return "Supervisor not found.";
                }

                // Delete the supervisor
                var deleteQuery = "DELETE FROM Supervisors WHERE id = @SupervisorId";
                int rowsAffected = await connection.ExecuteAsync(deleteQuery, new { SupervisorId = supervisorId });
                return rowsAffected > 0
                    ? "Supervisor deleted successfully."
                    : "Failed to delete supervisor.";
            }
            catch (Exception ex)
            {
                return "Error in the services for deleting supervisor: " + supervisorId;
            }
        }

    }

    public async Task<string> DeleteCovenant(int supervisorId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            // First check if supervisor exists
            connection.Open();
            var checkQuery = "SELECT COUNT(*) FROM Supervisors WHERE id = @SupervisorId";
            using (var checkCommand = new MySqlCommand(checkQuery, connection))
            {
                checkCommand.Parameters.AddWithValue("@SupervisorId", supervisorId);
                var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

                if (!exists)
                {
                    return "Supervisor not found.";
                }
            }

            // Check if supervisor actually has a covenant to delete
            var covenantCheckQuery = "SELECT COUNT(*) FROM Supervisors WHERE id = @SupervisorId AND supervised_department IS NOT NULL";
            using (var covenantCheckCmd = new MySqlCommand(covenantCheckQuery, connection))
            {
                covenantCheckCmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                var hasCovenant = Convert.ToInt32(await covenantCheckCmd.ExecuteScalarAsync()) > 0;

                if (!hasCovenant)
                {
                    return "Supervisor doesn't have a covenant assigned.";
                }
            }

            // Perform the deletion
            var updateQuery = "UPDATE Supervisors SET supervised_department = NULL ,location = NULL WHERE id = @SupervisorId";
            using (var updateCmd = new MySqlCommand(updateQuery, connection))
            {
                updateCmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                return rowsAffected > 0
                    ? "Covenant deleted successfully."
                    : "Failed to delete covenant.";
            }
        }
    }

    public async Task<Supervisor> GetSupervisorById(int supervisorId)
    {
        using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        {
            connection.Open();
            var query = @"  
               SELECT s.id, s.name, s.email, s.supervised_department, s.location   
               FROM Supervisors s  
               WHERE s.id = @SupervisorId";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@SupervisorId", supervisorId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        // Fetch the supervised department (if assigned)  
                        Department? supervisedDepartment = null;
                        if (!reader.IsDBNull(reader.GetOrdinal("supervised_department")))
                        {
                            supervisedDepartment = new Department
                            {
                                Name = reader.GetString("supervised_department"),
                                Location = reader.GetString("location")
                            };
                        }

                        // Create and return the Supervisor object  
                        return new Supervisor(
                            id: reader.GetInt32("id"),
                            name: reader.GetString("name"),
                            email: reader.GetString("email"),
                            department: supervisedDepartment ?? new Department()
                        );
                    }
                }
            }
        }

        return null; // Return null if no supervisor is found  
    }


    public async Task<bool> RejectRequestReallocation(int requestId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();

            string rejectQuery = @"UPDATE Reallocation 
                               SET RequestStatus = 'Rejected' 
                               WHERE RequestID = @RequestID";

            using (var rejectCmd = new MySqlCommand(rejectQuery, connection))
            {
                rejectCmd.Parameters.AddWithValue("@RequestID", requestId);
                int rowsAffected = await rejectCmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }

        }
    }



    public async Task<bool> ApproveRequestReallocation(int requestId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    // Get reallocation data
                    string selectQuery = @"
                    SELECT 
                        r.RequestedDepartment, 
                        r.RequestLocation, 
                        r.number_cab, 
                        r.RequestWing, 
                        r.RequestLevel,
                        r.CurrentCabinetID,
                        r.CurrentDepartment,
                        r.CurrentLocation,
                        r.RequestStatus
                    FROM Reallocation r
                    WHERE r.RequestID = @RequestID";

                    string? newDepartment = null;
                    string? newLocation = null;
                    int cabinetNumber = 0;
                    string? newWing = null;
                    int newLevel = 0;
                    string? oldCabinetId = null;
                    string? oldDepartment = null;
                    string? oldLocation = null;
                    string? requestStatus = null;

                    using (var selectCmd = new MySqlCommand(selectQuery, connection, transaction))
                    {
                        selectCmd.Parameters.AddWithValue("@RequestID", requestId);
                        using (var reader = await selectCmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                requestStatus = reader["RequestStatus"]?.ToString();
                                if (requestStatus != "Pending")
                                {
                                    await transaction.RollbackAsync();
                                    Console.WriteLine($"Request {requestId} is not pending. Status: {requestStatus}");
                                    return false;
                                }
                                newDepartment = reader["RequestedDepartment"]?.ToString();
                                newLocation = reader["RequestLocation"]?.ToString();
                                cabinetNumber = reader["number_cab"] != DBNull.Value ? Convert.ToInt32(reader["number_cab"]) : 0;
                                newWing = reader["RequestWing"]?.ToString();
                                newLevel = reader["RequestLevel"] != DBNull.Value ? Convert.ToInt32(reader["RequestLevel"]) : 0;
                                oldCabinetId = reader["CurrentCabinetID"]?.ToString();
                                oldDepartment = reader["CurrentDepartment"]?.ToString();
                                oldLocation = reader["CurrentLocation"]?.ToString();
                            }
                            else
                            {
                                await transaction.RollbackAsync();
                                Console.WriteLine($"Request {requestId} not found.");
                                return false;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(oldCabinetId))
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Invalid old cabinet ID for request {requestId}.");
                        return false;
                    }

                    //int lockerChange = 0;
                    //// Temporarily set cabinet_id in Lockers to NULL
                    //string tempUpdateLockersQuery = @"
                    //UPDATE Lockers 
                    //SET cabinet_id = NULL
                    //WHERE cabinet_id = @OldCabinetId";

                    //using (var tempLockersCmd = new MySqlCommand(tempUpdateLockersQuery, connection, transaction))
                    //{
                    //    tempLockersCmd.Parameters.AddWithValue("@OldCabinetId", oldCabinetId);
                    //    lockerChange = await tempLockersCmd.ExecuteNonQueryAsync();
                    //    Console.WriteLine($"Updated {lockerChange} lockers to NULL cabinet_id.");
                    //}

                    // Update Cabinet information
                    string updateCabinetQuery = @"
                    UPDATE Cabinets 
                    SET 
                        department_name = @NewDepartment,
                        wing = @NewWing,
                        level = @NewLevel,
                        location = @NewLocation
                    WHERE cabinet_id = @OldCabinetId";

                    using (var updateCmd = new MySqlCommand(updateCabinetQuery, connection, transaction))
                    {
                        updateCmd.Parameters.AddWithValue("@NewDepartment", newDepartment);
                        updateCmd.Parameters.AddWithValue("@NewWing", newWing);
                        updateCmd.Parameters.AddWithValue("@NewLevel", newLevel);
                        updateCmd.Parameters.AddWithValue("@NewLocation", newLocation);
                        updateCmd.Parameters.AddWithValue("@OldCabinetId", oldCabinetId);

                        int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            await transaction.RollbackAsync();
                            Console.WriteLine($"Failed to update cabinet {oldCabinetId}. Rows affected: {rowsAffected}. " +
    $"Parameters: department_name={newDepartment}, wing={newWing}, level={newLevel}, location={newLocation}");
                            return false;
                        }
                    }

                    // Get the new cabinet_id that was generated
                    string? newCabinetId = null;
                    string getNewCabinetIdQuery = @"
    SELECT cabinet_id FROM Cabinets 
    WHERE department_name = @NewDepartment 
    AND wing = @NewWing 
    AND level = @NewLevel 
    AND number_cab = @CabinetNumber";

                    using (var getCabinetCmd = new MySqlCommand(getNewCabinetIdQuery, connection, transaction))
                    {
                        getCabinetCmd.Parameters.AddWithValue("@NewDepartment", newDepartment);
                        getCabinetCmd.Parameters.AddWithValue("@NewWing", newWing);
                        getCabinetCmd.Parameters.AddWithValue("@NewLevel", newLevel);
                        getCabinetCmd.Parameters.AddWithValue("@CabinetNumber", cabinetNumber);

                        newCabinetId = await getCabinetCmd.ExecuteScalarAsync() as string;
                        if (string.IsNullOrEmpty(newCabinetId))
                        {
                            await transaction.RollbackAsync();
                            Console.WriteLine($"Failed to retrieve new cabinet ID for {newDepartment}, {newWing}, {newLevel}, {cabinetNumber}.");
                            return false;
                        }
                    }



                    var lockerIds = new List<(string OldId, string NewId)>();
                    string selectLockersQuery = @"
    SELECT Id 
    FROM Lockers 
    WHERE Id LIKE CONCAT(@OldCabinetIdPattern, '%')";

                    using (var selectLockersCmd = new MySqlCommand(selectLockersQuery, connection, transaction))
                    {
                        selectLockersCmd.Parameters.AddWithValue("@OldDepartment", oldDepartment);
                        selectLockersCmd.Parameters.AddWithValue("@OldCabinetIdPattern", oldCabinetId);
                        using (var reader = await selectLockersCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string oldLockerId = reader["Id"]?.ToString() ?? string.Empty;
                                string newLockerId = $"{newCabinetId}-{oldLockerId.Split('-').Last()}";
                                lockerIds.Add((oldLockerId, newLockerId));
                            }
                        }
                    }


                    // Update lockers
                    foreach (var (oldLockerId, newLockerId) in lockerIds)
                    {
                        string updateLockerQuery = @"
                            UPDATE Lockers 
                            SET 
                                Id = @NewLockerId
                            WHERE Id = @OldLockerId";

                        using (var lockerCmd = new MySqlCommand(updateLockerQuery, connection, transaction))
                        {
                            lockerCmd.Parameters.AddWithValue("@NewLockerId", newLockerId);
                            lockerCmd.Parameters.AddWithValue("@NewDepartment", newDepartment);
                            lockerCmd.Parameters.AddWithValue("@NewCabinetId", newCabinetId);
                            lockerCmd.Parameters.AddWithValue("@OldLockerId", oldLockerId);
                            int rowsAffected = await lockerCmd.ExecuteNonQueryAsync();
                            if (rowsAffected == 0)
                            {
                                await transaction.RollbackAsync();
                                Console.WriteLine($"Failed to update locker {oldLockerId} to {newLockerId}.");
                                return false;
                            }
                        }
                    }


                    // Mark reallocation request as approved
                    string approveQuery = @"
                    UPDATE Reallocation 
                    SET RequestStatus = 'Approved'    
                    WHERE RequestID = @RequestID";

                    using (var approveCmd = new MySqlCommand(approveQuery, connection, transaction))
                    {
                        approveCmd.Parameters.AddWithValue("@RequestID", requestId);
                        await approveCmd.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                    Console.WriteLine($"Reallocation request {requestId} approved successfully.");
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"Error approving reallocation {requestId}: {ex.Message}\nStack Trace: {ex.StackTrace}");
                    return false; // Changed to return false instead of rethrowing for testability
                }
            }
        }
    }

    //public async Task<bool> ApproveRequestReallocation(int requestId)
    //{
    //    using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
    //    {
    //        await connection.OpenAsync();
    //        using (var transaction = await connection.BeginTransactionAsync())
    //        {
    //            try
    //            {
    //                // Get all necessary reallocation data
    //                string selectQuery = @"
    //    SELECT 
    //        r.RequestedDepartment, 
    //        r.RequestLocation, 
    //        r.number_cab, 
    //        r.RequestWing, 
    //        r.RequestLevel,
    //        r.CurrentCabinetID,
    //        r.CurrentDepartment,
    //        r.CurrentLocation
    //    FROM Reallocation r
    //    WHERE r.RequestID = @RequestID AND r.RequestStatus = 'Pending'";

    //                string? newDepartment = null;
    //                string? newLocation = null;
    //                int cabinetNumber = 0;
    //                string? newWing = null;
    //                int newLevel = 0;
    //                string? oldCabinetId = null;
    //                string? oldDepartment = null;
    //                string? oldLocation = null;

    //                using (var selectCmd = new MySqlCommand(selectQuery, connection, transaction))
    //                {
    //                    selectCmd.Parameters.AddWithValue("@RequestID", requestId);
    //                    using (var reader = await selectCmd.ExecuteReaderAsync())
    //                    {
    //                        if (await reader.ReadAsync())
    //                        {
    //                            newDepartment = reader["RequestedDepartment"]?.ToString();
    //                            newLocation = reader["RequestLocation"]?.ToString();
    //                            cabinetNumber = reader["number_cab"] != DBNull.Value ? Convert.ToInt32(reader["number_cab"]) : 0;
    //                            newWing = reader["RequestWing"]?.ToString();
    //                            newLevel = reader["RequestLevel"] != DBNull.Value ? Convert.ToInt32(reader["RequestLevel"]) : 0;
    //                            oldCabinetId = reader["CurrentCabinetID"]?.ToString();
    //                            oldDepartment = reader["CurrentDepartment"]?.ToString();
    //                            oldLocation = reader["CurrentLocation"]?.ToString();
    //                        }
    //                        else
    //                        {
    //                            await transaction.RollbackAsync();
    //                            return false;// Request not found or not pending
    //                        }
    //                    }
    //                }
    //                var lockerChange = 0;
    //                // 1. Temporarily set cabinet_id in Lockers to NULL (allowed since cabinet_id is DEFAULT NULL)
    //                string tempUpdateLockersQuery = @"
    //    UPDATE Lockers 
    //    SET cabinet_id = NULL
    //    WHERE cabinet_id = @OldCabinetId";

    //                using (var tempLockersCmd = new MySqlCommand(tempUpdateLockersQuery, connection, transaction))
    //                {
    //                    tempLockersCmd.Parameters.AddWithValue("@OldCabinetId", oldCabinetId);
    //                 lockerChange=  await tempLockersCmd.ExecuteNonQueryAsync();
    //                }

    //                // 2. Update Cabinet information - this will regenerate the cabinet_id
    //                string updateCabinetQuery = @"
    //    UPDATE Cabinets 
    //    SET 
    //        department_name = @NewDepartment,
    //        wing = @NewWing,
    //        level = @NewLevel,
    //        location = @NewLocation
    //    WHERE cabinet_id = @OldCabinetId";

    //                using (var updateCmd = new MySqlCommand(updateCabinetQuery, connection, transaction))
    //                {
    //                    updateCmd.Parameters.AddWithValue("@NewDepartment", newDepartment);
    //                    updateCmd.Parameters.AddWithValue("@NewWing", newWing);
    //                    updateCmd.Parameters.AddWithValue("@NewLevel", newLevel);
    //                    updateCmd.Parameters.AddWithValue("@NewLocation", newLocation);
    //                    updateCmd.Parameters.AddWithValue("@OldCabinetId", oldCabinetId);

    //                    int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
    //                    if (rowsAffected == 0)
    //                    {
    //                        await transaction.RollbackAsync();
    //                        return false;
    //                    }
    //                }

    //                // Get the new cabinet_id that was generated
    //                string? newCabinetId = null;
    //                string getNewCabinetIdQuery = @"
    //    SELECT cabinet_id FROM Cabinets 
    //    WHERE department_name = @NewDepartment 
    //    AND wing = @NewWing 
    //    AND level = @NewLevel 
    //    AND number_cab = @CabinetNumber";

    //                using (var getCabinetCmd = new MySqlCommand(getNewCabinetIdQuery, connection, transaction))
    //                {
    //                    getCabinetCmd.Parameters.AddWithValue("@NewDepartment", newDepartment);
    //                    getCabinetCmd.Parameters.AddWithValue("@NewWing", newWing);
    //                    getCabinetCmd.Parameters.AddWithValue("@NewLevel", newLevel);
    //                    getCabinetCmd.Parameters.AddWithValue("@CabinetNumber", cabinetNumber);

    //                    newCabinetId = await getCabinetCmd.ExecuteScalarAsync() as string;
    //                    if (string.IsNullOrEmpty(newCabinetId))
    //                    {
    //                        await transaction.RollbackAsync();
    //                        return false;
    //                    }
    //                }

    //                if (lockerChange > 0)
    //                {
    //                    // 3. Retrieve all Lockers to update their IDs
    //                    var lockerIds = new List<(string OldId, string NewId)>();
    //                    string selectLockersQuery = @"
    //    SELECT Id 
    //    FROM Lockers 
    //    WHERE DepartmentName = @OldDepartment AND Id LIKE CONCAT(@OldCabinetIdPattern, '%')";

    //                    using (var selectLockersCmd = new MySqlCommand(selectLockersQuery, connection, transaction))
    //                    {
    //                        selectLockersCmd.Parameters.AddWithValue("@OldDepartment", oldDepartment);
    //                        selectLockersCmd.Parameters.AddWithValue("@OldCabinetIdPattern", oldCabinetId);
    //                        using (var reader = await selectLockersCmd.ExecuteReaderAsync())
    //                        {
    //                            while (await reader.ReadAsync())
    //                            {
    //                                string oldLockerId = reader["Id"]?.ToString() ?? string.Empty;
    //                                string newLockerId = $"{newCabinetId}-{oldLockerId.Split('-').Last()}";
    //                                lockerIds.Add((oldLockerId, newLockerId));
    //                            }
    //                        }
    //                    }


    //                    // 6. Update Lockers with new IDs, department, and cabinet_id
    //                    foreach (var (oldLockerId, newLockerId) in lockerIds)
    //                    {
    //                        string updateLockerQuery = @"
    //        UPDATE Lockers 
    //        SET 
    //            Id = @NewLockerId,
    //            DepartmentName = @NewDepartment,
    //            cabinet_id = @NewCabinetId
    //        WHERE Id = @OldLockerId";

    //                        using (var lockerCmd = new MySqlCommand(updateLockerQuery, connection, transaction))
    //                        {
    //                            lockerCmd.Parameters.AddWithValue("@NewLockerId", newLockerId);
    //                            lockerCmd.Parameters.AddWithValue("@NewDepartment", newDepartment);
    //                            lockerCmd.Parameters.AddWithValue("@NewCabinetId", newCabinetId);
    //                            lockerCmd.Parameters.AddWithValue("@OldLockerId", oldLockerId);
    //                            await lockerCmd.ExecuteNonQueryAsync();
    //                        }
    //                    }
    //                }//this is new here



    //                // 7. Mark the reallocation request as approved
    //                string approveQuery = @"
    //    UPDATE Reallocation 
    //    SET 
    //        RequestStatus = 'Approved'    
    //    WHERE RequestID = @RequestID";

    //                using (var approveCmd = new MySqlCommand(approveQuery, connection, transaction))
    //                {
    //                    approveCmd.Parameters.AddWithValue("@RequestID", requestId);

    //                    await approveCmd.ExecuteNonQueryAsync();
    //                }

    //                await transaction.CommitAsync();
    //                return true;
    //            }
    //            catch (Exception ex)
    //            {
    //                await transaction.RollbackAsync();
    //                Console.WriteLine($"Error approving reallocation: {ex.Message}");

    //                throw;

    //            }
    //        }
    //    }
    //}
    public async Task<List<string>> GetAffectedStudentAsync(string cabinetId)
    {
        var affectedStudents = new List<string>();
        try
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                string query = "SELECT email FROM Students WHERE locker_id IN (SELECT Id FROM Lockers WHERE Id LIKE CONCAT(@cabinetId, '%'))";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@cabinetId", cabinetId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var email = reader["email"]?.ToString();
                            if (!string.IsNullOrEmpty(email))
                            {
                                affectedStudents.Add(email);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching affected students: {ex.Message}");
        }

        return affectedStudents;
    }
    //emas 



    public async Task<List<Reallocation>> ReallocationResponse()
    {
        var reallocations = new List<Reallocation>();

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = @"SELECT RequestID, SupervisorID, CurrentDepartment, RequestLocation, CurrentLocation,
                                RequestedDepartment, CurrentCabinetID, NewCabinetID
                         FROM Reallocation
                         WHERE RequestStatus = 'Pending' 
                         AND RequestedDepartment != CurrentDepartment";

            using (var command = new MySqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    reallocations.Add(new Reallocation
                    {
                        RequestID = reader["RequestID"] != DBNull.Value ? Convert.ToInt32(reader["RequestID"]) : 0,
                        SupervisorID = reader["SupervisorID"] != DBNull.Value ? Convert.ToInt32(reader["SupervisorID"]) : 0, // Convert to int
                        CurrentDepartment = reader["CurrentDepartment"]?.ToString(),
                        RequestedDepartment = reader["RequestedDepartment"]?.ToString(),
                        CurrentCabinetID = reader["CurrentCabinetID"]?.ToString(),
                        NewCabinetID = reader["NewCabinetID"]?.ToString(),
                        CurrentLocation = reader["CurrentLocation"]?.ToString(),
                        RequestLocation = reader["RequestLocation"]?.ToString()
                    });
                }
            }
        }

        return reallocations;
    }




    public async Task<List<Report>> ViewForwardedReports()
    {
        var reports = new List<Report>();

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            connection.Open();
            var query = @"
            SELECT 
                r.Id AS ReportId,
                r.Subject AS ProblemDescription,
                r.Statement AS DetailedDescription,
                r.Type AS ReportType,
                r.Status AS ReportStatus,
                r.ReportDate AS ReportDate,
                r.ResolvedDate AS ResolvedDate,
                r.ResolvedDetails AS ResolutionDetails,
                r.ImageData AS ImageData,
                r.ImageMimeType AS ImageMimeType,
                l.Id AS LockerNumber,
                l.Status AS LockerStatus,
                u.id AS ReporterId,
                u.name AS ReporterName,
                u.email AS ReporterEmail,
                d.name AS DepartmentName
       
            FROM 
                Reports r
            JOIN 
                Lockers l ON r.LockerId = l.Id
            JOIN 
                Students u ON r.ReporterId = u.id
            JOIN 
                Departments d ON l.DepartmentName = d.name
where r.Type='THEFT' and r.SentToAdmin=1
            ORDER BY 
                r.ReportDate DESC";

            using (var command = new MySqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    reports.Add(new Report
                    {
                        ReportId = reader.GetInt32("ReportId"),
                        Reporter = new Student(
                            reader.GetInt32("ReporterId"),
                            reader.GetString("ReporterName"),
                            reader.GetString("ReporterEmail"),
                            reader.GetString("DepartmentName")

                        // Department is fetched from Lockers
                        ),
                        Locker = new Locker
                        {
                            LockerId = reader.GetString("LockerNumber"),
                            Status = (LockerStatus)Enum.Parse(typeof(LockerStatus), reader.GetString("LockerStatus")),
                            Department = reader.GetString("DepartmentName"),
                        },
                        Type = (ReportType)Enum.Parse(typeof(ReportType), reader.GetString("ReportType")),
                        Status = (ReportStatus)Enum.Parse(typeof(ReportStatus), reader.GetString("ReportStatus")),
                        Subject = reader.GetString("ProblemDescription"),
                        Statement = reader.GetString("DetailedDescription"),
                        ReportDate = reader.GetDateTime("ReportDate"),
                        ResolvedDate = reader.IsDBNull(reader.GetOrdinal("ResolvedDate")) ? (DateTime?)null : reader.GetDateTime("ResolvedDate"),
                        ResolutionDetails = reader.IsDBNull(reader.GetOrdinal("ResolutionDetails")) ? null : reader.GetString("ResolutionDetails"),
                        ImageData = reader.IsDBNull(reader.GetOrdinal("ImageData")) ? null : (byte[])reader["ImageData"],
                        ImageMimeType = reader.IsDBNull(reader.GetOrdinal("ImageMimeType")) ? null : reader.GetString("ImageMimeType")
                    });
                }
            }
        }
        return reports;
    }





    public async Task<List<Cabinet>> ViewCabinetInfo(string? searchCab = null, string? location = null, int? level = null, string? department = null, string? status = null, string? wing = null)
    {
        var cabinets = new List<Cabinet>();
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = @"SELECT number_cab, wing, level, location, department_name, 
                         cabinet_id, Capacity, status 
                      FROM Cabinets WHERE 1=1";

            var command = new MySqlCommand();
            command.Connection = connection;

            if (!string.IsNullOrEmpty(searchCab) && searchCab != "")
            {
                query += " AND cabinet_id like @searchCab";
                command.Parameters.AddWithValue("@searchCab", "%" + searchCab.Trim() + "%");

                // command.Parameters.AddWithValue("@searchCab", $"%{searchCab}%");
            }



            if (!string.IsNullOrEmpty(location) && location != "All")
            {
                query += " AND location = @location";
                command.Parameters.AddWithValue("@location", location);
            }

            if (level.HasValue)
            {
                query += " AND level = @level";
                command.Parameters.AddWithValue("@level", level.Value);
            }

            if (!string.IsNullOrEmpty(department) && department != "All Dep")
            {
                query += " AND department_name = @department";
                command.Parameters.AddWithValue("@department", department);
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query += " AND status = @status";
                command.Parameters.AddWithValue("@status", status);
            }

            if (!string.IsNullOrEmpty(wing) && wing != "All")
            {
                query += " AND wing = @wing";
                command.Parameters.AddWithValue("@wing", wing);
            }
            query += " order by department_name";
            command.CommandText = query;

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    cabinets.Add(new Cabinet
                    {
                        CabinetNumber = reader.GetInt32("number_cab"),
                        Wing = reader["wing"].ToString(),
                        Level = reader.GetInt32("level"),
                        Location = reader["location"].ToString(),
                        Department = reader["department_name"].ToString(),
                        Cabinet_id = reader["cabinet_id"].ToString(),
                        Capacity = reader.GetInt32("Capacity"),
                        Status = Enum.TryParse<CabinetStatus>(reader["status"].ToString(), out var statusEnum)
                                 ? statusEnum
                                 : CabinetStatus.IN_SERVICE,
                        EmployeeName = ""
                    });






                }
            }


        }

        return cabinets;
    }





    public async Task<List<Supervisor>> ViewAllSupervisorInfo()
    {
        var supervisors = new List<Supervisor>();

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            connection.Open();
            var query = @"
            SELECT DISTINCT
                s.id, 
                s.name, 
                s.email, 
                s.location,
                s.supervised_department,
                d.name AS department_name,
                d.total_wings,
                d.Location AS department_location
            FROM 
                Supervisors s
            LEFT JOIN 
                Departments d ON s.supervised_department = d.name
            GROUP BY s.id";

            using (var command = new MySqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    Department supervisedDepartment = null;

                    // Only create department object if supervisor has one assigned
                    if (!reader.IsDBNull(reader.GetOrdinal("supervised_department")))
                    {
                        supervisedDepartment = new Department
                        {
                            Name = reader.IsDBNull(reader.GetOrdinal("supervised_department")) ? null : reader.GetString("supervised_department"),
                            Total_Wings = reader.IsDBNull(reader.GetOrdinal("total_wings")) ? 0 : reader.GetInt32("total_wings"),
                            Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString("location")
                        };
                    }

                    supervisors.Add(new Supervisor(
                        id: reader.GetInt32("id"),
                        name: reader.GetString("name"),
                        email: reader.GetString("email"),
                        department: supervisedDepartment
                    )
                    {
                        Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString("location")
                    });
                }
            }
        }

        return supervisors;
    }



    public async Task<List<Department>> GetDepartments()
    {
        var departments = new List<Department>();

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            connection.Open();
            var query = "SELECT name, total_wings, Location FROM Departments";

            using (var command = new MySqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    departments.Add(new Department
                    {
                        Name = reader.GetString("name"),
                        Total_Wings = reader.GetInt32("total_wings"),
                        Location = reader.GetString("Location")
                    });
                }
            }
        }

        return departments;
    }
    public async Task<List<Department>> GetDepartmentsByLocation(string location)
    {
        var departments = new List<Department>();

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = "SELECT name, Location FROM Departments WHERE Location = @Location";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Location", location);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        departments.Add(new Department
                        {
                            Name = reader.GetString("name"),
                            Location = reader.GetString("Location")
                        });
                    }
                }
            }
        }

        return departments;
    }


    public async Task<bool> ResolveReport(int reportId, string? resolutionDetails)
    {
        try
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                var query = @"UPDATE Reports 
                         SET Status = 'RESOLVED', 
                             ResolvedDate = @ResolvedDate,
                             ResolvedDetails = @ResolutionDetails
                         WHERE Id = @ReportId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ReportId", reportId);
                    command.Parameters.AddWithValue("@ResolvedDate", DateTime.Now);
                    command.Parameters.AddWithValue("@ResolutionDetails", resolutionDetails);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> ReviewReport(int reportId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();

            var query = @"UPDATE Reports 
                     SET Status = 'IN_REVIEW' 
                     WHERE Id = @ReportId";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ReportId", reportId);
                return await command.ExecuteNonQueryAsync() > 0;
            }
        }
    }

    public async Task<bool> RejectReport(int reportId)
    {
        try
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                var query = @"UPDATE Reports 
                     SET Status = 'REJECTED', 
                         ResolvedDate = @ResolvedDate
                     WHERE Id = @ReportId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ReportId", reportId);
                    command.Parameters.AddWithValue("@ResolvedDate", DateTime.Now);
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<Report> GetReportDetails(int reportId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = @"
            SELECT r.Id AS ReportId, r.LockerId, r.Subject, r.Statement, r.Status, r.ReportDate, r.ResolvedDate, r.ResolvedDetails,
                   l.DepartmentName, s.id AS ReporterId, s.name, s.email, s.department
            FROM Reports r
            JOIN Lockers l ON r.LockerId = l.Id
            JOIN Students s ON r.ReporterId = s.id
            WHERE r.Id = @ReportId";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ReportId", reportId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Report
                        {
                            ReportId = reader.GetInt32("ReportId"),
                            Locker = new Locker
                            {
                                LockerId = reader.GetString("LockerId"),
                                Department = reader.GetString("DepartmentName")
                            },
                            Subject = reader.GetString("Subject"),
                            Statement = reader.GetString("Statement"),
                            Status = Enum.Parse<ReportStatus>(reader.GetString("Status")),
                            ReportDate = reader.GetDateTime("ReportDate"),
                            ResolvedDate = reader.IsDBNull("ResolvedDate") ? null : reader.GetDateTime("ResolvedDate"),
                            ResolutionDetails = reader.IsDBNull("ResolvedDetails") ? null : reader.GetString("ResolvedDetails"),
                            Reporter = new Student
                            (
                                 reader.GetInt32("ReporterId"),
                                 reader.GetString("name"),
                                reader.GetString("email"),
                                reader.GetString("department")
                            )
                        };
                    }
                }
            }
        }
        return null; // Return null if no report is found
    }


    public async Task<int> IsDepartmentAssigned(string departmentName, string location)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = @"SELECT id FROM Supervisors 
                     WHERE supervised_department = @DepartmentName 
                     AND location = @Location";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@DepartmentName", departmentName);
                command.Parameters.AddWithValue("@Location", location);
                var id = Convert.ToInt32(await command.ExecuteScalarAsync());
                return id;
            }
        }

    }

    public async Task<Reallocation> GetReallocationRequestById(int requestId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = @"SELECT * FROM Reallocation WHERE RequestID = @RequestID";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@RequestID", requestId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Reallocation
                        {
                            RequestID = reader.GetInt32("RequestID"),
                            SupervisorID = reader.GetInt32("SupervisorID"),
                            CurrentDepartment = reader.GetString("CurrentDepartment"),
                            RequestedDepartment = reader.GetString("RequestedDepartment"),
                            CurrentCabinetID = reader.GetString("CurrentCabinetID"),
                            NewCabinetID = reader.GetString("NewCabinetID"),
                            CurrentLocation = reader.GetString("CurrentLocation"),
                            RequestLocation = reader.GetString("RequestLocation"),
                            RequestWing = reader.GetString("RequestWing"),
                            RequestLevel = reader.GetInt32("RequestLevel"),
                        };
                    }
                }
            }
        }
        return null; // Return null if no reallocation request is found
    }


    public async Task<object> GetSemesterSettings()
    {
        using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var query = "SELECT Id, SemesterEndDate FROM SemesterSettings ORDER BY CreatedAt DESC LIMIT 1";
        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new
            {
                Id = reader.GetInt32(0),
                SemesterEndDate = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1)
            };
        }
        return new { Id = 0, SemesterEndDate = (DateTime?)null };
    }

    public async Task<bool> SaveSemesterSettings(DateTime endDate, int? existingId = null)
    {
        using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        string query;
        if (existingId.HasValue)
        {
            // Update existing record
            query = @"
                UPDATE SemesterSettings 
                SET SemesterEndDate = @EndDate, UpdatedAt = CURRENT_TIMESTAMP
                WHERE Id = @Id";
        }
        else
        {
            // Insert new record
            query = @"
                INSERT INTO SemesterSettings (SemesterEndDate)
                VALUES (@EndDate)";
        }

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@EndDate", endDate);
        if (existingId.HasValue)
        {
            command.Parameters.AddWithValue("@Id", existingId.Value);
        }

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;

    }

    public async Task<bool> ClearReservationsAndReports()
    {
        using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Update lockers to EMPTY
            var updateLockersQuery = "UPDATE Lockers SET Status = 'EMPTY' WHERE Status = 'RESERVED'";
            using (var cmd = new MySqlCommand(updateLockersQuery, connection, transaction))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // update reservations
            var deleteReservationsQuery = "UPDATE Reservations SET Status = 'CANCELED'";
            using (var cmd = new MySqlCommand(deleteReservationsQuery, connection, transaction))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Delete reports
            var deleteReportsQuery = "DELETE FROM Reports WHERE Status = 'RESOLVED' OR Status = 'REJECTED'";
            using (var cmd = new MySqlCommand(deleteReportsQuery, connection, transaction))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            //update students
            var updateStudentsQuery = "UPDATE Students SET locker_id = NULL ";
            using (var cmd = new MySqlCommand(updateStudentsQuery, connection, transaction))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
        return true;
    }

    public async Task<List<string>> GetAllStudentsEmails()
    {
        var users = new List<string>();
        using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        var query = @"
            SELECT email FROM Students where locker_id is not null
            ";
        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(reader.GetString(0));
        }

        return users;
    }
    //get all supervisors emails 
    public async Task<List<string>> GetAllSupervisorsEmails()
    {
        var emails = new List<string>();
        using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();
        var query = "SELECT email FROM Supervisors WHERE supervised_department is not null";
        using var command = new MySqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            emails.Add(reader.GetString(0));
        }
        return emails;
    }

}