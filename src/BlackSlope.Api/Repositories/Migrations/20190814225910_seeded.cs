using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BlackSlope.Api.Migrations
{
    /// <summary>
    /// Manages population and cleanup of database.
    /// </summary>
    public partial class Seeded : Migration
    {
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Current formatting is more readable than alternative.")]
        [SuppressMessage("Performance", "CA1814:Prefer jagged arrays over multidimensional", Justification = "Does not waste space + Migration generated")]
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Contract.Requires(migrationBuilder != null);

            migrationBuilder.InsertData(
                table: "Movies",
                columns: new[] { "Id", "Description", "ReleaseDate", "Title" },
                values: new object[,]
                {
                    { 1, "Lorem ipsum dolor sit amet, ut consul soluta persius quo, et eam mundi scribentur, eros invidunt dissentias no eum.", new DateTime(2019, 8, 14, 17, 59, 10, 715, DateTimeKind.Local).AddTicks(5038), "The Shawshank Redemption" },
                    { 28, "Et probo ornatus sententiae cum, in unum urbanitas usu.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8907), "Se7en" },
                    { 29, "Velit omittam inciderint te cum, sit at urbanitas reformidans.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8909), "Léon: The Professional" },
                    { 30, "Causae tincidunt pro no, ius munere viderer albucius ex.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8911), "The Silence of the Lambs" },
                    { 31, "Id detraxit facilisi eleifend mea, mel ex nobis bonorum oporteat, ius oportere evertitur ut.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8913), "Star Wars: Episode IV - A New Hope" },
                    { 32, "No discere adolescens mnesarchum eam, quo latine voluptua ei.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8916), "It's a Wonderful Life" },
                    { 33, "Ei pro dolorem fierent blandit, te mea viris nihil legimus.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8918), "Once Upon a Time ... in Hollywood" },
                    { 34, "Ex duo euismod fastidii, cu euismod splendide signiferumque qui, eu eos doctus virtute.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8920), "Spider-Man: Into the Spider-Verse" },
                    { 35, "Eum cu quem integre verterem, has animal fabulas ut.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8922), "Avengers: Infinity War" },
                    { 36, "Tacimates definiebas has an.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8925), "Whiplash" },
                    { 37, "Nec ut ridens meliore pertinax.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8927), "The Intouchables" },
                    { 38, "Mei te graeci essent, dico sapientem cum ea, eum ei graeci alterum.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8929), "The Prestige" },
                    { 39, "Elit quando dictas eos ei.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8932), "The Departed" },
                    { 40, "Eros tota utinam mei ei, iisque consequuntur te his.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8934), "The Pianist" },
                    { 41, "Ne per fugit graece, quando expetendis id sea.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8936), "Memento" },
                    { 42, "Duo novum noluisse et, at vel adhuc porro minim.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8939), "Gladiator" },
                    { 43, "Dictas contentiones no his, exerci oportere ea eam.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8941), "American History X" },
                    { 44, "No sonet omnes mnesarchum quo.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8944), "The Lion King" },
                    { 45, "Postea platonem has te, ei quod dicit sit, sea et movet delicata scribentur.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8946), "Terminator 2: Judgment Day" },
                    { 46, "Malis dictas detracto et ius, no qualisque vulputate per.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8948), "Cinema Paradiso" },
                    { 47, "Platonem oportere in has, nam recusabo delicata elaboraret ei, dico gubergren hendrerit ex sea.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8950), "Grave of the Fireflies" },
                    { 48, "Cu usu velit omittam temporibus, natum vulputate sea eu.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8953), "Back to the Future" },
                    { 27, "Ex atomorum repudiandae has, euismod vocibus ei eam, ei eam esse pertinacia abhorreant.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8904), "The Usual Suspects" },
                    { 26, "Clita mollis disputando cu eam, no pri insolens abhorreant.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8902), "Life Is Beautiful " },
                    { 25, "Eu adhuc percipit eleifend eam.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8900), "The Green Mile" },
                    { 24, "Has vocent alienum te.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8898), "Saving Private Ryan" },
                    { 2, "Eos dolor perpetua ne, cum agam causae petentium ei.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8797), "The Godfather" },
                    { 3, "At idque electram moderatius vix. Legere postulant at per.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8809), "The Dark Knight" },
                    { 4, "Sanctus fuisset pericula cu usu, mea an idque dicam accumsan.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8812), "The Godfather: Part II" },
                    { 5, "Et natum tollit sed.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8814), "The Lord of the Rings: The Return of the King" },
                    { 6, "Usu ad omnium civibus persecuti.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8817), "Pulp Fiction" },
                    { 7, "In graeco omnesque oporteat vim, ad sed nonumy consulatu.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8819), "Schindler's List" },
                    { 8, "Odio euismod et vel, has ad modo graecis pertinacia.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8821), "12 Angry Men" },
                    { 9, "Vim nibh solet scaevola te, sea illud posse partem an.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8823), "Inception" },
                    { 10, "Ad mea errem legimus efficiendi.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8826), "Fight Club" },
                    { 11, "Sale dolorum fabellas te cum, cu sea purto civibus menandri.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8866), "The Lord of the Rings: The Fellowship of the Ring" },
                    { 49, "Gubergren scriptorem eu vel, eius percipitur te per, vel tale habeo et.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8955), "Raiders of the Lost Ark" },
                    { 12, "Idque viris zril vel et.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8869), "Forrest Gump" },
                    { 14, "Cum meliore admodum ei, eos id summo audire, qui facilisi deterruisset ei.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8873), "Avengers: Endgame" },
                    { 15, "Vix ei falli salutatus eloquentiam.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8876), "The Lord of the Rings: The Two Towers" },
                    { 16, "Congue verear consetetur pri at.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8878), "The Matrix" },
                    { 17, "Ius eu sapientem constituam, aeque assueverit his ea, in homero graeco saperet quo.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8880), "Goodfellas" },
                    { 18, "Et est vero animal torquatos, illum principes eum ad, ad vocent iisque fuisset qui.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8884), "Star Wars: Episode V - The Empire Strikes Back" },
                    { 19, "Has vocent fastidii appareat ne, mel eu fabellas scripserit.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8886), "One Flew Over the Cuckoo's Nest" },
                    { 20, "Et duo iudico semper fabulas.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8888), "Seven Samurai" },
                    { 21, "Eos te volumus interesset, sint cotidieque eum et.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8890), "Interstellar" },
                    { 22, "Ad vel graece principes definitiones, ad vide populo vis, ex eos sumo efficiantur necessitatibus.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8893), "City of God" },
                    { 23, "Mel an sumo iusto, clita facilis adipiscing cum cu.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8895), "Spirited Away" },
                    { 13, "Ius ut luptatum democritum ullamcorper. Dolor possit facilis sit an, sit ei rebum meliore interesset.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8871), "The Good, the Bad and the Ugly" },
                    { 50, "Alii insolens inciderint pro an, ei eos utinam commodo tacimates.", new DateTime(2019, 8, 14, 17, 59, 10, 717, DateTimeKind.Local).AddTicks(8957), "Apocalypse Now" },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            Contract.Requires(migrationBuilder != null);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 50);
        }
    }
}
