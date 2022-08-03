using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace P21IntegrationWindowsService.Migrations
{
    public partial class ContactSyncInitializer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contactsync",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    p21_id = table.Column<string>(nullable: true),
                    pk_id = table.Column<string>(nullable: true),
                    firstname = table.Column<string>(nullable: true),
                    lastname = table.Column<string>(nullable: true),
                    email_address = table.Column<string>(nullable: true),
                    phone_number = table.Column<string>(nullable: true),
                    status = table.Column<int>(nullable: false),
                    synced_on = table.Column<DateTime>(nullable: true),
                    sync_trigger_by = table.Column<string>(nullable: true),
                    updated_on = table.Column<DateTime>(nullable: true),
                    update_trigger_by = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contactsync", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                columns: table => new
                {
                    icid = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    frequency = table.Column<int>(nullable: false),
                    company = table.Column<string>(nullable: true),
                    svcurl = table.Column<string>(nullable: true),
                    p21url = table.Column<string>(nullable: true),
                    nextrun = table.Column<DateTime>(nullable: true),
                    lastrun = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings", x => x.icid);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contactsync");

            migrationBuilder.DropTable(
                name: "settings");
        }
    }
}
