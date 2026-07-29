using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveAllocationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveAllocations_LeaveTypes_LeaveTypeId1",
                table: "LeaveAllocations");

            migrationBuilder.DropIndex(
                name: "IX_LeaveAllocations_LeaveTypeId1",
                table: "LeaveAllocations");

            migrationBuilder.DropColumn(
                name: "LeaveTypeId1",
                table: "LeaveAllocations");

            migrationBuilder.AlterColumn<Guid>(
                name: "LeaveTypeId",
                table: "LeaveAllocations",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "LeaveAllocations",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LeaveAllocations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAllocations_EmployeeId",
                table: "LeaveAllocations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAllocations_LeaveTypeId",
                table: "LeaveAllocations",
                column: "LeaveTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveAllocations_LeaveTypes_LeaveTypeId",
                table: "LeaveAllocations",
                column: "LeaveTypeId",
                principalTable: "LeaveTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveAllocations_Users_EmployeeId",
                table: "LeaveAllocations",
                column: "EmployeeId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveAllocations_LeaveTypes_LeaveTypeId",
                table: "LeaveAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveAllocations_Users_EmployeeId",
                table: "LeaveAllocations");

            migrationBuilder.DropIndex(
                name: "IX_LeaveAllocations_EmployeeId",
                table: "LeaveAllocations");

            migrationBuilder.DropIndex(
                name: "IX_LeaveAllocations_LeaveTypeId",
                table: "LeaveAllocations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LeaveAllocations");

            migrationBuilder.AlterColumn<int>(
                name: "LeaveTypeId",
                table: "LeaveAllocations",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "LeaveAllocations",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<Guid>(
                name: "LeaveTypeId1",
                table: "LeaveAllocations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAllocations_LeaveTypeId1",
                table: "LeaveAllocations",
                column: "LeaveTypeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveAllocations_LeaveTypes_LeaveTypeId1",
                table: "LeaveAllocations",
                column: "LeaveTypeId1",
                principalTable: "LeaveTypes",
                principalColumn: "Id");
        }
    }
}
