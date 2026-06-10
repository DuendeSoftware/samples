using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityServerHost.Migrations.PersistedGrantDb
{
    /// <inheritdoc />
    public partial class UpdateV8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SamlLogoutSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LogoutId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SerializedSession = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamlLogoutSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SamlSigninStates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SerializedState = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ServiceProviderEntityId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamlSigninStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SamlLogoutSessionRequestIndices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SamlLogoutSessionId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamlLogoutSessionRequestIndices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SamlLogoutSessionRequestIndices_SamlLogoutSessions_SamlLogoutSessionId",
                        column: x => x.SamlLogoutSessionId,
                        principalTable: "SamlLogoutSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SamlLogoutSessionRequestIndices_RequestId",
                table: "SamlLogoutSessionRequestIndices",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SamlLogoutSessionRequestIndices_SamlLogoutSessionId",
                table: "SamlLogoutSessionRequestIndices",
                column: "SamlLogoutSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SamlLogoutSessions_ExpiresAtUtc",
                table: "SamlLogoutSessions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SamlLogoutSessions_LogoutId",
                table: "SamlLogoutSessions",
                column: "LogoutId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SamlSigninStates_ExpiresAtUtc",
                table: "SamlSigninStates",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SamlSigninStates_StateId",
                table: "SamlSigninStates",
                column: "StateId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SamlLogoutSessionRequestIndices");

            migrationBuilder.DropTable(
                name: "SamlSigninStates");

            migrationBuilder.DropTable(
                name: "SamlLogoutSessions");
        }
    }
}
