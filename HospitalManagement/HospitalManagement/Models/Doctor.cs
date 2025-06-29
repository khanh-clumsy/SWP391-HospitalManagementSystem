using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HospitalManagement.Models;

public partial class Doctor
{
    public int DoctorId { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string DepartmentName { get; set; } = null!;
    public bool IsDepartmentHead { get; set; }
    public int ExperienceYear { get; set; }
    public string? Degree { get; set; }
    public bool IsActive { get; set; }
    public bool IsSpecial { get; set; }
    public string? Gender { get; set; }
    public string? ProfileImage { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<News> News { get; set; } = new List<News>();
    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public string GetFullGender()
    {
        return this.Gender switch
        {
            "M" => "Male",
            "F" => "Female",
            _ => "Other"
        };
    }

    public string GenerateDoctorCode()
    {
        string ans = "";
        string s = this.FullName;
        var part = s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for(int i = 0;i<part.Length;i++)
        {
            string x = part[i];
            if (i < part.Length - 1) ans += char.ToUpper(x[0]);
            else ans = x + ans;
        }
        ans += this.DoctorId.ToString();
        return ans;
    }
    // public string GenerateDoctorCode()
    // {
    //     var parts = this.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    //     if (parts.Length == 0) return "Unknown" + this.DoctorId;

    //     var last = parts[^1];
    //     var initials = string.Join("", parts.Take(parts.Length - 1).Select(p => p[0]));

    //     return $"{RemoveDiacritics(last)}{initials.ToUpper()}{this.DoctorId}";
    // }

    // private string RemoveDiacritics(string text)
    // {
    //     if (string.IsNullOrWhiteSpace(text)) return text;

    //     var normalized = text.Normalize(NormalizationForm.FormD);
    //     var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
    //     return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
    // }
}
