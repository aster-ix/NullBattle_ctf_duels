using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NullBattle.Duels.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "duels",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    difficulty = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    task_id = table.Column<long>(type: "bigint", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    winner_user_id = table.Column<long>(type: "bigint", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_duels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "duels_categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    duel_id = table.Column<long>(type: "bigint", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    category_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    banned_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_duels_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_duels_categories_duels_duel_id",
                        column: x => x.duel_id,
                        principalTable: "duels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "duels_users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    duel_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    result = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_duels_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_duels_users_duels_duel_id",
                        column: x => x.duel_id,
                        principalTable: "duels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "submissions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    duel_user_id = table.Column<long>(type: "bigint", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_submissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_submissions_duel_users_duel_user_id",
                        column: x => x.duel_user_id,
                        principalTable: "duels_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_duels_categories_duel_id_category_id",
                table: "duels_categories",
                columns: new[] { "duel_id", "category_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_duels_users_duel_id_user_id",
                table: "duels_users",
                columns: new[] { "duel_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_duels_users_user_id",
                table: "duels_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_submissions_duel_user_id",
                table: "submissions",
                column: "duel_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "duels_categories");

            migrationBuilder.DropTable(
                name: "submissions");

            migrationBuilder.DropTable(
                name: "duels_users");

            migrationBuilder.DropTable(
                name: "duels");
        }
    }
}
