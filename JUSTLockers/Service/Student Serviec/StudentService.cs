using System;
using System.Data;
using JUSTLockers.Classes;
using JUSTLockers.Services;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace JUSTLockers.Service
{
    public class StudentService : IStudentService
    {
        private readonly IConfiguration _configuration;

        public StudentService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void Login()
        {
            throw new NotImplementedException();
        }
        //leen may not use it 
        public async Task<List<Locker>> ViewAvailableLockers(string departmentName)
        {
            var availableLockers = new List<Locker>();

            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                var query = @"SELECT Id, DepartmentName, Status 
                              FROM Lockers 
                              WHERE DepartmentName = @DepartmentName 
                              AND Status = 'EMPTY'";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DepartmentName", departmentName);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            availableLockers.Add(new Locker
                            {
                                LockerId = reader.GetString("Id"),
                                Department = reader.GetString("DepartmentName"),
                                Status = (LockerStatus)Enum.Parse(typeof(LockerStatus), reader.GetString("Status"))
                            });
                        }
                    }
                }
            }

            return availableLockers;
        }
        //leen
        public async Task<bool> ReserveLocker(int studentId, string lockerId)
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Check if locker is available
                        var checkQuery = @"SELECT COUNT(*) FROM Lockers 
                                        WHERE Id = @LockerId 
                                        AND Status = 'EMPTY'";

                        using (var checkCmd = new MySqlCommand(checkQuery, connection, transaction))
                        {
                            checkCmd.Parameters.AddWithValue("@LockerId", lockerId);
                            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                            if (count == 0)
                            {
                                return false; // Locker not available
                            }
                        }

                        // 2. Update locker status
                        var updateLockerQuery = @"UPDATE Lockers 
                                                 SET Status = 'RESERVED' 
                                                 WHERE Id = @LockerId";

                        using (var updateCmd = new MySqlCommand(updateLockerQuery, connection, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@LockerId", lockerId);
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        // 3. Create reservation record
                        var reservationQuery = @"INSERT INTO Reservations 
                                                (Id, StudentId, LockerId, ReservationDate, Status) 
                                                VALUES 
                                                (@ReservationId, @StudentId, @LockerId, @ReservationDate, 'RESERVED')";

                        using (var reservationCmd = new MySqlCommand(reservationQuery, connection, transaction))
                        {
                            reservationCmd.Parameters.AddWithValue("@ReservationId", Guid.NewGuid().ToString());
                            reservationCmd.Parameters.AddWithValue("@StudentId", studentId);
                            reservationCmd.Parameters.AddWithValue("@LockerId", lockerId);
                            reservationCmd.Parameters.AddWithValue("@ReservationDate", DateTime.Now);

                            await reservationCmd.ExecuteNonQueryAsync();
                        }

                        // 4. Update student's locker reference
                        var updateStudentQuery = @"UPDATE Students 
                                                SET locker_id = @LockerId 
                                                WHERE id = @StudentId";

                        using (var studentCmd = new MySqlCommand(updateStudentQuery, connection, transaction))
                        {
                            studentCmd.Parameters.AddWithValue("@LockerId", lockerId);
                            studentCmd.Parameters.AddWithValue("@StudentId", studentId);

                            await studentCmd.ExecuteNonQueryAsync();
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
        //leen
        public async Task<Reservation> ViewReservationInfo(int studentId)
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT r.Id, r.LockerId, r.ReservationDate, r.Status, 
                           l.Department, s.name AS StudentName, s.id AS StudentId
                    FROM Reservations r
                    JOIN Lockers l ON r.LockerId = l.Id
                    JOIN Students s ON r.StudentId = s.id
                    WHERE r.StudentId = @StudentId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StudentId", studentId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Reservation
                            {
                                Id = reader.GetString("Id"),
                                LockerId = reader.GetString("LockerId"),
                                StudentName = reader.GetString("StudentName"),
                                Date = reader.GetDateTime("ReservationDate"),
                                Status = (ReservationStatus)Enum.Parse(typeof(ReservationStatus), reader.GetString("Status")),
                                StudentId = reader.GetInt32("StudentId")
                            };
                        }
                    }
                }
            }
            return null;
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

            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", reportId);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }
       


        public async Task<List<Report>> ViewAllReports()
        {
            var reports = new List<Report>();

            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                var query = "SELECT * FROM Reports";

                using (var command = new MySqlCommand(query, connection))
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

            return reports;
        }

        //public async Task<List<Report>> ViewReportedIssues()
        //{
        //    var reports = new List<Report>();
        //    var query = "SELECT * FROM Reports";

        //    using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        //    {
        //        await connection.OpenAsync();

        //        using (var command = new MySqlCommand(query, connection))
        //        using (var reader = await command.ExecuteReaderAsync())
        //        {
        //            while (await reader.ReadAsync())
        //            {
        //                reports.Add(new Report
        //                {
        //                    Reporter = null,
        //                    Locker = null,

        //                    ReportId = reader.GetInt32("Id"),
        //                    ReporterId = reader.GetInt32("ReporterId"),
        //                    LockerId = reader.GetString("LockerId"),

        //                    // Converting string from DB to enums
        //                    Type = Enum.Parse<ReportType>(reader.GetString("Type")),
        //                    Status = Enum.Parse<ReportStatus>(reader.GetString("Status")),

        //                    Subject = reader.GetString("Subject"),
        //                    Statement = reader.GetString("Statement"),
        //                    ReportDate = reader.GetDateTime("ReportDate"),
        //                    ResolvedDate = reader.IsDBNull("ResolvedDate") ? null : reader.GetDateTime("ResolvedDate"),
        //                    ResolutionDetails = reader.IsDBNull("ResolvedDetails") ? null : reader.GetString("ResolvedDetails"),
        //                    ImageData = reader.IsDBNull("ImageData") ? null : (byte[])reader["ImageData"],
        //                    ImageMimeType = reader.IsDBNull("ImageMimeType") ? null : reader.GetString("ImageMimeType")
        //                });
        //            }
        //        }
        //    }

        //    return reports;
        //}
        public void CheckReportStatus()
        {
            throw new NotImplementedException();
        }

        public void RemoveReservation()
        {
            throw new NotImplementedException();
        }

        public void ViewNotifications()
        {
            throw new NotImplementedException();
        }
        //leen
        public async Task<bool> CancelReservation(int studentId)
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Get the reservation to cancel
                        var reservationQuery = @"SELECT Id, LockerId FROM Reservations 
                                       WHERE StudentId = @StudentId AND Status = 'RESERVED'";

                        string reservationId = null;
                        string lockerId = null;

                        using (var cmd = new MySqlCommand(reservationQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@StudentId", studentId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    reservationId = reader.GetString("Id");
                                    lockerId = reader.GetString("LockerId");
                                }
                            }
                        }

                        if (lockerId == null) return false;

                        // Update locker status
                        var updateLockerQuery = "UPDATE Lockers SET Status = 'EMPTY' WHERE Id = @LockerId";
                        using (var lockerCmd = new MySqlCommand(updateLockerQuery, connection, transaction))
                        {
                            lockerCmd.Parameters.AddWithValue("@LockerId", lockerId);
                            await lockerCmd.ExecuteNonQueryAsync();
                        }

                        // Update reservation status
                        var updateReservationQuery = "DELETE FROM Reservations WHERE Id = @ReservationId";
                        using (var reservationCmd = new MySqlCommand(updateReservationQuery, connection, transaction))
                        {
                            reservationCmd.Parameters.AddWithValue("@ReservationId", reservationId);
                            await reservationCmd.ExecuteNonQueryAsync();
                        }

                        // Update student record
                        var updateStudentQuery = "UPDATE Students SET locker_id = NULL WHERE id = @StudentId";
                        using (var studentCmd = new MySqlCommand(updateStudentQuery, connection, transaction))
                        {
                            studentCmd.Parameters.AddWithValue("@StudentId", studentId);
                            await studentCmd.ExecuteNonQueryAsync();
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

        public async Task<List<WingInfo>> GetAvailableWingsAndLevels(string departmentName, string Location )
        {
            var wings = new List<WingInfo>();

            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                // Group by wing and level, calculate available lockers based on capacity and reserved lockers
                var query = @"
            SELECT 
                c.wing AS Wing, 
                c.level AS Level, 
                SUM(c.Capacity - COALESCE(c.ReservedLockers, 0)) AS AvailableLockers
            FROM Cabinets c
            WHERE c.department_name = @DepartmentName 
                AND c.location = @Location
                AND c.status = 'IN_SERVICE'
                AND c.Capacity > COALESCE(c.ReservedLockers, 0)
            GROUP BY c.wing, c.level
            HAVING AvailableLockers > 0
            ORDER BY c.wing, c.level";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DepartmentName", departmentName);
                    command.Parameters.AddWithValue("@Location", Location);

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
            return wings;
        }

        public async Task<Reservation> GetCurrentReservationAsync(int studentId)
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                var query = @"SELECT r.Id, r.LockerId, r.ReservationDate, r.Status, r.StudentId,
                     l.DepartmentName, s.name AS StudentName
                     FROM Reservations r
                     JOIN Lockers l ON r.LockerId = l.Id
                     JOIN Students s ON r.StudentId = s.id
                     WHERE r.StudentId = @StudentId 
                     AND r.Status = 'RESERVED'";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StudentId", studentId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Reservation
                            {
                                StudentId=reader.GetInt32("StudentId"),
                                Id = reader.GetString("Id"),
                                LockerId = reader.GetString("LockerId"),
                                StudentName = reader.GetString("StudentName"),
                                Date = reader.GetDateTime("ReservationDate"),
                                Status = (ReservationStatus)Enum.Parse(typeof(ReservationStatus), reader.GetString("Status"))
                            };
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
                        
                        // 1. Find a cabinet with available capacity in the specified wing and level
                        var findCabinetQuery = @"
                    SELECT cabinet_id, number_cab, Capacity, ReservedLockers
                    FROM Cabinets
                    WHERE department_name = @DepartmentName
                        AND location = @Location
                        AND wing = @Wing
                        AND level = @Level
                        AND status = 'IN_SERVICE'
                        AND Capacity > COALESCE(ReservedLockers, 0)
                    LIMIT 1";

                        string cabinetId = null;
                        int cabinetNumber = 0;
                        int capacity = 0;
                        int reservedLockers = 0;

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
                                    cabinetId = reader.GetString("Cabinet_id");
                                    cabinetNumber = reader.GetInt32("number_cab");
                                    capacity = reader.GetInt32("Capacity");
                                    reservedLockers = reader.GetInt32("ReservedLockers");
                                }
                            }
                        }

                        if (cabinetId == null)
                        {
                            return null; // No cabinets with available capacity
                        }

                        // 2. Try to find an existing empty locker
                        string lockerId = null;
                        var findLockerQuery = @"
                            SELECT Id
                            FROM Lockers
                            WHERE Cabinet_id = @CabinetId
                            AND Status = 'EMPTY'
                            LIMIT 1";

                        using (var findLockerCmd = new MySqlCommand(findLockerQuery, connection, transaction))
                        {
                            findLockerCmd.Parameters.AddWithValue("@CabinetId", cabinetId);
                            using (var reader = await findLockerCmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    lockerId = reader.GetString("Id");
                                }
                            }
                        }

                        // 3. If no empty locker exists, create a new one
                        if (lockerId == null)
                        {
                            if(reservedLockers<10)
                            lockerId = cabinetId + " -L"+level+"0"+ reservedLockers;
                            else lockerId = cabinetId + " -L" + level + reservedLockers;
                            var insertLockerQuery = @"
                                INSERT INTO Lockers (Id, DepartmentName, Cabinet_id, Status)
                                VALUES (@LockerId, @Department, @CabinetId, 'RESERVED')";

                            using (var insertCmd = new MySqlCommand(insertLockerQuery, connection, transaction))
                            {
                                insertCmd.Parameters.AddWithValue("@LockerId", lockerId);
                                insertCmd.Parameters.AddWithValue("@Department", departmentName);
                                insertCmd.Parameters.AddWithValue("@CabinetId", cabinetId);
                                await insertCmd.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
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

                        // 4. Update cabinet's ReservedLockers count
                        var updateCabinetQuery = @"
                            UPDATE Cabinets
                    SET ReservedLockers = COALESCE(ReservedLockers, 0) + 1
                    WHERE cabinet_id = @CabinetId";

                        using (var cabinetCmd = new MySqlCommand(updateCabinetQuery, connection, transaction))
                        {
                            cabinetCmd.Parameters.AddWithValue("@CabinetId", cabinetId);
                            await cabinetCmd.ExecuteNonQueryAsync();
                        }

                        // 5. Create reservation record
                        var reservationId = "R"+lockerId+" Student ID "+studentId;
                        var reservationQuery = @" 
                            INSERT INTO Reservations (Id, StudentId, LockerId, ReservationDate, Status)
                            VALUES (@ReservationId, @StudentId, @LockerId, @ReservationDate, 'RESERVED')";

                        using (var reservationCmd = new MySqlCommand(reservationQuery, connection, transaction))
                        {
                            reservationCmd.Parameters.AddWithValue("@ReservationId", reservationId);
                            reservationCmd.Parameters.AddWithValue("@StudentId", studentId);
                            reservationCmd.Parameters.AddWithValue("@LockerId", lockerId);
                            reservationCmd.Parameters.AddWithValue("@ReservationDate", DateTime.Now);
                            await reservationCmd.ExecuteNonQueryAsync();
                        }

                        // 6. Update student's locker reference
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
    }

        public class DepartmentInfo
    {
        public string DepartmentName { get; set; }
        public string Location { get; set; }
    }

    public class WingInfo
    {
        public string Wing { get; set; }
        public int Level { get; set; }
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
}
