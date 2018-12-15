using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MCBot.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MCBot
{
    public class Staff : ModuleBase<SocketCommandContext>
    {
        [Command("staffapply")]
        public async Task ApplyForStaffAsync([Remainder] string app = default(string))
        {
            if (string.IsNullOrEmpty(app))
            {
                await ReplyAsync(":no_entry_sign:  Staff Application was not recieved");
                EmbedBuilder builder = new EmbedBuilder();
                builder.Title = Settings.Default.StaffApplicationTitle;
                builder.Description = Settings.Default.StaffApplicationDescription;
                var footer = new EmbedFooterBuilder();
                footer.Text = Settings.Default.StaffApplicationFooter;
                footer.IconUrl = Settings.Default.StaffApplicationIcon;

                builder.Footer = footer;
                var c = Settings.Default.StaffApplicationColor;
                var r = c.R;
                var g = c.G;
                var b = c.B;
                var a = c.A;
                builder.Color = new Color(r, g, b);

                var demoname = "&user";
                var demoage = 18;
                var demorole = "Example";
                var demoabout = "This is a demo";
                var demoexperiance = "I have owned 3 successful Minecraft servers";
                var demoreason = "I believe that you should grad me this role, is because i believe" +
                    " that my services to this server could be a great help to grow the community.";
                builder.AddField("Layout", 
                    $"```\n" +
                    $"Name: {demoname}\n" +
                    $"Age: {demoage}\n" +
                    $"Role: {demorole}\n" +
                    $"About: {demoabout}\n" +
                    $"Experiance: {demoexperiance}\n" +
                    $"Reason: {demoreason}\n" +
                    $"```\n" +
                    $"***Note***\n" +
                    $"if you are applying for Moderator or Admin please Add a 'M' at the end to to specify Discord Or Minecraft\n" +
                    $"for the username field you must use `&user` so that discord will find you properly."
                    );
                
                await ReplyAsync("", false, builder.Build());

            }
            else
            {
                Random r = new Random();
                var code = Guid.NewGuid().ToString();
                var codeparts = code.Split('-');

                var ticketnumber1 = r.Next(0, codeparts.Length - 1);
                var ticketnumber2 = r.Next(0, codeparts.Length - 1);

                var ticketnumber = (codeparts[ticketnumber1] + "-" + codeparts[ticketnumber2]).ToUpper();


                await ReplyAsync(":white_check_mark: Staff Application has been recieved and will be look over by one of our admis, thank you\n" +
                    $"Your ticket number: `{ticketnumber}`");
                app = app.Replace("```", string.Empty);
                app = app.Replace("&user", Context.User.Username);
                var path = $"{AppDomain.CurrentDomain.BaseDirectory}StaffApplications";
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                StreamWriter sw = new StreamWriter(path + $"\\#{ticketnumber}.tca", false);
                sw.WriteLine($"Staff Application");
                sw.WriteLine($"Date: {DateTime.Now}");
                sw.WriteLine($"Ticket Number: {ticketnumber}");
                sw.WriteLine($"Ticket Status: {AppFile.TicketStatus.Open}");
                sw.Write(app);
                sw.Flush();
                sw.Close();
                new AppFile().Process(path + $"\\#{ticketnumber}.tca").Save();

                await TCProgram.client.GetGuild(Settings.Default.TCGuild).GetTextChannel(Settings.Default.StaffChannelID).SendMessageAsync(
                    $":white_check_mark: Staff Application has been sumited\n" +
                    $"It is currently waiting for reviewed.\n" +
                    $"Do `!staffreview` to see how many applications are open" +
                    $"Do `!staffreview {ticketnumber}` to review\n" +
                    $"Do `!staffaccept {ticketnumber}` to accept\n" +
                    $"Do `!staffdeny {ticketnumber}` to deny\n" +
                    $"Do `!staffdeny {ticketnumber}` 'reason-hea' to deny and reply to the sender");

                
            }
        }
        
        [Command("staffreview"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task ReviewApplication([Remainder] string ticketnumber = default(string))
        {
            var path = $"{AppDomain.CurrentDomain.BaseDirectory}StaffApplications";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

    }

    [Serializable]
    public class AppFile
    {
        public enum TicketStatus { Open, Accepted, Rejected }
        public enum Role { None, Helper, Builder, Moderator, Admin }

        public DateTime date;
        public string ticketNumber;
        public TicketStatus ticketStatus;
        public string username;
        public int age;
        public Role role;
        public string about;
        public string experiance;
        public string reason;

        [NonSerialized] public string path;
        
        public AppFile Process(string p)
        {
            path = p;
            var lines = File.ReadLines(path).ToList();
            date = DateTime.Parse(lines[1].Split(':')[1]);
            ticketNumber = lines[2].Split(':')[1];
            ticketStatus = (TicketStatus)Enum.Parse(typeof(TicketStatus), lines[3].Split(':')[1]);
            username = lines[4].Split(':')[1];
            age = int.Parse(lines[5].Split(':')[1]);
            role = (Role)Enum.Parse(typeof(Role), lines[6].Split(':')[1]);
            about = lines[7].Split(':')[1];
            experiance = lines[8].Split(':')[1];
            reason = lines[9].Split(':')[1];
            return this;
        }

        public void Save()
        {
            XmlSerializer xml = new XmlSerializer(typeof(AppFile));
            xml.Serialize(new FileStream(path, FileMode.CreateNew), this);
        }
        public AppFile Load(string p)
        {
            XmlSerializer xml = new XmlSerializer(typeof(AppFile));
            AppFile af = (AppFile)xml.Deserialize(new FileStream(p, FileMode.Open));
            if (af == null) return null;
            date = af.date;
            ticketNumber = af.ticketNumber;
            ticketStatus = af.ticketStatus;
            username = af.username;
            age = af.age;
            role = af.role;
            about = af.about;
            experiance = af.experiance;
            reason = af.reason;
            
            return this;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();


            return sb.ToString();
        }
    }
}