using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class PackageCategory
{
    public int PackageCategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<Package> Packages { get; set; } = new List<Package>();
}
