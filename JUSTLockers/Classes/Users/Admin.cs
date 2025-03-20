namespace JUSTLockers.Classes;
using JUSTLockers.Services;
using System.Collections.Generic;
public class Admin : User
{
    public List<Reallocation> ReallocationRequests { get; set; }
    public List<Report> ReportList { get; set; }
    public HashSet<Supervisor> Supervisors { get; set; }

    public Admin(int id, string name, string email)
        : base(id, name, email)
    {
        ReallocationRequests = new List<Reallocation>();
        ReportList = new List<Report>();
        Supervisors = new HashSet<Supervisor>();
    }


}
