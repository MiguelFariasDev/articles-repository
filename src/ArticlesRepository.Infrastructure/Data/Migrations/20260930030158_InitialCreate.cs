using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArticlesRepository.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    autores = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    resumo = table.Column<string>(type: "text", nullable: false),
                    palavras_chave = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    area = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pdf_s3_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    resumo_traduzido = table.Column<string>(type: "text", nullable: true),
                    status_processamento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "pendente"),
                    criado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_articles_area",
                table: "articles",
                column: "area");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "articles");
        }
    }
}
