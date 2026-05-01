using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISHMS.DAL.Migrations
{
    /// <inheritdoc />
    public partial class departmentandBed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            // امسح البيانات القديمة اللي مش عندها Departments
            migrationBuilder.Sql("DELETE FROM Rooms;");
            migrationBuilder.Sql("DELETE FROM Beds;");

            migrationBuilder.DropForeignKey(
                name: "FK_Beds_Patients_PatientId",
                table: "Beds");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Wards_WardId",
                table: "Rooms");

            migrationBuilder.DropTable(
                name: "WaitingPatients");

            migrationBuilder.DropTable(
                name: "Wards");

            migrationBuilder.DropIndex(
                name: "IX_Beds_PatientId",
                table: "Beds");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Patients");

            migrationBuilder.RenameColumn(
                name: "WardId",
                table: "Rooms",
                newName: "DepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Rooms_WardId",
                table: "Rooms",
                newName: "IX_Rooms_DepartmentId");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Background",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BedId",
                table: "Patients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentTreatment",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousMedications",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_BedId",
                table: "Patients",
                column: "BedId",
                unique: true,
                filter: "[BedId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Beds_BedId",
                table: "Patients",
                column: "BedId",
                principalTable: "Beds",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Departments_DepartmentId",
                table: "Rooms",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Beds_BedId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Departments_DepartmentId",
                table: "Rooms");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Patients_BedId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "Background",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "BedId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "CurrentTreatment",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "PreviousMedications",
                table: "Patients");

            migrationBuilder.RenameColumn(
                name: "DepartmentId",
                table: "Rooms",
                newName: "WardId");

            migrationBuilder.RenameIndex(
                name: "IX_Rooms_DepartmentId",
                table: "Rooms",
                newName: "IX_Rooms_WardId");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Patients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Patients",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "WaitingPatients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitingPatients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitingPatients_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beds_PatientId",
                table: "Beds",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitingPatients_PatientId",
                table: "WaitingPatients",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Beds_Patients_PatientId",
                table: "Beds",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Wards_WardId",
                table: "Rooms",
                column: "WardId",
                principalTable: "Wards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
