using JUSTLockers.Classes;
using JUSTLockers.Service;
using MySqlConnector;
namespace JUSTLockers.Services;


public class SupervisorService : ISupervisorService
    {
        private readonly IConfiguration _configuration;
    private readonly AdminService _adminService;

    public SupervisorService(IConfiguration configuration , AdminService adminService)
        {
            _configuration = configuration;
            _adminService = adminService;
        }
        public void Login()
        {
            throw new NotImplementedException();
        }

   
    public async Task<List<Report>> ViewReportedIssues(int? userId)
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

            using (var command = new MySqlCommand(query, connection)) {
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
        return reports;
    }

    public async Task<List<Report>> TheftIssues(string filter)
    {
        var reports = new List<Report>();

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
        return reports;

    }





    public async Task<List<Student>> ViewAllStudentReservations(int? userId, int? searchstu = null)
    {
        var students = new List<Student>();

        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();

            var query = @"
            SELECT s.id, s.name, s.email, s.Major, s.locker_id
            FROM Students s
            JOIN Supervisors su 
              ON s.department = su.supervised_department 
              AND s.Location = su.location
            WHERE su.id = @userId and s.locker_id is not null";

            if (searchstu.HasValue)
            {
                query += " AND s.id = @searchstu";
            }

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@userId", userId);

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

        return students;
    }



    public async Task<(string message , int requestId)> ReallocationRequestFormSameDep(Reallocation model)
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
                                return ("Supervisor not found.",0);
                            }
                        }
                    }

                    // Step 2: Check if the supervisor is authorized
                    if (supervisorLocation != model.CurrentLocation || supervisorDepartment != model.CurrentDepartment)
                    {
                        return ($"You are not allowed to reallocate a cabinet outside your department and location: {supervisorDepartment}/{supervisorLocation}.", 0);
                    }

                    if (supervisorLocation != model.RequestLocation || supervisorDepartment != model.RequestedDepartment)
                    {
                        return ($"You need Admin approval to reallocate a cabinet outside your department and location: {supervisorDepartment}/{supervisorLocation}.",0);
                    }

                   

                    // inserte this reallocation request 
                    string insertQuery = @"
                    INSERT INTO Reallocation
                    (SupervisorID, CurrentDepartment, RequestedDepartment, 
                    RequestLocation, CurrentLocation, RequestWing, RequestLevel, 
                    number_cab, CurrentCabinetID)
                    VALUES
                    (@SupervisorID, @CurrentDepartment, @RequestedDepartment, 
                    @RequestLocation, @CurrentLocation, @RequestWing, @RequestLevel,
                    @NumberCab, @CurrentCabinetID);
                    SELECT LAST_INSERT_ID();"; // Retrieve the last inserted ID

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

                        var reallocationId = Convert.ToInt32(await command.ExecuteScalarAsync()); // Get the inserted ID
                        if (reallocationId > 0)
                        {
                            await transaction.CommitAsync();
                            await _adminService.ApproveRequestReallocation(reallocationId);

                            return ($"Cabinet reallocation was successful.",reallocationId);
                        }
                    }
                   return ($"Cabinet reallocation was successful.", 0);


                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return ($"Error processing reallocation request: {ex.Message}",0);
                }
            }
        }
    }










    public void Notify()
        {
            throw new NotImplementedException();
        }
    //here 

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
                                        CurrentCabinetID = @CurrentCabinetID";

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
                    checkCommand.Parameters.AddWithValue("@CurrentCabinetID", model.CurrentCabinetID ?? (object)DBNull.Value);

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
                


            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error sending report to admin: {ex.Message}");
        }
    }







    public async Task<List<BlockedStudent>> BlockedStudents()
    {


        var blockedStudents = new List<BlockedStudent>();
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
        return blockedStudents;

    }








        public void CancelStudentReservation()
        {
            throw new NotImplementedException();
        }

        public void ManualReserve()
        {
            throw new NotImplementedException();
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
                    LockerId = reader["locker_id"].ToString() ,
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
        using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();
            var query = @"
            SELECT c.number_cab, c.wing, c.level, c.location, c.department_name, 
                   c.cabinet_id, c.Capacity, c.status 
            FROM Cabinets c
            JOIN Supervisors s ON c.department_name = s.supervised_department
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
        return cabinets;
    }

    public async Task<DepartmentInfo> GetDepartmentInfo(int userId)
    {
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

    public void ViewNotifications()
    {
        throw new NotImplementedException();
    }

    public Task<List<Locker>> ViewAvailableLockers(string departmentName)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CancelReservation(int studentId)
    {
        throw new NotImplementedException();
    }

    //public void UnblockStudent(int id)
    //{
    //    using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
    //    {
    //        string query = "DELETE FROM BlockList WHERE student_id = @id";
    //        var command = new MySqlCommand(query, connection);
    //        command.Parameters.AddWithValue("@id", id);

    //        connection.Open();
    //        command.ExecuteNonQuery();
    //    }
    //}
}
