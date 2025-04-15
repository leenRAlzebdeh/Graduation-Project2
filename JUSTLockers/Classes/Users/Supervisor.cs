namespace JUSTLockers.Classes;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Supervisor : User
{

    
    public string Location { get; set; }
   public string DepartmentName { get; set; }
    public Department SupervisedDepartment { get; set; }
    // public List<Reservation> ReservationList { get; set; }
    // public List<Report> ReportList { get; set; }
    //public List<Cabinet> Covenant { get; set; }

    public Supervisor()
       : base() // Provide default values for base class constructor
    {
        SupervisedDepartment = new Department();

    }


    public Supervisor(int id, string name, string email, Department department)
        : base(id, name, email)
    {
        SupervisedDepartment = department;
  
    }
}
