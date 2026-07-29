using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLeaveRequestProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "LeaveRequests",
                newName: "RequestComments");

            migrationBuilder.RenameColumn(
                name: "DurationInDays",
                table: "LeaveRequests",
                newName: "NumberOfDays");

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "LeaveRequests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Cancelled",
                table: "LeaveRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LeaveTypeId",
                table: "LeaveRequests",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId",
                principalTable: "LeaveTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "Cancelled",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "LeaveTypeId",
                table: "LeaveRequests");

            migrationBuilder.RenameColumn(
                name: "RequestComments",
                table: "LeaveRequests",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "NumberOfDays",
                table: "LeaveRequests",
                newName: "DurationInDays");
        }
    }
}
