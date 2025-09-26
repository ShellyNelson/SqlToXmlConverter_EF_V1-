using System.Xml.Serialization;

namespace SqlToXmlConverter.Models;

[XmlRoot("Employee")]
public class Employee
{
    [XmlElement("Id")]
    public int Id { get; set; }

    [XmlElement("FirstName")]
    public string FirstName { get; set; } = string.Empty;

    [XmlElement("LastName")]
    public string LastName { get; set; } = string.Empty;

    [XmlElement("Email")]
    public string Email { get; set; } = string.Empty;

    [XmlElement("Department")]
    public string Department { get; set; } = string.Empty;

    [XmlElement("HireDate")]
    public DateTime HireDate { get; set; }

    [XmlElement("Salary")]
    public decimal Salary { get; set; }
}
