using Microsoft.EntityFrameworkCore.Migrations;

namespace EntityModel.Migrations
{
    public partial class ReportSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    MCReturnType = table.Column<int>(type: "INTEGER", nullable: false),
                    MCPeriods = table.Column<int>(type: "INTEGER", nullable: false),
                    MCRuns = table.Column<int>(type: "INTEGER", nullable: false),
                    MCClusterSize = table.Column<int>(type: "INTEGER", nullable: false),
                    MCWithReplacement = table.Column<bool>(type: "INTEGER", nullable: false),
                    BenchmarkId = table.Column<int>(type: "INTEGER", nullable: true),
                    BacktestExternalInstrumentId = table.Column<int>(type: "INTEGER", nullable: true),
                    BacktestSource = table.Column<int>(type: "INTEGER", nullable: false),
                    BacktestComparisonReturnType = table.Column<int>(type: "INTEGER", nullable: false),
                    ReturnsToBenchmark = table.Column<int>(type: "INTEGER", nullable: false),
                    VaRReturnType = table.Column<int>(type: "INTEGER", nullable: false),
                    AutoCorrReturnType = table.Column<int>(type: "INTEGER", nullable: false),
                    VaRDays = table.Column<int>(type: "INTEGER", nullable: false),
                    SelectedTags = table.Column<string>(type: "TEXT", nullable: true),
                    SelectedStrategies = table.Column<string>(type: "TEXT", nullable: true),
                    SelectedInstruments = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportSettings_Benchmarks_BenchmarkId",
                        column: x => x.BenchmarkId,
                        principalTable: "Benchmarks",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportSettings_BenchmarkId",
                table: "ReportSettings",
                column: "BenchmarkId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportSettings");
        }
    }
}