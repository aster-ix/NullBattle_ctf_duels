using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NullBattle.Ratings.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ratings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    total_played = table.Column<int>(type: "integer", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false),
                    losses = table.Column<int>(type: "integer", nullable: false),
                    draws = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ratings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ratings_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rating_id = table.Column<long>(type: "bigint", nullable: false),
                    duel_id = table.Column<long>(type: "bigint", nullable: false),
                    old_rating = table.Column<int>(type: "integer", nullable: false),
                    new_rating = table.Column<int>(type: "integer", nullable: false),
                    delta = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ratings_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_ratings_history_ratings_rating_id",
                        column: x => x.rating_id,
                        principalTable: "ratings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ratings_user_id",
                table: "ratings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ratings_history_rating_id_duel_id",
                table: "ratings_history",
                columns: new[] { "rating_id", "duel_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ratings_history");

            migrationBuilder.DropTable(
                name: "ratings");
        }
    }
}
