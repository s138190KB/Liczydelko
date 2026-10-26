using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Liczydelko.Migrations
{
    /// <inheritdoc />
    public partial class Mig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DefinicjeLeveli",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numer = table.Column<int>(type: "INTEGER", nullable: false),
                    Nazwa = table.Column<string>(type: "TEXT", nullable: false),
                    Opis = table.Column<string>(type: "TEXT", nullable: false),
                    WymaganyEtap = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefinicjeLeveli", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Odznaki",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kod = table.Column<string>(type: "TEXT", nullable: false),
                    Nazwa = table.Column<string>(type: "TEXT", nullable: false),
                    Opis = table.Column<string>(type: "TEXT", nullable: false),
                    Ikona = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Odznaki", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoziomyEdukacyjne",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kod = table.Column<string>(type: "TEXT", nullable: true),
                    Nazwa = table.Column<string>(type: "TEXT", nullable: false),
                    TypSzkoly = table.Column<string>(type: "TEXT", nullable: false),
                    Zakres = table.Column<string>(type: "TEXT", nullable: false),
                    Kolejnosc = table.Column<int>(type: "INTEGER", nullable: false),
                    Opis = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoziomyEdukacyjne", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Uzytkownicy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Haslo = table.Column<string>(type: "TEXT", nullable: false),
                    Nick = table.Column<string>(type: "TEXT", nullable: false),
                    Punkty = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    SeriaDni = table.Column<int>(type: "INTEGER", nullable: false),
                    DataRejestracji = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Rola = table.Column<string>(type: "TEXT", nullable: false),
                    NieudaneProby = table.Column<int>(type: "INTEGER", nullable: false),
                    BlokadaDo = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uzytkownicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dzialy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kod = table.Column<string>(type: "TEXT", nullable: true),
                    Nazwa = table.Column<string>(type: "TEXT", nullable: false),
                    Opis = table.Column<string>(type: "TEXT", nullable: false),
                    Kategoria = table.Column<string>(type: "TEXT", nullable: false),
                    PoziomEdukacyjnyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dzialy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dzialy_PoziomyEdukacyjne_PoziomEdukacyjnyId",
                        column: x => x.PoziomEdukacyjnyId,
                        principalTable: "PoziomyEdukacyjne",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Awatary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UzytkownikId = table.Column<int>(type: "INTEGER", nullable: false),
                    Dane = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Typ = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Awatary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Awatary_Uzytkownicy_UzytkownikId",
                        column: x => x.UzytkownikId,
                        principalTable: "Uzytkownicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OdznakiUzytkownikow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UzytkownikId = table.Column<int>(type: "INTEGER", nullable: false),
                    OdznakaId = table.Column<int>(type: "INTEGER", nullable: false),
                    DataZdobycia = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdznakiUzytkownikow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OdznakiUzytkownikow_Odznaki_OdznakaId",
                        column: x => x.OdznakaId,
                        principalTable: "Odznaki",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OdznakiUzytkownikow_Uzytkownicy_UzytkownikId",
                        column: x => x.UzytkownikId,
                        principalTable: "Uzytkownicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lekcje",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kod = table.Column<string>(type: "TEXT", nullable: true),
                    Tytul = table.Column<string>(type: "TEXT", nullable: false),
                    Wprowadzenie = table.Column<string>(type: "TEXT", nullable: false),
                    Teoria = table.Column<string>(type: "TEXT", nullable: false),
                    Wzory = table.Column<string>(type: "TEXT", nullable: false),
                    Wskazowki = table.Column<string>(type: "TEXT", nullable: false),
                    DzialId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kolejnosc = table.Column<int>(type: "INTEGER", nullable: false),
                    WymaganyWynik = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lekcje", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lekcje_Dzialy_DzialId",
                        column: x => x.DzialId,
                        principalTable: "Dzialy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Przyklady",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kod = table.Column<string>(type: "TEXT", nullable: true),
                    Tresc = table.Column<string>(type: "TEXT", nullable: false),
                    Rozwiazanie = table.Column<string>(type: "TEXT", nullable: false),
                    LekcjaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Przyklady", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Przyklady_Lekcje_LekcjaId",
                        column: x => x.LekcjaId,
                        principalTable: "Lekcje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PytaniaQuizu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kod = table.Column<string>(type: "TEXT", nullable: true),
                    Tresc = table.Column<string>(type: "TEXT", nullable: false),
                    OdpA = table.Column<string>(type: "TEXT", nullable: false),
                    OdpB = table.Column<string>(type: "TEXT", nullable: false),
                    OdpC = table.Column<string>(type: "TEXT", nullable: false),
                    OdpD = table.Column<string>(type: "TEXT", nullable: false),
                    Poprawna = table.Column<string>(type: "TEXT", nullable: false),
                    Trudnosc = table.Column<int>(type: "INTEGER", nullable: false),
                    LekcjaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PytaniaQuizu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PytaniaQuizu_Lekcje_LekcjaId",
                        column: x => x.LekcjaId,
                        principalTable: "Lekcje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wyniki",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UzytkownikId = table.Column<int>(type: "INTEGER", nullable: false),
                    LekcjaId = table.Column<int>(type: "INTEGER", nullable: false),
                    LiczbaPunktow = table.Column<int>(type: "INTEGER", nullable: false),
                    Razem = table.Column<int>(type: "INTEGER", nullable: false),
                    Trudnosc = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wyniki", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wyniki_Lekcje_LekcjaId",
                        column: x => x.LekcjaId,
                        principalTable: "Lekcje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Wyniki_Uzytkownicy_UzytkownikId",
                        column: x => x.UzytkownikId,
                        principalTable: "Uzytkownicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Zadania",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kod = table.Column<string>(type: "TEXT", nullable: true),
                    LekcjaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rodzaj = table.Column<string>(type: "TEXT", nullable: false),
                    Tresc = table.Column<string>(type: "TEXT", nullable: false),
                    Odpowiedz = table.Column<string>(type: "TEXT", nullable: false),
                    BledneRozwiazanie = table.Column<string>(type: "TEXT", nullable: true),
                    Wyjasnienie = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zadania", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Zadania_Lekcje_LekcjaId",
                        column: x => x.LekcjaId,
                        principalTable: "Lekcje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Odpowiedzi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UzytkownikId = table.Column<int>(type: "INTEGER", nullable: false),
                    PytanieQuizuId = table.Column<int>(type: "INTEGER", nullable: false),
                    WybranaOdp = table.Column<string>(type: "TEXT", nullable: false),
                    CzyPoprawna = table.Column<bool>(type: "INTEGER", nullable: false),
                    CzasSekundy = table.Column<int>(type: "INTEGER", nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Odpowiedzi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Odpowiedzi_PytaniaQuizu_PytanieQuizuId",
                        column: x => x.PytanieQuizuId,
                        principalTable: "PytaniaQuizu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Odpowiedzi_Uzytkownicy_UzytkownikId",
                        column: x => x.UzytkownikId,
                        principalTable: "Uzytkownicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Powtorki",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UzytkownikId = table.Column<int>(type: "INTEGER", nullable: false),
                    PytanieQuizuId = table.Column<int>(type: "INTEGER", nullable: false),
                    Pudelko = table.Column<int>(type: "INTEGER", nullable: false),
                    NastepnaPowtorka = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Powtorki", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Powtorki_PytaniaQuizu_PytanieQuizuId",
                        column: x => x.PytanieQuizuId,
                        principalTable: "PytaniaQuizu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Powtorki_Uzytkownicy_UzytkownikId",
                        column: x => x.UzytkownikId,
                        principalTable: "Uzytkownicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WynikiCwiczen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UzytkownikId = table.Column<int>(type: "INTEGER", nullable: false),
                    ZadanieId = table.Column<int>(type: "INTEGER", nullable: false),
                    CzyPoprawna = table.Column<bool>(type: "INTEGER", nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WynikiCwiczen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WynikiCwiczen_Uzytkownicy_UzytkownikId",
                        column: x => x.UzytkownikId,
                        principalTable: "Uzytkownicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WynikiCwiczen_Zadania_ZadanieId",
                        column: x => x.ZadanieId,
                        principalTable: "Zadania",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Awatary_UzytkownikId",
                table: "Awatary",
                column: "UzytkownikId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dzialy_Kod",
                table: "Dzialy",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dzialy_PoziomEdukacyjnyId",
                table: "Dzialy",
                column: "PoziomEdukacyjnyId");

            migrationBuilder.CreateIndex(
                name: "IX_Lekcje_DzialId",
                table: "Lekcje",
                column: "DzialId");

            migrationBuilder.CreateIndex(
                name: "IX_Lekcje_Kod",
                table: "Lekcje",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Odpowiedzi_PytanieQuizuId",
                table: "Odpowiedzi",
                column: "PytanieQuizuId");

            migrationBuilder.CreateIndex(
                name: "IX_Odpowiedzi_UzytkownikId",
                table: "Odpowiedzi",
                column: "UzytkownikId");

            migrationBuilder.CreateIndex(
                name: "IX_Odznaki_Kod",
                table: "Odznaki",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OdznakiUzytkownikow_OdznakaId",
                table: "OdznakiUzytkownikow",
                column: "OdznakaId");

            migrationBuilder.CreateIndex(
                name: "IX_OdznakiUzytkownikow_UzytkownikId_OdznakaId",
                table: "OdznakiUzytkownikow",
                columns: new[] { "UzytkownikId", "OdznakaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Powtorki_PytanieQuizuId",
                table: "Powtorki",
                column: "PytanieQuizuId");

            migrationBuilder.CreateIndex(
                name: "IX_Powtorki_UzytkownikId_PytanieQuizuId",
                table: "Powtorki",
                columns: new[] { "UzytkownikId", "PytanieQuizuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoziomyEdukacyjne_Kod",
                table: "PoziomyEdukacyjne",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Przyklady_Kod",
                table: "Przyklady",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Przyklady_LekcjaId",
                table: "Przyklady",
                column: "LekcjaId");

            migrationBuilder.CreateIndex(
                name: "IX_PytaniaQuizu_Kod",
                table: "PytaniaQuizu",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PytaniaQuizu_LekcjaId",
                table: "PytaniaQuizu",
                column: "LekcjaId");

            migrationBuilder.CreateIndex(
                name: "IX_Uzytkownicy_Email",
                table: "Uzytkownicy",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wyniki_LekcjaId",
                table: "Wyniki",
                column: "LekcjaId");

            migrationBuilder.CreateIndex(
                name: "IX_Wyniki_UzytkownikId",
                table: "Wyniki",
                column: "UzytkownikId");

            migrationBuilder.CreateIndex(
                name: "IX_WynikiCwiczen_UzytkownikId",
                table: "WynikiCwiczen",
                column: "UzytkownikId");

            migrationBuilder.CreateIndex(
                name: "IX_WynikiCwiczen_ZadanieId",
                table: "WynikiCwiczen",
                column: "ZadanieId");

            migrationBuilder.CreateIndex(
                name: "IX_Zadania_Kod",
                table: "Zadania",
                column: "Kod",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Zadania_LekcjaId",
                table: "Zadania",
                column: "LekcjaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Awatary");

            migrationBuilder.DropTable(
                name: "DefinicjeLeveli");

            migrationBuilder.DropTable(
                name: "Odpowiedzi");

            migrationBuilder.DropTable(
                name: "OdznakiUzytkownikow");

            migrationBuilder.DropTable(
                name: "Powtorki");

            migrationBuilder.DropTable(
                name: "Przyklady");

            migrationBuilder.DropTable(
                name: "Wyniki");

            migrationBuilder.DropTable(
                name: "WynikiCwiczen");

            migrationBuilder.DropTable(
                name: "Odznaki");

            migrationBuilder.DropTable(
                name: "PytaniaQuizu");

            migrationBuilder.DropTable(
                name: "Uzytkownicy");

            migrationBuilder.DropTable(
                name: "Zadania");

            migrationBuilder.DropTable(
                name: "Lekcje");

            migrationBuilder.DropTable(
                name: "Dzialy");

            migrationBuilder.DropTable(
                name: "PoziomyEdukacyjne");
        }
    }
}
