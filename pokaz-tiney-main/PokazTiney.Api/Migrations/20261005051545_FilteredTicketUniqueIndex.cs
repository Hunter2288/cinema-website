using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokazTiney.Api.Migrations
{
    /// <inheritdoc />
    public partial class FilteredTicketUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_SessionId_SeatNumber",
                table: "Tickets");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Sessions",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SessionId_SeatNumber",
                table: "Tickets",
                columns: new[] { "SessionId", "SeatNumber" },
                unique: true,
                filter: "[Status] <> 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_SessionId_SeatNumber",
                table: "Tickets");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Sessions",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SessionId_SeatNumber",
                table: "Tickets",
                columns: new[] { "SessionId", "SeatNumber" },
                unique: true);
        }
    }
}
