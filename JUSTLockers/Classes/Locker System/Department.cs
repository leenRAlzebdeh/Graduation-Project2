using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace JUSTLockers.Classes;

[Table("Departments")]
public class Department
{



    [Key]
    [Required]
    public string Name { get; set; }

    [Required]
    public int Total_Wings { get; set; }

    [Required]
    public string Location { get; set; }

}