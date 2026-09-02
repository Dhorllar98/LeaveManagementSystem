using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaveManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHandoverAndPublicHolidays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HandoverUserId",
                table: "LeaveRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PublicHolidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHolidays", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_HandoverUserId",
                table: "LeaveRequests",
                column: "HandoverUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Users_HandoverUserId",
                table: "LeaveRequests",
                column: "HandoverUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Users_HandoverUserId",
                table: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "PublicHolidays");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_HandoverUserId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "HandoverUserId",
                table: "LeaveRequests");
        }
    }
}
