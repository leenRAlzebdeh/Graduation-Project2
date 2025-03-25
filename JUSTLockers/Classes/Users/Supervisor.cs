namespace JUSTLockers.Classes;
using System.Collections.Generic;
public class Supervisor : User
{
    public Department SupervisedDepartment { get; set; }
    public List<Reservation> ReservationList { get; set; }
    public List<Report> ReportList { get; set; }
    public List<Cabinet> Covenant { get; set; }
    public string Location { get; set; }

    public Supervisor(int id, string name, string email, Department department)
        : base(id, name, email)
    {
        SupervisedDepartment = department;
        ReservationList = new List<Reservation>();
        ReportList = new List<Report>();
        Covenant = new List<Cabinet>();
    }
}
