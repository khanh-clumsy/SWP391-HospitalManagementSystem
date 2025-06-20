using System;
using System.Collections.Generic;

namespace HospitalManagement.Models;

public partial class RoomTest
{
    public int RoomTestId { get; set; }

    public int TestId { get; set; }

    public int RoomId { get; set; }

    public virtual Room Room { get; set; } = null!;

    public virtual Test Test { get; set; } = null!;
}
