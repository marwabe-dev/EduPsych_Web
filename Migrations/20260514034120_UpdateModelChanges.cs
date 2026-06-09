using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduPsych_Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "counselor_id1",
                table: "withdrawal_requests");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "withdrawal_requests",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "withdrawal_requests",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AddColumn<long>(
                name: "counselor_id1",
                table: "withdrawal_requests",
                type: "bigint",
                nullable: true);
        }
    }
}
