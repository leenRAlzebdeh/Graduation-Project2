using JUSTLockers.Classes;
using JUSTLockers.Service;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
namespace JUSTLockers.Services;


public class SupervisorService : ISupervisorService
{
    private readonly IConfiguration _configuration;
    private readonly AdminService _adminService;
    private readonly IMemoryCache _memoryCache;
    public SupervisorService(IConfiguration configuration,
        AdminService adminService, IMemoryCache? memoryCache)
    {
        _configuration = configuration;
        _adminService = adminService;
        _memoryCache = memoryCache;
    }
    public async Task<List<Report>> ViewReportedIssues(int? userId)
    {
        string cacheKey = $"Reports_{userId}";
        if (_memoryCache.TryGetValue(cacheKey, out List<Report> cachedReports))
        {
            return cachedReports;
        }
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
    r.SentToAdmin AS SentToAdmin,
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
    Cabinets c ON l.Cabinet_id = c.cabinet_id
JOIN 
    Students u ON r.ReporterId = u.id
JOIN 
    Departments d ON l.DepartmentName = d.name

WHERE 
    d.name = (SELECT supervised_department FROM Supervisors WHERE id = @userId) AND
    c.Location = (SELECT location FROM Supervisors WHERE id = @userId)


          
          ";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
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
                                Status = Enum.Parse<LockerStatus>(reader.GetString("LockerStatus")),
                                Department = reader.GetString("DepartmentName"),
                            },
                            Type = Enum.Parse<ReportType>(reader.GetString("ReportType")),
                            Status = Enum.Parse<ReportStatus>(reader.GetString("ReportStatus")),
                            Subject = reader.GetString("ProblemDescription"),
                            Statement = reader.GetString("DetailedDescription"),
                            ReportDate = reader.GetDateTime("ReportDate"),
                            ResolvedDate = reader.IsDBNull(reader.GetOrdinal("ResolvedDate")) ? (DateTime?)null : reader.GetDateTime("ResolvedDate"),
                            ResolutionDetails = reader.IsDBNull(reader.GetOrdinal("ResolutionDetails")) ? null : reader.GetString("ResolutionDetails"),
                            ImageData = reader.IsDBNull(reader.GetOrdinal("ImageData")) ? null : (byte[])reader["ImageData"],
                            ImageMimeType = reader.IsDBNull(reader.GetOrdinal("ImageMimeType")) ? null : reader.GetString("ImageMimeType"),
                            SentToAdmin = reader.GetBoolean("SentToAdmin")

                        });
                    }
                }
            }
        }
        // Cache the reports for 5 minutes
        _memoryCache.Set(cacheKey, reports, TimeSpan.FromMinutes(3));
        return reports;
    }
    public async Task<List<Report>> TheftIssues(string filter)
    {
        var reports = new List<Report>();
        string cacheKey = $"TheftReports_{filter}";
        if (_memoryCache.TryGetValue(cacheKey, out List<Report> cachedReports))
        {
            return cachedReports;
        }

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();

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
            r.SentToAdmin AS SentToAdmin,
            l.Id AS LockerNumber,
            l.Status AS LockerStatus,
            u.id AS ReporterId,
            u.name AS ReporterName,
            u.email AS ReporterEmail,
            d.name AS DepartmentName
        FROM 
            Reports r
        JOIN Lockers l ON r.LockerId = l.Id
        JOIN Students u ON r.ReporterId = u.id
        JOIN Departments d ON l.DepartmentName = d.name
        WHERE (@Filter IS NULL OR r.Type = @Filter)
    ";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Filter", filter?.ToLower() == "theft" ? "Theft" : null);

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
                            ImageMimeType = reader.IsDBNull(reader.GetOrdinal("ImageMimeType")) ? null : reader.GetString("ImageMimeType"),
                            SentToAdmin = reader.GetBoolean("SentToAdmin")
                        });
                    }
                }
            }
        }
        _memoryCache.Set(cacheKey, reports, TimeSpan.FromMinutes(3));
        return reports;

    }
    public async Task<List<Student>> ViewAllStudentReservations(int? userId, int? searchstu = null)
    {
        var students = new List<Student>();
        string cacheKey = $"StudentReservation_{userId}_{searchstu}";
        if (_memoryCache.TryGetValue(cacheKey, out List<Student> cachedStudents))
        {
            return cachedStudents;
        }

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();

            var query = @"
            SELECT s.id, s.name, s.email, s.Major, s.locker_id
            FROM Students s
            JOIN Supervisors su
            JOIN Reservations r
            on r.StudentId= s.id
            AND r.Status='Reserved'
            WHERE su.id =@userId and s.locker_id is not NULL 
            AND r.LockerId REGEXP CONCAT('^', @supervised_department, '([0-9]+)?-')";


            if (searchstu.HasValue)
            {
                query += " AND s.id = @searchstu";
            }

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@supervised_department", (await GetSupervisorLocationAndDepartment(userId.Value)).Department ?? (object)DBNull.Value);

                if (searchstu.HasValue)
                {
                    command.Parameters.AddWithValue("@searchstu", searchstu.Value);
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        students.Add(new Student
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("name"),
                            Email = reader.GetString("email"),
                            Major = reader.GetString("Major"),
                            LockerId = reader.IsDBNull(reader.GetOrdinal("locker_id")) ? null : reader.GetString("locker_id")
                        });
                    }
                }
            }
        }
        _memoryCache.Set(cacheKey, students, TimeSpan.FromMinutes(3));
        return students;
    }
    public async Task<(string message, int requestId)> ReallocationRequestFormSameDep(Reallocation model)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    // Step 1: Validate Supervisor's Department and Location
                    string query1 = "SELECT location, supervised_department FROM Supervisors WHERE id = @SupervisorID";
                    string supervisorLocation = null;
                    string supervisorDepartment = null;

                    using (var command1 = new MySqlCommand(query1, connection, transaction))
                    {
                        command1.Parameters.AddWithValue("@SupervisorID", model.SupervisorID);

                        using (var reader = await command1.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                supervisorLocation = reader["location"].ToString();
                                supervisorDepartment = reader["supervised_department"].ToString();
                            }
                            else
                            {
                                return ("Supervisor not found.", 0);
                            }
                        }
                    }

                    // Step 2: Check if the supervisor is authorized
                    if (supervisorLocation != model.CurrentLocation || supervisorDepartment != model.CurrentDepartment ||
                        supervisorLocation != model.RequestLocation || supervisorDepartment != model.RequestedDepartment)
                    {
                        return ($"You are not allowed to reallocate a cabinet outside your covenant of department and location: {supervisorDepartment}/{supervisorLocation}. ", 0);
                    }

                    // Step 3: Validate the requested cabinet (replace the empty if () ; statement)
                    string validateCabinetQuery = @"
                    SELECT COUNT(*) 
                    FROM Cabinets 
                    WHERE department_name = @RequestedDepartment 
                    AND wing = @RequestWing 
                    AND level = @RequestLevel 
                    AND number_cab = @NumberCab 
                    AND location = @RequestLocation";
                    using (var validateCmd = new MySqlCommand(validateCabinetQuery, connection, transaction))
                    {
                        validateCmd.Parameters.AddWithValue("@RequestedDepartment", model.RequestedDepartment);
                        validateCmd.Parameters.AddWithValue("@RequestWing", model.RequestWing);
                        validateCmd.Parameters.AddWithValue("@RequestLevel", model.RequestLevel);
                        validateCmd.Parameters.AddWithValue("@NumberCab", model.NumberCab);
                        validateCmd.Parameters.AddWithValue("@RequestLocation", model.RequestLocation);

                        var cabinetExists = Convert.ToInt32(await validateCmd.ExecuteScalarAsync());
                        if (cabinetExists > 0)
                        {
                            return ("The requested cabinet already exists at the specified location.", 0);
                        }
                    }

                    // Step 4: Insert the reallocation request
                    string insertQuery = @"
                    INSERT INTO Reallocation
                    (SupervisorID, CurrentDepartment, RequestedDepartment, 
                     RequestLocation, CurrentLocation, RequestWing, RequestLevel, 
                     number_cab, CurrentCabinetID)
                    VALUES
                    (@SupervisorID, @CurrentDepartment, @RequestedDepartment, 
                     @RequestLocation, @CurrentLocation, @RequestWing, @RequestLevel,
                     @NumberCab, @CurrentCabinetID);
                    SELECT LAST_INSERT_ID();";

                    int reallocationId;
                    using (var command = new MySqlCommand(insertQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@SupervisorID", model.SupervisorID);
                        command.Parameters.AddWithValue("@CurrentDepartment", model.CurrentDepartment ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@RequestedDepartment", model.RequestedDepartment ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CurrentLocation", model.CurrentLocation ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@RequestLocation", model.RequestLocation ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@RequestWing", model.RequestWing ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@RequestLevel", model.RequestLevel);
                        command.Parameters.AddWithValue("@NumberCab", model.NumberCab);
                        command.Parameters.AddWithValue("@CurrentCabinetID", model.CurrentCabinetID ?? (object)DBNull.Value);

                        reallocationId = Convert.ToInt32(await command.ExecuteScalarAsync());
                        if (reallocationId <= 0)
                        {
                            await transaction.RollbackAsync();
                            return ("Failed to insert reallocation request.", 0);
                        }
                    }

                    // Step 5: Perform the reallocation (merged from ApproveRequestReallocation)
                    // 5.1: Temporarily set cabinet_id in Lockers to NULL
                    string tempUpdateLockersQuery = @"
                    UPDATE Lockers 
                    SET cabinet_id = NULL
                    WHERE cabinet_id = @CurrentCabinetId";
                    int lockersUpdated;
                    using (var tempLockersCmd = new MySqlCommand(tempUpdateLockersQuery, connection, transaction))
                    {
                        tempLockersCmd.Parameters.AddWithValue("@CurrentCabinetId", model.CurrentCabinetID);
                        lockersUpdated = await tempLockersCmd.ExecuteNonQueryAsync();
                    }

                    // 5.2: Update Cabinet information
                    string updateCabinetQuery = @"
                    UPDATE Cabinets 
                    SET 
                        department_name = @RequestedDepartment,
                        wing = @RequestWing,
                        level = @RequestLevel,
                        location = @RequestLocation
                    WHERE cabinet_id = @CurrentCabinetId";
                    using (var updateCmd = new MySqlCommand(updateCabinetQuery, connection, transaction))
                    {
                        updateCmd.Parameters.AddWithValue("@RequestedDepartment", model.RequestedDepartment);
                        updateCmd.Parameters.AddWithValue("@RequestWing", model.RequestWing);
                        updateCmd.Parameters.AddWithValue("@RequestLevel", model.RequestLevel);
                        updateCmd.Parameters.AddWithValue("@RequestLocation", model.RequestLocation);
                        updateCmd.Parameters.AddWithValue("@CurrentCabinetId", model.CurrentCabinetID);

                        int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            await transaction.RollbackAsync();
                            return ("Failed to update cabinet information.", 0);
                        }
                    }

                    // 5.3: Get the new cabinet_id
                    string? newCabinetId = null;
                    string getNewCabinetIdQuery = @"
                    SELECT cabinet_id 
                    FROM Cabinets 
                    WHERE department_name = @RequestedDepartment 
                    AND wing = @RequestWing 
                    AND level = @RequestLevel 
                    AND number_cab = @NumberCab";
                    using (var getCabinetCmd = new MySqlCommand(getNewCabinetIdQuery, connection, transaction))
                    {
                        getCabinetCmd.Parameters.AddWithValue("@RequestedDepartment", model.RequestedDepartment);
                        getCabinetCmd.Parameters.AddWithValue("@RequestWing", model.RequestWing);
                        getCabinetCmd.Parameters.AddWithValue("@RequestLevel", model.RequestLevel);
                        getCabinetCmd.Parameters.AddWithValue("@NumberCab", model.NumberCab);

                        newCabinetId = await getCabinetCmd.ExecuteScalarAsync() as string;
                        if (string.IsNullOrEmpty(newCabinetId))
                        {
                            await transaction.RollbackAsync();
                            return ("Failed to retrieve new cabinet ID.", 0);
                        }
                    }

                    // 5.4: Update Lockers if any were affected
                    if (lockersUpdated > 0)
                    {
                        var lockerIds = new List<(string OldId, string NewId)>();
                        string selectLockersQuery = @"
                        SELECT Id 
                        FROM Lockers 
                        WHERE DepartmentName = @CurrentDepartment 
                        AND Id LIKE CONCAT(@CurrentCabinetId, '%')";
                        using (var selectLockersCmd = new MySqlCommand(selectLockersQuery, connection, transaction))
                        {
                            selectLockersCmd.Parameters.AddWithValue("@CurrentDepartment", model.CurrentDepartment);
                            selectLockersCmd.Parameters.AddWithValue("@CurrentCabinetId", model.CurrentCabinetID);
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

                        foreach (var (oldLockerId, newLockerId) in lockerIds)
                        {
                            string updateLockerQuery = @"
                            UPDATE Lockers 
                            SET 
                                Id = @NewLockerId,
                                DepartmentName = @RequestedDepartment,
                                cabinet_id = @NewCabinetId
                            WHERE Id = @OldLockerId";
                            using (var lockerCmd = new MySqlCommand(updateLockerQuery, connection, transaction))
                            {
                                lockerCmd.Parameters.AddWithValue("@NewLockerId", newLockerId);
                                lockerCmd.Parameters.AddWithValue("@RequestedDepartment", model.RequestedDepartment);
                                lockerCmd.Parameters.AddWithValue("@NewCabinetId", newCabinetId);
                                lockerCmd.Parameters.AddWithValue("@OldLockerId", oldLockerId);
                                await lockerCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    // 5.5: Mark the reallocation request as approved
                    string approveQuery = @"
                    UPDATE Reallocation 
                    SET RequestStatus = 'Approved'
                    WHERE RequestID = @RequestID";

                    string approveQuery2 = @"
                     UPDATE Reallocation 
                     SET RequestStatus = 'Rejected'    
                     WHERE RequestID != @RequestID and CurrentCabinetID=@oldCabinetId and RequestStatus='Pending'";
                    using (var approveCmd = new MySqlCommand(approveQuery, connection, transaction))
                    {
                        approveCmd.Parameters.AddWithValue("@RequestID", reallocationId);
                        await approveCmd.ExecuteNonQueryAsync();
                    }
                    using (var approveCmd = new MySqlCommand(approveQuery2, connection, transaction))
                    {
                        approveCmd.Parameters.AddWithValue("@RequestID", reallocationId);
                        approveCmd.Parameters.AddWithValue("@oldCabinetId", model.CurrentCabinetID);
                        await approveCmd.ExecuteNonQueryAsync();
                    }

                    AdminService.ClearCache(_memoryCache, "CabinetInfo_");
                    AdminService.ClearCache(_memoryCache, "AvailableWings_");
                    AdminService.ClearCache(_memoryCache, "AvailableLockers_");
                    AdminService.ClearCache(_memoryCache, $"CurrentReservation_");

                    // Step 6: Commit the transaction
                    await transaction.CommitAsync();
                    return ($"Cabinet reallocation was successful.", reallocationId);
                }
                catch (Exception ex)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                    }
                    catch (Exception rollbackEx)
                    {
                        return ("Error during transaction rollback.", 0);
                    }
                    return ($"Error processing reallocation request: {ex.Message}", 0);
                }
            }
        }
    }
    public async Task<string> ReallocationRequest(Reallocation model)
    {
        try
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                // Step 1: Get the supervisor's department and location
                string query1 = "SELECT location, supervised_department FROM Supervisors WHERE id = @SupervisorID";
                using (var command1 = new MySqlCommand(query1, connection))
                {
                    command1.Parameters.AddWithValue("@SupervisorID", model.SupervisorID);

                    using (var reader = await command1.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string supervisorLocation = reader["location"].ToString();
                            string supervisorDepartment = reader["supervised_department"].ToString();

                            if (supervisorLocation != model.CurrentLocation || supervisorDepartment != model.CurrentDepartment)
                            {
                                return "You are not allowed to make a Reallocate Cabinet outside your Convenant of Department " + supervisorDepartment + "/" + supervisorLocation + ".";
                            }
                            if (supervisorLocation == model.RequestLocation && supervisorDepartment == model.RequestedDepartment)
                            {
                                return "You Don't have  Admin's Approve To Reallcoate a cabinet inside your Convenant of Department " + supervisorDepartment + "/" + supervisorLocation + ".";
                            }
                        }
                        else
                        {
                            return "Supervisor not found.";
                        }
                    }
                }

                // Step 2: Check if the same request already exists
                string checkQuery = @"SELECT COUNT(*) FROM Reallocation
                                  WHERE SupervisorID = @SupervisorID AND
                                        CurrentDepartment = @CurrentDepartment AND
                                        RequestedDepartment = @RequestedDepartment AND
                                        CurrentLocation = @CurrentLocation AND
                                        RequestLocation = @RequestLocation AND
                                        RequestWing = @RequestWing AND
                                        RequestLevel = @RequestLevel AND
                                        number_cab = @NumberCab AND
                                        CurrentCabinetID = @CurrentCabinetID AND
                                        RequestStatus='Pending'"
;
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@SupervisorID", model.SupervisorID);
                    checkCommand.Parameters.AddWithValue("@CurrentDepartment", model.CurrentDepartment ?? (object)DBNull.Value);
                    checkCommand.Parameters.AddWithValue("@RequestedDepartment", model.RequestedDepartment ?? (object)DBNull.Value);
                    checkCommand.Parameters.AddWithValue("@CurrentLocation", model.CurrentLocation ?? (object)DBNull.Value);
                    checkCommand.Parameters.AddWithValue("@RequestLocation", model.RequestLocation ?? (object)DBNull.Value);
                    checkCommand.Parameters.AddWithValue("@RequestWing", model.RequestWing ?? (object)DBNull.Value);
                    checkCommand.Parameters.AddWithValue("@RequestLevel", model.RequestLevel);
                    checkCommand.Parameters.AddWithValue("@NumberCab", model.NumberCab);
                    checkCommand.Parameters.AddWithValue("@CurrentCabinetID", model.CurrentCabinetID?.Trim() ?? (object)DBNull.Value);

                    var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                    if (count > 0)
                    {
                        return "This request has already been submitted.";
                    }
                }

                // Step 3: Insert the request
                string insertQuery = @"INSERT INTO Reallocation 
                                (SupervisorID, CurrentDepartment, RequestedDepartment, 
                                 RequestLocation, CurrentLocation, RequestWing, RequestLevel, 
                                 number_cab, CurrentCabinetID) 
                                VALUES 
                                (@SupervisorID, @CurrentDepartment, @RequestedDepartment, 
                                 @RequestLocation, @CurrentLocation, @RequestWing, @RequestLevel,
                                 @NumberCab, @CurrentCabinetID)";

                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@SupervisorID", model.SupervisorID);
                    command.Parameters.AddWithValue("@CurrentDepartment", model.CurrentDepartment ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@RequestedDepartment", model.RequestedDepartment ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CurrentLocation", model.CurrentLocation ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@RequestLocation", model.RequestLocation ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@RequestWing", model.RequestWing ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@RequestLevel", model.RequestLevel);
                    command.Parameters.AddWithValue("@NumberCab", model.NumberCab);
                    command.Parameters.AddWithValue("@CurrentCabinetID", model.CurrentCabinetID ?? (object)DBNull.Value);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    AdminService.ClearCache(_memoryCache, "ReallocationRequests_");
                    AdminService.ClearCache(_memoryCache, "ReallocationResponse");

                    return rowsAffected > 0 ? "Request sent successfully! Wait Admin Response" : "Failed to send request.";
                }
            }
        }
        catch (Exception ex)
        {
            return $"Error sending request: {ex.Message}";
        }
    }
    public async Task SendToAdmin(int reportId)
    {
        try
        {
            var query = "Update Reports set SentToAdmin=1  WHERE Id = @Id";

            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", reportId);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                AdminService.ClearCache(_memoryCache, "Reports");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error sending report to admin: {ex.Message}");
        }
    }
    public async Task<(string? Location, string? Department)> GetSupervisorLocationAndDepartment(int userId)
    {
        string? location = null;
        string? department = null;
        string cacheKey = $"SupervisorLocationDepartment_{userId}";
        if (_memoryCache.TryGetValue(cacheKey, out (string? Location, string? Department) cachedValues))
        {
            return cachedValues;
        }

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();

            string query = "SELECT location, supervised_department FROM Supervisors WHERE id = @userId";
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        location = reader["location"]?.ToString();
                        department = reader["supervised_department"]?.ToString();
                    }
                }
            }
        }
        _memoryCache.Set(cacheKey, (location, department), TimeSpan.FromMinutes(3));
        return (location, department);
    }
    public async Task<List<Reallocation>> ReallocationRequestsInfo(int? id, string? filter, string? location, string? department)
    {
        var reallocations = new List<Reallocation>();
        string cacheKey = $"ReallocationRequests_{id}_{filter}_{location}_{department}";
        if (_memoryCache.TryGetValue(cacheKey, out List<Reallocation> cachedReallocations))
        {
            return cachedReallocations;
        }
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();

            var query = @"SELECT RequestID, SupervisorID, CurrentDepartment, RequestLocation, CurrentLocation,
                             RequestedDepartment, CurrentCabinetID, NewCabinetID, RequestStatus, RequestDate
                      FROM Reallocation
                      WHERE RequestedDepartment != CurrentDepartment AND SupervisorID = @id and CurrentDepartment=@department and CurrentLocation=@location ";

            if (!string.IsNullOrEmpty(filter) && filter.ToLower() != "all")
            {
                query += " AND RequestStatus = @filter";
            }

            query += " ORDER BY RequestDate DESC";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@id", id ?? 0);
                command.Parameters.AddWithValue("@department", department ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@location", location ?? (object)DBNull.Value);


                if (!string.IsNullOrEmpty(filter) && filter.ToLower() != "all")
                {
                    // Normalize filter ( pending -- Pending)
                    filter = char.ToUpper(filter[0]) + filter.Substring(1).ToLower();
                    command.Parameters.AddWithValue("@filter", filter);
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        reallocations.Add(new Reallocation
                        {
                            RequestID = reader["RequestID"] != DBNull.Value ? Convert.ToInt32(reader["RequestID"]) : 0,
                            SupervisorID = reader["SupervisorID"] != DBNull.Value ? Convert.ToInt32(reader["SupervisorID"]) : 0,
                            CurrentDepartment = reader["CurrentDepartment"]?.ToString(),
                            RequestedDepartment = reader["RequestedDepartment"]?.ToString(),
                            CurrentCabinetID = reader["CurrentCabinetID"]?.ToString(),
                            NewCabinetID = reader["NewCabinetID"]?.ToString(),
                            CurrentLocation = reader["CurrentLocation"]?.ToString(),
                            RequestLocation = reader["RequestLocation"]?.ToString(),
                            RequestStatus = reader["RequestStatus"] != DBNull.Value
                                ? (RequestStatus)Enum.Parse(typeof(RequestStatus), reader["RequestStatus"].ToString(), true)
                                : RequestStatus.PENDING,
                            RequestDate = reader["RequestDate"] != DBNull.Value
                                ? Convert.ToDateTime(reader["RequestDate"]).AddHours(3)
                                : DateTime.MinValue
                        });
                    }
                }
            }
        }
        _memoryCache.Set(cacheKey, reallocations, TimeSpan.FromMinutes(3));
        return reallocations;
    }
    public async Task<List<BlockedStudent>> BlockedStudents()
    {
        var blockedStudents = new List<BlockedStudent>();
        string cacheKey = "BlockedStudents";
        if (_memoryCache.TryGetValue(cacheKey, out List<BlockedStudent> cachedBlockedStudents))
        {
            return cachedBlockedStudents;
        }

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            string query = @"
              SELECT 
    bs.*, 
    s.name AS student_name,
    s.email,
    s.department, 
    s.Location, 
    s.Major,  
    su.name AS supervisor_name
FROM BlockList bs
JOIN Students s ON bs.student_id = s.id
JOIN Supervisors su ON bs.blocked_by = su.id
                ";
            await connection.OpenAsync();
            using (var command = new MySqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    blockedStudents.Add(new BlockedStudent
                    {
                        //Id = reader.GetInt32("id"),
                        StudentId = reader.GetInt32("student_id"),
                        Student = new Student(
                        reader.GetInt32("student_id"),
                        reader.GetString("student_name"),
                        reader.GetString("email"),
                        reader.GetString("Major"),
                        reader.GetString("department"),
                        reader.GetString("Location")
                    ),
                        //BlockedUntil = reader.GetDateTime("BlockedUntil"),
                        BlockedBy = reader.GetString("supervisor_name") // aliased column
                    });

                }
            }
        }
        _memoryCache.Set(cacheKey, blockedStudents, TimeSpan.FromMinutes(3));
        return blockedStudents;

    }
    public Student GetStudentById(int id)
    {
        Student student = null;

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            string query = "SELECT * FROM Students WHERE id = @id";
            var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            connection.Open();
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                student = new Student(
                    Convert.ToInt32(reader["id"]),
                    reader["name"].ToString(),
                    reader["email"].ToString(),
                    reader["department"]?.ToString() ?? "",
                   reader["Location"]?.ToString() ?? ""
                )
                {
                    LockerId = reader["locker_id"].ToString(),
                    IsBlocked = IsStudentBlocked(id)
                };
            }
        }

        return student;
    }
    public bool IsStudentBlocked(int id)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            string query = "SELECT COUNT(*) FROM BlockList WHERE student_id = @id";
            var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            connection.Open();
            var result = Convert.ToInt32(command.ExecuteScalar());
            return result > 0;
        }
    }
    public string BlockStudent(int id, int? userId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            // Check if the student is already blocked
            if (IsStudentBlocked(id))
            {
                return "Student is already blocked.";
            }

            string checkQuery = @"
            SELECT COUNT(*) 
            FROM Students s
            JOIN Supervisors u ON u.id = @userId
            WHERE s.id = @studentId 
              AND s.department = u.supervised_department 
              AND s.Location = u.location";

            var checkCommand = new MySqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@studentId", id);
            checkCommand.Parameters.AddWithValue("@userId", userId);

            connection.Open();
            int matchCount = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (matchCount > 0)
            {
                string insertQuery = "INSERT INTO BlockList (student_id, blocked_by) VALUES (@id, @userId)";
                var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@id", id);
                insertCommand.Parameters.AddWithValue("@userId", userId);

                insertCommand.ExecuteNonQuery();

                AdminService.ClearCache(_memoryCache, "BlockedStudents");
                AdminService.ClearCache(_memoryCache, "IsStudentBlocked");
                AdminService.ClearCache(_memoryCache, $"CurrentReservation_{id}");
                AdminService.ClearCache(_memoryCache, "AvailableWings_");
                AdminService.ClearCache(_memoryCache, "AvailableLockers_");
                AdminService.ClearCache(_memoryCache, $"HasLocker-{id}");

                return "Student successfully blocked.";
            }
            else
            {
                return "Cannot block student outside your department/location.";
            }
        }
    }
    public string UnblockStudent(int id, int? userId)
    {
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            //check if the student is not blocked
            if (!IsStudentBlocked(id))
            {
                return "Student is not blocked.";
            }

            string checkQuery = @"
            SELECT COUNT(*) 
            FROM Students s
            JOIN Supervisors u ON u.id = @userId
            WHERE s.id = @studentId 
              AND s.department = u.supervised_department 
              AND s.Location = u.location";

            var checkCommand = new MySqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@studentId", id);
            checkCommand.Parameters.AddWithValue("@userId", userId);

            connection.Open();
            int matchCount = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (matchCount > 0)
            {
                string insertQuery = "DELETE FROM BlockList WHERE student_id = @id";
                var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@id", id);
                insertCommand.Parameters.AddWithValue("@userId", userId);

                AdminService.ClearCache(_memoryCache, "BlockedStudents");
                AdminService.ClearCache(_memoryCache, "IsStudentBlocked");
                AdminService.ClearCache(_memoryCache, $"CurrentReservation_{id}");
                AdminService.ClearCache(_memoryCache, "AvailableWings_");
                AdminService.ClearCache(_memoryCache, "AvailableLockers_");
                AdminService.ClearCache(_memoryCache, $"HasLocker-{id}");

                insertCommand.ExecuteNonQuery();
                return "Student successfully Unblocked.";
            }
            else
            {
                return "Cannot Unblock student outside your department/location.";
            }
        }
    }
    public async Task<List<Cabinet>> ViewCabinetInfo(int? userId, string? searchCab = null, string? location = null, int? level = null, string? department = null, string? status = null, string? wing = null)
    {
        var cabinets = new List<Cabinet>();
        string cacheKey = $"CabinetInfo_{userId}_{searchCab}_{location}_{level}_{department}_{status}_{wing}";
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = @"
            SELECT c.number_cab, c.wing, c.level, c.location, c.department_name, 
                   c.cabinet_id, c.Capacity, c.status 
            FROM Cabinets c
            JOIN Supervisors s ON c.department_name = s.supervised_department AND c.location = s.location 
            WHERE s.id = @userId";

            var command = new MySqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@userId", userId ?? 0);

            if (!string.IsNullOrEmpty(searchCab))
            {
                query += " AND c.cabinet_id LIKE @searchCab";
                command.Parameters.AddWithValue("@searchCab", "%" + searchCab.Trim() + "%");
            }

            if (!string.IsNullOrEmpty(location) && location != "All")
            {
                query += " AND c.location = @location";
                command.Parameters.AddWithValue("@location", location);
            }

            if (level.HasValue)
            {
                query += " AND c.level = @level";
                command.Parameters.AddWithValue("@level", level.Value);
            }

            if (!string.IsNullOrEmpty(department) && department != "All Dep")
            {
                query += " AND c.department_name = @department";
                command.Parameters.AddWithValue("@department", department);
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query += " AND c.status = @status";
                command.Parameters.AddWithValue("@status", status);
            }

            if (!string.IsNullOrEmpty(wing) && wing != "All")
            {
                query += " AND c.wing = @wing";
                command.Parameters.AddWithValue("@wing", wing);
            }

            query += " ORDER BY c.department_name";
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
        _memoryCache.Set(cacheKey, cabinets, TimeSpan.FromMinutes(3));
        return cabinets;
    }
    public async Task<DepartmentInfo> GetDepartmentInfo(int userId)
    {
        string cacheKey = $"DepartmentInfo_{userId}";
        if (_memoryCache.TryGetValue(cacheKey, out DepartmentInfo cachedInfo))
        {
            return cachedInfo;
        }
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = @"SELECT supervised_department, location FROM Supervisors WHERE id = @userId";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        _memoryCache.Set(cacheKey, new DepartmentInfo
                        {
                            DepartmentName = reader.GetString("supervised_department"),
                            Location = reader.GetString("location")
                        }, TimeSpan.FromMinutes(3));

                        return new DepartmentInfo
                        {
                            DepartmentName = reader.GetString("supervised_department"),
                            Location = reader.GetString("location")
                        };
                    }
                }
            }
        }

        return null;
    }
    public bool HasCovenant(int? userId)
    {
        string cacheKey = $"HasCovenant_{userId}";
        if (_memoryCache.TryGetValue(cacheKey, out bool hasCovenant))
        {
            return hasCovenant;
        }
        try

        {
            int count;

            string query = @"SELECT COUNT(*) FROM Supervisors 
                 WHERE id = @userId
                 AND supervised_department IS NOT NULL 
                ";

            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);

                    count = Convert.ToInt32(cmd.ExecuteScalar());
                    _memoryCache.Set(cacheKey, count > 0, TimeSpan.FromMinutes(3));
                    return count > 0;

                }
            }

        }
        catch (Exception ex)
        {
            return false;
        }

    }
}
