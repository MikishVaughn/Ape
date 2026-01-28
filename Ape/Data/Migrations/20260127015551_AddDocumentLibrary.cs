using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ape.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PDFCategories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ParentCategoryID = table.Column<int>(type: "int", nullable: true),
                    AccessLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PDFCategories", x => x.CategoryID);
                    table.ForeignKey(
                        name: "FK_PDFCategories_PDFCategories_ParentCategoryID",
                        column: x => x.ParentCategoryID,
                        principalTable: "PDFCategories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CategoryFiles",
                columns: table => new
                {
                    FileID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryFiles", x => x.FileID);
                    table.ForeignKey(
                        name: "FK_CategoryFiles_PDFCategories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "PDFCategories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryFiles_CategoryID",
                table: "CategoryFiles",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_PDFCategories_ParentCategoryID",
                table: "PDFCategories",
                column: "ParentCategoryID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryFiles");

            migrationBuilder.DropTable(
                name: "PDFCategories");
        }
    }
}
