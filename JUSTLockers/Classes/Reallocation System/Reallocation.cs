using System.ComponentModel.DataAnnotations;

namespace JUSTLockers.Classes;

public class Reallocation
{
    [Key]
    public int? RequestID { get; set; }

   
    public int? SupervisorID { get; set; }

    public string? CurrentDepartment { get; set; }

    public string? RequestedDepartment { get; set; }

    public RequestStatus? RequestStatus { get; set; }

    public string? RequestLocation { get; set; }
    public string? CurrentLocation { get; set; }

    public DateTime? RequestDate { get; set; }

    public string? RequestWing { get; set; }

    public int? RequestLevel { get; set; }

    public int? NumberCab { get; set; }

    public string? CurrentCabinetID { get; set; }
    public string? NewCabinetID { get; set; }

 

}

public enum RequestStatus
{
    PENDING,
    APPROVED,
    REJECTED
}
