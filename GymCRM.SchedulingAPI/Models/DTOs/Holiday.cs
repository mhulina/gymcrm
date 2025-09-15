namespace GymCRM.SchedulingAPI.Models.DTOs;

public class Holiday
{
    public Guid Id { get; set; }
    public string EnglishName { get; set; }
    public string LocalName { get; set; }
    public string CountryCode { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; }
    public string RegionCode { get; set; }
    public int Year { get; set; }
}