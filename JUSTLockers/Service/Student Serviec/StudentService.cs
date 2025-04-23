using System;
using JUSTLockers.Classes;
using JUSTLockers.Services;
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
        //leen
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

                var query = @"SELECT r.Id, r.LockerId, r.ReservationDate, r.Status, 
                            l.DepartmentName, s.name AS StudentName , s.id as StudentId
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
                                StudentId = reader.GetString("StudentId")
                            };
                        }
                    }
                }
            }

            return null; // No reservation found
        }

        public void ReportProblem()
        {
            throw new NotImplementedException();
        }

        public void DeleteReport()
        {
            throw new NotImplementedException();
        }

        public void ViewAllReports()
        {
            throw new NotImplementedException();
        }

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
        public async Task<bool> CancelReservation(int studentId, string reservationId)
        {
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Get locker ID from reservation
                        string lockerId = null;
                        var getLockerQuery = @"SELECT LockerId FROM Reservations 
                                             WHERE Id = @ReservationId 
                                             AND StudentId = @StudentId";

                        using (var getLockerCmd = new MySqlCommand(getLockerQuery, connection, transaction))
                        {
                            getLockerCmd.Parameters.AddWithValue("@ReservationId", reservationId);
                            getLockerCmd.Parameters.AddWithValue("@StudentId", studentId);

                            lockerId = (await getLockerCmd.ExecuteScalarAsync())?.ToString();

                            if (lockerId == null)
                            {
                                return false; // Reservation not found
                            }
                        }

                        // 2. Update locker status to EMPTY
                        var updateLockerQuery = @"UPDATE Lockers 
                                                SET Status = 'EMPTY' 
                                                WHERE Id = @LockerId";

                        using (var updateCmd = new MySqlCommand(updateLockerQuery, connection, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@LockerId", lockerId);
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        // 3. Update reservation status to CANCELED
                        var updateReservationQuery = @"UPDATE Reservations 
                                                      SET Status = 'CANCELED' 
                                                      WHERE Id = @ReservationId";

                        using (var reservationCmd = new MySqlCommand(updateReservationQuery, connection, transaction))
                        {
                            reservationCmd.Parameters.AddWithValue("@ReservationId", reservationId);
                            await reservationCmd.ExecuteNonQueryAsync();
                        }

                        // 4. Remove locker reference from student
                        var updateStudentQuery = @"UPDATE Students 
                                                SET locker_id = NULL 
                                                WHERE id = @StudentId";

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
    }
}
