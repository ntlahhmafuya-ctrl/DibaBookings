using DIBA_Backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DIBA_Backend.Migrations
{
    [DbContext(typeof(DIBABookingsDbContext))]
    [Migration("20261011090000_AddReadinessHardening")]
    partial class AddReadinessHardening
    {
        // The complete target model is maintained in DIBABookingsDbContextModelSnapshot.
        // The migration's Up/Down operations are defined in the companion file.
    }
}
