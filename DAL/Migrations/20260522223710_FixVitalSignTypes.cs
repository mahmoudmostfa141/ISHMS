using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISHMS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixVitalSignTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "OxygenLevel",
                table: "VitalSigns",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OxygenLevel",
                table: "VitalSigns",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");
        }
    }
}
