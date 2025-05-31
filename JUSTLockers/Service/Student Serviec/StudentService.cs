using JUSTLockers.Classes;
using JUSTLockers.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MySqlConnector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JUSTLockers.Service
{
    public class StudentService : IStudentService
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;
        private readonly AdminService adminService;
        public StudentService(IConfiguration configuration,
            IMemoryCache? memoryCache, AdminService adminService)
        {
            _configuration = configuration;
            _memoryCache = memoryCache;
            this.adminService = adminService;
        }
        public async Task<bool> SaveReportAsync(int ReportID, int reporterId, string LockerId, string problemType, string Subject, string description, IFormFile imageFile)
        {
            try
            {
                byte[] imageData = null;
                string mimeType = null; // To store the MIME type

                if (imageFile != null && imageFile.Length > 0)
                {
                    // Capture the MIME type
                    mimeType = imageFile.ContentType;

                    // Convert image to byte array
                    using (var ms = new MemoryStream())
                    {
                        await imageFile.CopyToAsync(ms);
                        imageData = ms.ToArray();
                    }
                }

                // Save the report data including image data and MIME type
                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    string query = @"INSERT INTO Reports (Id, ReporterId, LockerId, Type,Subject, Statement, ImageData, ImageMimeType) 
                             VALUES (@ReportID, @ReporterId, @LockerId, @ProblemType,@Subject, @Description, @ImageFile, @ImageMimeType)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ReportID", ReportID);
                        command.Parameters.AddWithValue("@ReporterId", reporterId);
                        command.Parameters.AddWithValue("@LockerId", LockerId);
                        command.Parameters.AddWithValue("@ProblemType", problemType);
                        command.Parameters.AddWithValue("@Subject", Subject);
                        command.Parameters.AddWithValue("@Description", description);

                        command.Parameters.Add("@ImageFile", MySqlDbType.Blob).Value = imageData;
                        command.Parameters.AddWithValue("@ImageMimeType", mimeType);

                        await connection.OpenAsync();
                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        AdminService.ClearCache(_memoryCache, "Reports");

                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving report: {ex.Message}");
                return false;
            }
        }
        public async Task DeleteReport(int reportId)
        {
            var query = "DELETE FROM Reports WHERE Id = @Id";
            var student = await adminService.GetReportDetails(reportId); 

            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", reportId);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
            AdminService.ClearCache(_memoryCache, "Reports");
            //_memoryCache.Remove($"ViewAllReports_{student.ReporterId}"); 
        }
        public async Task<(string email, Report report)> GetReportByAsync(int reportId)
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                var query = @"
            SELECT 
                r.Id AS ReportId,
                r.ReporterId AS ReporterId,
                r.LockerId AS LockerId,
                r.ResolvedDetails AS ResolutionDetails,
                s.email AS StudentEmail
            FROM 
                Reports r
            JOIN 
                Students s ON r.ReporterId = s.id
            WHERE 
                r.Id = @ReportId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ReportId", reportId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var report = new Report
                            {
                                ReportId = reader.GetInt32("ReportId"),
                                ReporterId = reader.GetInt32("ReporterId"),
                                LockerId = reader.GetString("LockerId"),
                                ResolutionDetails = reader.IsDBNull(reader.GetOrdinal("ResolutionDetails"))
                                    ? null
                                    : reader.GetString("ResolutionDetails")
                            };
                            var email = reader.GetString("StudentEmail");

                            return (email, report);
                        }
                        else
                        {
                            throw new KeyNotFoundException($"Report ID {reportId} vanished! Maybe it’s hiding in a locker? 😺");
                        }
                    }
                }
            }
        }
        public async Task<List<Report>> ViewAllReports(int? studentId)
        {
            var reports = new List<Report>();
            string cached = $"ViewAllReports_{studentId}"; // Unique key per student
            if (_memoryCache.TryGetValue(cached, out List<Report> cachedReports))
            {
                return cachedReports;
            }
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                var query = "SELECT * FROM Reports where ReporterId=@studentId";


                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@studentId", studentId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var report = new Report
                            {
                                Reporter = null,
                                Locker = null,
                                ReportId = reader.GetInt32("Id"),
                                ReporterId = reader.GetInt32("ReporterId"),
                                LockerId = reader.GetString("LockerId"),

                                Type = Enum.TryParse(reader.GetString("Type"), out ReportType type) ? type : ReportType.OTHER,
                                Status = Enum.TryParse(reader.GetString("Status"), out ReportStatus status) ? status : ReportStatus.REPORTED,

                                Subject = reader.GetString("Subject"),
                                Statement = reader.GetString("Statement"),
                                ReportDate = reader.GetDateTime("ReportDate"),
                                ResolvedDate = reader.IsDBNull(reader.GetOrdinal("ResolvedDate")) ? null : reader.GetDateTime("ResolvedDate"),
                                ResolutionDetails = reader.IsDBNull(reader.GetOrdinal("ResolvedDetails")) ? null : reader.GetString("ResolvedDetails"),
                                ImageData = reader.IsDBNull(reader.GetOrdinal("ImageData")) ? null : (byte[])reader["ImageData"],
                                ImageMimeType = reader.IsDBNull(reader.GetOrdinal("ImageMimeType")) ? null : reader.GetString("ImageMimeType")
                            };

                            reports.Add(report);
                        }
                    }
                }
            }
            _memoryCache.Set(cached, reports, TimeSpan.FromMinutes(3));
            return reports;
        }
        public async Task<bool> CancelReservation(int studentId , string status)
        {
            AdminService.ClearCache(_memoryCache, $"CurrentReservation_{studentId}");
            AdminService.ClearCache(_memoryCache, $"StudentReservation_");
            var departmentInfo = await GetDepartmentInfo(studentId); // Get department info to invalidate related caches
            if (departmentInfo != null)
            {
                AdminService.ClearCache(_memoryCache, "AvailableWings_");
                AdminService.ClearCache(_memoryCache, "AvailableLockers_"); // Invalidate available lockers cache
                AdminService.ClearCache(_memoryCache, $"HasLocker-{studentId}");
                // Invalidate has locker cache
            }
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Get the reservation to cancel along with cabinet info
                        var reservationQuery = @"
                    SELECT r.Id, r.LockerId, l.cabinet_id 
                    FROM Reservations r
                    JOIN Lockers l ON r.LockerId = l.Id
                    WHERE r.StudentId = @StudentId 
                    AND r.Status = 'RESERVED'";

                        string reservationId = null;
                        string lockerId = null;
                        string cabinetId = null;

                        using (var cmd = new MySqlCommand(reservationQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@StudentId", studentId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    reservationId = reader.GetString("Id");
                                    lockerId = reader.GetString("LockerId");
                                    cabinetId = reader.GetString("cabinet_id");
                                }
                            }
                        }

                        if (lockerId == null) return false;

                        // Update student record
                        var updateStudentQuery = "UPDATE Students SET locker_id = NULL WHERE id = @StudentId";
                        using (var studentCmd = new MySqlCommand(updateStudentQuery, connection, transaction))
                        {
                            studentCmd.Parameters.AddWithValue("@StudentId", studentId);
                            await studentCmd.ExecuteNonQueryAsync();
                        }

                        // Delete the reservation record
                        var checkCanceledReservationQuery = @"
                            SELECT Id 
                            FROM Reservations 
                            WHERE Id = @reservationId AND Status = 'CANCELED'";

                        string canceledReservationId = null;

                        using (var checkCmd = new MySqlCommand(checkCanceledReservationQuery, connection, transaction))
                        {
                            checkCmd.Parameters.AddWithValue("@reservationId", reservationId);
                            using (var reader = await checkCmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    canceledReservationId = reader.GetString("Id");
                                }
                            }
                        }

                        if (canceledReservationId != null)
                        {
                            var deleteCanceledReservationQuery = "DELETE FROM Reservations WHERE Id = @ReservationId AND Status = 'CANCELED'";
                            using (var deleteCmd = new MySqlCommand(deleteCanceledReservationQuery, connection, transaction))
                            {
                                deleteCmd.Parameters.AddWithValue("@ReservationId", canceledReservationId);
                                await deleteCmd.ExecuteNonQueryAsync();
                            }
                        }

                        // Proceed to update or create the new reservation
                        var updateReservationQuery = "UPDATE Reservations SET Status = 'CANCELED' WHERE Id = @ReservationId";
                        using (var reservationCmd = new MySqlCommand(updateReservationQuery, connection, transaction))
                        {
                            reservationCmd.Parameters.AddWithValue("@ReservationId", reservationId);
                            await reservationCmd.ExecuteNonQueryAsync();
                        }

                        var updateLockerQuery = "UPDATE Lockers SET Status = @status WHERE Id = @LockerId";
                        using (var lockerCmd = new MySqlCommand(updateLockerQuery, connection, transaction))
                        {
                            lockerCmd.Parameters.AddWithValue("@LockerId", lockerId);
                            lockerCmd.Parameters.AddWithValue("@status", status);
                            await lockerCmd.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();

                        return true;
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }
        public async Task<DepartmentInfo> GetDepartmentInfo(int studentId)
        {
            string cached = $"GetDepartmentInfo-{studentId}";
            if(_memoryCache.TryGetValue(cached, out DepartmentInfo cachedvalues))
            {
                return cachedvalues;
            }

            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                var query = @"SELECT department, Location FROM Students WHERE id = @StudentId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StudentId", studentId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            _memoryCache.Set(cached, new DepartmentInfo
                            {
                                DepartmentName = reader.GetString("department"),
                                Location = reader.GetString("Location")
                            }, TimeSpan.FromMinutes(3));

                            return new DepartmentInfo
                            {
                                DepartmentName = reader.GetString("department"),
                                Location = reader.GetString("Location")
                            };
                        }
                    }
                }
            }
            return null;
        }
        public async Task<List<WingInfo>> GetAvailableWingsAndLevels(string departmentName, string location)
        {
            var wings = new List<WingInfo>();
            string cacheKey = $"AvailableWings_{departmentName}_{location}";
            if (_memoryCache.TryGetValue(cacheKey, out List<WingInfo> cachedWings))
            {
                return cachedWings;
            }

            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                // First query: Calculate available lockers per cabinet
                var subquery = @"
                    SELECT 
                        c.cabinet_id,
                        GREATEST(c.Capacity - COALESCE(c.ReservedLockers, 0), 0) + 
                        COUNT(DISTINCT CASE WHEN l.Status = 'EMPTY' THEN l.Id END) AS CabinetAvailableLockers
                    FROM Cabinets c
                    LEFT JOIN Lockers l ON c.cabinet_id = l.cabinet_id
                    WHERE c.department_name = @DepartmentName
                        AND c.location = @Location
                        AND c.status = 'IN_SERVICE'
                    GROUP BY c.cabinet_id";

                // Second query: Aggregate by wing and level
                var query = @"
                    SELECT 
                        c.wing AS Wing, 
                        c.level AS Level, 
                        SUM(sub.CabinetAvailableLockers) AS AvailableLockers
                    FROM Cabinets c
                    JOIN (" + subquery + @") sub ON c.cabinet_id = sub.cabinet_id
                    WHERE c.department_name = @DepartmentName
                        AND c.location = @Location
                        AND c.status = 'IN_SERVICE'
                    GROUP BY c.wing, c.level
                    HAVING AvailableLockers > 0
                    ORDER BY c.wing, c.level";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DepartmentName", departmentName);
                    command.Parameters.AddWithValue("@Location", location);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            wings.Add(new WingInfo
                            {
                                Wing = reader.GetString("Wing"),
                                Level = reader.GetInt32("Level"),
                                AvailableLockers = reader.GetInt32("AvailableLockers")
                            });
                        }
                    }
                }
            }
            _memoryCache.Set(cacheKey, wings, TimeSpan.FromMinutes(3)); // Cache for 3 minutes
            return wings;
        }
        public async Task<Reservation> GetCurrentReservationAsync(int studentId)
        {
            //use inMemoryCache to store the reservation data for 5 minutes
            string cacheKey = $"CurrentReservation_{studentId}";
            if (_memoryCache.TryGetValue(cacheKey, out Reservation cachedReservation))
            {
                return cachedReservation;
            }

            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                var query = @"SELECT r.Id, r.LockerId, r.ReservationDate, r.Status, r.StudentId,
                     l.DepartmentName,c.location,c.wing, c.level, s.name AS StudentName
                     FROM Reservations r
                     JOIN Lockers l ON r.LockerId = l.Id
                     JOIN Students s ON r.StudentId = s.id
                     JOIN Cabinets c ON l.Cabinet_id = c.cabinet_id
                     WHERE r.StudentId = @StudentId 
                     AND r.Status = 'RESERVED'";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StudentId", studentId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var reservation = new Reservation
                            {
                                Id = reader.GetString("Id"),
                                LockerId = reader.GetString("LockerId"),
                                Date = reader.GetDateTime("ReservationDate"),
                                Status = (ReservationStatus)Enum.Parse(typeof(ReservationStatus), reader.GetString("Status")),
                                StudentId = reader.GetInt32("StudentId"),
                                StudentName = reader.GetString("StudentName"),
                                Department = reader.GetString("DepartmentName"),
                                Location = reader.GetString("location"),
                                Wing = reader.GetString("wing"),
                                Level = reader.GetInt32("level")
                            };
                            _memoryCache.Set(cacheKey, reservation, TimeSpan.FromMinutes(5)); // Cache for 5 minutes
                            return reservation;

                        }
                    }
                }
            }
            return null;
        }
        public async Task<string> ReserveLockerInWingAndLevel(int studentId, string departmentName, string location, string wing, int level)
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Check if student is in the BlockList
                        var checkBlockQuery = @"SELECT COUNT(*) FROM BlockList WHERE student_id = @StudentId";
                        using (var checkCmd = new MySqlCommand(checkBlockQuery, connection, transaction))
                        {
                            checkCmd.Parameters.AddWithValue("@StudentId", studentId);
                            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                            if (count > 0)
                            {
                                return null; // Student is blocked, cannot reserve
                            }
                        }

                        //check if student already has a locker reserved
                        var hasLockerQuery = @"
                            SELECT COUNT(*) 
                            FROM Reservations 
                            WHERE StudentId = @StudentId AND Status = 'RESERVED'";
                        using (var hasLockerCmd = new MySqlCommand(hasLockerQuery, connection, transaction))
                            {
                            hasLockerCmd.Parameters.AddWithValue("@StudentId", studentId);
                            var lockerCount = Convert.ToInt32(await hasLockerCmd.ExecuteScalarAsync());
                            if (lockerCount > 0)
                            {
                                return null; // Student already has a locker reserved
                            }
                        }



                        // Find a cabinet with either available capacity or existing EMPTY lockers
                        var findCabinetQuery = @"
                    SELECT c.cabinet_id, c.number_cab, c.Capacity, c.ReservedLockers,
                           l.Id AS EmptyLockerId
                    FROM Cabinets c
                    LEFT JOIN Lockers l ON c.cabinet_id = l.cabinet_id AND l.Status = 'EMPTY'
                    WHERE c.department_name = @DepartmentName
                        AND c.location = @Location
                        AND c.wing = @Wing
                        AND c.level = @Level
                        AND c.status = 'IN_SERVICE'
                        AND (c.Capacity > COALESCE(c.ReservedLockers, 0) OR l.Id IS NOT NULL)
                    LIMIT 1";

                        string cabinetId = null;
                        int cabinetNumber = 0;
                        int capacity = 0;
                        int reservedLockers = 0;
                        string lockerId = null;

                        using (var findCmd = new MySqlCommand(findCabinetQuery, connection, transaction))
                        {
                            findCmd.Parameters.AddWithValue("@DepartmentName", departmentName);
                            findCmd.Parameters.AddWithValue("@Location", location);
                            findCmd.Parameters.AddWithValue("@Wing", wing);
                            findCmd.Parameters.AddWithValue("@Level", level);

                            using (var reader = await findCmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    cabinetId = reader.GetString("cabinet_id");
                                    cabinetNumber = reader.GetInt32("number_cab");
                                    capacity = reader.GetInt32("Capacity");
                                    reservedLockers = reader.GetInt32("ReservedLockers");

                                    // Use existing empty locker if available
                                    if (!reader.IsDBNull(reader.GetOrdinal("EmptyLockerId")))
                                    {
                                        lockerId = reader.GetString("EmptyLockerId");
                                    }
                                }
                            }
                        }

                        if (cabinetId == null)
                        {
                            return null; // No available cabinets
                        }

                        bool isNewLocker = false;
                        // If we didn't find an existing empty locker, create a new one
                        if (lockerId == null)
                        {
                            if (reservedLockers >= capacity)
                            {
                                return null; // No capacity available
                            }

                            if (reservedLockers < 10)
                                lockerId = cabinetId + " -L" + level + "0" + (reservedLockers + 1);
                            else
                                lockerId = cabinetId + " -L" + level + (reservedLockers + 1);
                            isNewLocker = true;

                            var insertLockerQuery = @"
                        INSERT INTO Lockers (Id, DepartmentName, Cabinet_id, Status)
                        VALUES (@LockerId, @DepartmentName, @CabinetId, 'RESERVED')";

                            using (var insertCmd = new MySqlCommand(insertLockerQuery, connection, transaction))
                            {
                                insertCmd.Parameters.AddWithValue("@LockerId", lockerId);
                                insertCmd.Parameters.AddWithValue("@DepartmentName", departmentName);
                                insertCmd.Parameters.AddWithValue("@CabinetId", cabinetId);
                                await insertCmd.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            isNewLocker = false;
                            // Update existing locker to RESERVED
                            var updateLockerQuery = @"
                        UPDATE Lockers
                        SET Status = 'RESERVED'
                        WHERE Id = @LockerId";

                            using (var updateCmd = new MySqlCommand(updateLockerQuery, connection, transaction))
                            {
                                updateCmd.Parameters.AddWithValue("@LockerId", lockerId);
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }

                        // Update cabinet's ReservedLockers count if we created a new locker
                        if (isNewLocker)
                        {
                            var updateCabinetQuery = @"
                        UPDATE Cabinets
                        SET ReservedLockers = COALESCE(ReservedLockers, 0) + 1
                        WHERE cabinet_id = @CabinetId";

                            using (var cabinetCmd = new MySqlCommand(updateCabinetQuery, connection, transaction))
                            {
                                cabinetCmd.Parameters.AddWithValue("@CabinetId", cabinetId);
                                await cabinetCmd.ExecuteNonQueryAsync();
                            }
                        }

                        // Check if a reservation already exists for the same lockerId and studentId
                        var checkReservationQuery = @"
                            SELECT COUNT(*) 
                            FROM Reservations 
                            WHERE LockerId = @LockerId AND StudentId = @StudentId";

                        int existingReservationCount;
                        using (var checkCmd = new MySqlCommand(checkReservationQuery, connection, transaction))
                        {
                            checkCmd.Parameters.AddWithValue("@LockerId", lockerId);
                            checkCmd.Parameters.AddWithValue("@StudentId", studentId);
                            existingReservationCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        }

                        if (existingReservationCount > 0)
                        {
                            // Update the existing reservation
                            var updateReservationQuery = @"
                                UPDATE Reservations 
                                SET ReservationDate = @ReservationDate, Status = 'RESERVED'
                                WHERE LockerId = @LockerId AND StudentId = @StudentId";

                            using (var updateCmd = new MySqlCommand(updateReservationQuery, connection, transaction))
                            {
                                updateCmd.Parameters.AddWithValue("@LockerId", lockerId);
                                updateCmd.Parameters.AddWithValue("@StudentId", studentId);
                                updateCmd.Parameters.AddWithValue("@ReservationDate", DateTime.Now);
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // Create a new reservation record
                            var reservationId = "R-" + lockerId + "-" + studentId;
                            var insertReservationQuery = @"
                                INSERT INTO Reservations (Id, StudentId, LockerId, ReservationDate, Status)
                                VALUES (@ReservationId, @StudentId, @LockerId, @ReservationDate, 'RESERVED')";

                            using (var insertCmd = new MySqlCommand(insertReservationQuery, connection, transaction))
                            {
                                insertCmd.Parameters.AddWithValue("@ReservationId", reservationId);
                                insertCmd.Parameters.AddWithValue("@StudentId", studentId);
                                insertCmd.Parameters.AddWithValue("@LockerId", lockerId);
                                insertCmd.Parameters.AddWithValue("@ReservationDate", DateTime.Now);
                                await insertCmd.ExecuteNonQueryAsync();
                            }
                        }

                        // Update student's locker reference
                        var updateStudentQuery = @"
                    UPDATE Students
                    SET locker_id = @LockerId
                    WHERE id = @StudentId";

                        using (var studentCmd = new MySqlCommand(updateStudentQuery, connection, transaction))
                        {
                            studentCmd.Parameters.AddWithValue("@LockerId", lockerId);
                            studentCmd.Parameters.AddWithValue("@StudentId", studentId);
                            await studentCmd.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();

                        AdminService.ClearCache(_memoryCache, "AvailableWings_"); 
                        AdminService.ClearCache(_memoryCache, "AvailableLockers_"); // Invalidate available lockers cache
                        AdminService.ClearCache(_memoryCache, $"HasLocker-{studentId}");
                        AdminService.ClearCache(_memoryCache, $"CurrentReservation_{studentId}");
                        AdminService.ClearCache(_memoryCache, $"CabinetInfo_"); // Invalidate department info cache

                        return lockerId;
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }
        public bool HasLocker(int? userId)
        {

            bool hasLocker = false;
            string cached = $"HasLocker-{userId}";
            if(_memoryCache.TryGetValue(cached, out bool cachedHasLocker))
            {
                return cachedHasLocker;
            }
            try

            {
                int count;

                string query = @"SELECT COUNT(*) FROM Reservations 
                 WHERE StudentId = @userId
                AND Status = 'RESERVED'
                 AND LockerId IS NOT NULL 
                ";

                using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);

                        count = Convert.ToInt32(cmd.ExecuteScalar());
                        hasLocker = count > 0;

                    }
                }


            }
            catch (Exception ex)
            {

                //TempData["ErrorMessage"] = "An error occurred while checking locker status: " + ex.Message;
                throw ex;
            }

            _memoryCache.Set(cached, hasLocker, TimeSpan.FromMinutes(3));
            return hasLocker;
        }
        public async Task<bool> IsStudentBlocked(int studentId)
        {
            string cached = $"IsStudentBlocked-{studentId}";
            if(_memoryCache.TryGetValue(cached,out bool cachedHasStudentBlocked))
            {
                return cachedHasStudentBlocked;
            }
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                var query = @"SELECT COUNT(*) FROM BlockList WHERE student_id = @StudentId";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StudentId", studentId);
                    var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    _memoryCache.Set(cached, count > 0, TimeSpan.FromMinutes(3));
                    return count > 0;
                }
            }
        }
        public async Task<List<WingInfo>> GetAllAvailableLockerCounts(string location = null, string department = null, string wing = null, int? level = null)
        {
            var wings = new List<WingInfo>();
            string cacheKey = $"AvailableLockers_{location ?? "null"}_{department ?? "null"}_{wing ?? "null"}_{level?.ToString() ?? "null"}";
            if (_memoryCache.TryGetValue(cacheKey, out List<WingInfo> cachedWings))
            {
                return cachedWings;
            }
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                // First query: Calculate available lockers per cabinet
                var subquery = @"
            SELECT 
                c.cabinet_id,
                c.location,
                c.department_name,
                c.wing,
                c.level,
                GREATEST(c.Capacity - COALESCE(c.ReservedLockers, 0), 0) + 
                COUNT(CASE WHEN l.Status = 'EMPTY' THEN l.Id END) AS CabinetAvailableLockers
            FROM Cabinets c
            LEFT JOIN Lockers l ON c.cabinet_id = l.cabinet_id
            WHERE c.status = 'IN_SERVICE'";

                var parameters = new List<MySqlParameter>();

                // Add filters to subquery
                if (!string.IsNullOrEmpty(location))
                {
                    subquery += " AND c.location = @Location";
                    parameters.Add(new MySqlParameter("@Location", location));
                }
                if (!string.IsNullOrEmpty(department))
                {
                    subquery += " AND c.department_name = @Department";
                    parameters.Add(new MySqlParameter("@Department", department));
                }
                if (!string.IsNullOrEmpty(wing))
                {
                    subquery += " AND c.wing = @Wing";
                    parameters.Add(new MySqlParameter("@Wing", wing));
                }
                if (level.HasValue)
                {
                    subquery += " AND c.level = @Level";
                    parameters.Add(new MySqlParameter("@Level", level.Value));
                }

                subquery += " GROUP BY c.cabinet_id, c.location, c.department_name, c.wing, c.level";

                // Second query: Aggregate by location, department, wing, and level
                var query = @"
            SELECT 
                location,
                department_name AS Department,
                wing AS Wing, 
                level AS Level, 
                SUM(CabinetAvailableLockers) AS AvailableLockers
            FROM (" + subquery + @") AS subquery
            GROUP BY location, department_name, wing, level
            HAVING AvailableLockers > 0
            ORDER BY location, department_name, wing, level";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            wings.Add(new WingInfo
                            {
                                Location = reader.GetString("location"),
                                Department = reader.GetString("Department"),
                                Wing = reader.GetString("Wing"),
                                Level = reader.GetInt32("Level"),
                                AvailableLockers = reader.GetInt32("AvailableLockers")
                            });
                        }
                    }
                }
            }
            _memoryCache.Set(cacheKey, wings, TimeSpan.FromMinutes(5));
            return wings;
        }
        public async Task<FilterOptions> GetFilterOptions()
        {
            var options = new FilterOptions();
            
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                // Get all locations
                
                var locationQuery = "SELECT DISTINCT location FROM Departments";
                using (var cmd = new MySqlCommand(locationQuery, connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            options.Locations.Add(reader.GetString("location"));
                        }
                    }
                }

                // Get departments by location
                var deptQuery = "SELECT DISTINCT name, location FROM Departments";
                using (var cmd = new MySqlCommand(deptQuery, connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var dept = reader.GetString("name");
                            var loc = reader.GetString("location");
                            if (!options.DepartmentsByLocation.ContainsKey(loc))
                            {
                                options.DepartmentsByLocation[loc] = new List<string>();
                            }
                            options.DepartmentsByLocation[loc].Add(dept);
                        }
                    }
                }

                // Get wings by department and location
                var wingQuery = "SELECT DISTINCT wing, department_name, location FROM Cabinets";
                using (var cmd = new MySqlCommand(wingQuery, connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var wing = reader.GetString("wing");
                            var dept = reader.GetString("department_name");
                            var loc = reader.GetString("location");

                            var key = $"{loc}|{dept}";
                            if (!options.WingsByDeptLocation.ContainsKey(key))
                            {
                                options.WingsByDeptLocation[key] = new List<string>();
                            }
                            options.WingsByDeptLocation[key].Add(wing);
                        }
                    }
                }

                // Get levels by wing, department and location
                var levelQuery = "SELECT DISTINCT level, wing, department_name, location FROM Cabinets";
                using (var cmd = new MySqlCommand(levelQuery, connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var level = reader.GetInt32("level");
                            var wing = reader.GetString("wing");
                            var dept = reader.GetString("department_name");
                            var loc = reader.GetString("location");

                            var key = $"{loc}|{dept}|{wing}";
                            if (!options.LevelsByWingDeptLocation.ContainsKey(key))
                            {
                                options.LevelsByWingDeptLocation[key] = new List<int>();
                            }
                            options.LevelsByWingDeptLocation[key].Add(level);
                        }
                    }
                }
            }
            return options;
        }
    }
    public class FilterOptions
    {
        public List<string> Locations { get; set; } = new List<string>();
        public Dictionary<string, List<string>> DepartmentsByLocation { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> WingsByDeptLocation { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, List<int>> LevelsByWingDeptLocation { get; set; } = new Dictionary<string, List<int>>();
    }
    public class DepartmentInfo
    {
        public string DepartmentName { get; set; }
        public string Location { get; set; }
    }
    public class WingInfo
    {
        public string Location { get; set; }
        public string Wing { get; set; }
        public int Level { get; set; }
        public string Department { get; set; }
        public int AvailableLockers { get; set; }
    }
    public class ReservationRequest
    {
        public int StudentId { get; set; }
        public string DepartmentName { get; set; }
        public string Location { get; set; }
        public string Wing { get; set; }
        public int Level { get; set; }
    }
    public class LockerInfo
    {
        public string Id { get; set; }
        public string DepartmentName { get; set; }
        public LockerStatus Status { get; set; }
        public string CabinetId { get; set; }
        public string Location { get; set; }
        public string Wing { get; set; }
        public int Level { get; set; }
    }
}
