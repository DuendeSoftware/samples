using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityServerHost.Migrations.ConfigurationDb
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SamlServiceProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClockSkewTicks = table.Column<long>(type: "INTEGER", nullable: true),
                    RequestMaxAgeTicks = table.Column<long>(type: "INTEGER", nullable: true),
                    AssertionConsumerServiceBinding = table.Column<int>(type: "INTEGER", nullable: false),
                    SingleLogoutServiceUrl = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SingleLogoutServiceBinding = table.Column<int>(type: "INTEGER", nullable: true),
                    RequireSignedAuthnRequests = table.Column<bool>(type: "INTEGER", nullable: false),
                    EncryptAssertions = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireConsent = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowIdpInitiated = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultNameIdFormat = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DefaultPersistentNameIdentifierClaimType = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SigningBehavior = table.Column<int>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastAccessed = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NonEditable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamlServiceProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SamlServiceProviderAssertionConsumerServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    SamlServiceProviderId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamlServiceProviderAssertionConsumerServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SamlServiceProviderAssertionConsumerServices_SamlServiceProviders_SamlServiceProviderId",
                        column: x => x.SamlServiceProviderId,
                        principalTable: "SamlServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SamlServiceProviderClaimMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClaimType = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    SamlAttributeName = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    SamlServiceProviderId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamlServiceProviderClaimMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SamlServiceProviderClaimMappings_SamlServiceProviders_SamlServiceProviderId",
                        column: x => x.SamlServiceProviderId,
                        principalTable: "SamlServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SamlServiceProviderEncryptionCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    SamlServiceProviderId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamlServiceProviderEncryptionCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SamlServiceProviderEncryptionCertificates_SamlServiceProviders_SamlServiceProviderId",
                        column: x => x.SamlServiceProviderId,
                        principalTable: "SamlServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SamlServiceProviderSigningCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    SamlServiceProviderId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamlServiceProviderSigningCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SamlServiceProviderSigningCertificates_SamlServiceProviders_SamlServiceProviderId",
                        column: x => x.SamlServiceProviderId,
                        principalTable: "SamlServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SamlServiceProviderAssertionConsumerServices_SamlServiceProviderId_Url",
                table: "SamlServiceProviderAssertionConsumerServices",
                columns: new[] { "SamlServiceProviderId", "Url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SamlServiceProviderClaimMappings_SamlServiceProviderId_ClaimType",
                table: "SamlServiceProviderClaimMappings",
                columns: new[] { "SamlServiceProviderId", "ClaimType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SamlServiceProviderEncryptionCertificates_SamlServiceProviderId",
                table: "SamlServiceProviderEncryptionCertificates",
                column: "SamlServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_SamlServiceProviders_EntityId",
                table: "SamlServiceProviders",
                column: "EntityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SamlServiceProviderSigningCertificates_SamlServiceProviderId",
                table: "SamlServiceProviderSigningCertificates",
                column: "SamlServiceProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SamlServiceProviderAssertionConsumerServices");

            migrationBuilder.DropTable(
                name: "SamlServiceProviderClaimMappings");

            migrationBuilder.DropTable(
                name: "SamlServiceProviderEncryptionCertificates");

            migrationBuilder.DropTable(
                name: "SamlServiceProviderSigningCertificates");

            migrationBuilder.DropTable(
                name: "SamlServiceProviders");
        }
    }
}
