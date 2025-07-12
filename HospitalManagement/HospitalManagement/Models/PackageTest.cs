using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class PackageTest
{
    public int PackageTestId { get; set; }

    public int PackageId { get; set; }

    public int TestId { get; set; }

    public virtual Package Package { get; set; } = null!;

    public virtual Test Test { get; set; } = null!;
}
