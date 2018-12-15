using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MCBot.Properties;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MCBot
{
    public class Staff : ModuleBase<SocketCommandContext>
    {
        public static readonly string  path = $"{AppDomain.CurrentDomain.BaseDirectory}StaffApplications\\";
        [Command("staffapp")]
        public async Task ApplyForStaffAsync([Remainder] string app = default(string))
        {
            Directory.CreateDirectory(path);
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
                var demoage = 100;
                var demorole = AppFile.Role.None;
                var demoabout = "Example";
                var demoexperiance = "Example";
                var demoreason = "Example";
                builder.AddField("Layout Exampel", 
                    $"```\n" +
                    $"!staffapp\n" +
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


                await Context.User.SendMessageAsync($":white_check_mark: Staff Application has been recieved and will be look over by one of our admis, thank you\n" +
                    $"Your ticket number: `{ticketnumber}`");
                app = app.Replace("```", string.Empty);
                app = app.Replace("&user", $"{Context.User.Username }#{Context.User.Discriminator}");
                
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                StreamWriter sw = new StreamWriter(path + $"#{ticketnumber}.tcapp", false);
                sw.WriteLine($"Staff Application");
                sw.WriteLine($"Date: {DateTime.Now.ToShortDateString()}");
                sw.WriteLine($"Time: {DateTime.Now.ToShortTimeString()}");
                sw.WriteLine($"Ticket Number: {ticketnumber}");
                sw.WriteLine($"Ticket Status: {AppFile.TicketStatus.Open}");
                sw.Write(app);
                sw.Flush();
                sw.Close();
                var file = new AppFile().Process(ticketnumber);
                file.id = Context.User.Id;
                file.Save(ticketnumber);

                await TCProgram.client.GetGuild(Settings.Default.TCGuild).GetTextChannel(Settings.Default.StaffChannelID).SendMessageAsync(
                    $":white_check_mark: { Context.User.Mention }'s Staff Application has been sumited\n" +
                    $"It is currently waiting for reviewed.\n" +
                    $"Do `!staffappreview` to see how many applications are open" +
                    $"Do `!staffappreview {ticketnumber}` to review\n" +
                    $"Do `!staffappaccept {ticketnumber}` to accept\n" +
                    $"Do `!staffappdeny {ticketnumber}` to deny\n" +
                    $"Do `!staffappdeny {ticketnumber}` 'reason-hea' to deny and reply to the sender");
            }
        }
        [Command("staffappreview"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task ReviewApplication([Remainder] string ticketnumber = default(string))
        {

            Directory.CreateDirectory(path);
            if (!string.IsNullOrEmpty(ticketnumber))
            {
                await ReplyAsync("",  false, new AppFile().Load(ticketnumber).ToString());
            }
            else
            {
                var files = Directory.GetFiles(path);
                if (files.Length <= 3)
                {
                    foreach (var file in files)
                    {
                        await ReplyAsync($"Index {files.ToList().IndexOf(file)}", false, new AppFile().Load(new FileInfo(file).Name.Replace("#", string.Empty).Replace(new FileInfo(file).Extension, string.Empty)).ToString());
                    }
                }
                else
                {
                    await ReplyAsync("There is way to many result they will be DM to you");
                    foreach (var file in files)
                    {
                        await Context.User.SendMessageAsync($"Index {files.ToList().IndexOf(file)}", false, new AppFile().Load(new FileInfo(file).Name.Replace("#", string.Empty).Replace(new FileInfo(file).Extension, string.Empty)).ToString());
                    }
                }
            }
        }
        [Command("staffappaccept"), RequireBotPermission(GuildPermission.Administrator), RequireUserPermission(GuildPermission.Administrator)]
        public async Task AcceptApplicationAsync(string ticketnumber)
        {
            var file = new AppFile().Load(ticketnumber);
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == file.role.ToString());
            var guilduser = TCProgram.client.GetGuild(Settings.Default.TCGuild).GetUser(file.id);
            if(!guilduser.Roles.Contains(role))
            {
                await guilduser.AddRoleAsync(role);
                EmbedBuilder embed = new EmbedBuilder();
                embed.Color = Color.Green;
                embed.Title = "***CONGRADULATIONS***";
                embed.Description = "You have been approved for the for a role see details below.";
                embed.AddField("MessageFrom the Staff",
                $"Congratulation { file.GetUser.Mention } you have given the { file.role } role and this " +
                $"will take effect immodestly if you have any question you can ask them in the { TCProgram.client.GetChannel(Settings.Default.StaffChannelID) }.");
            
                await file.GetUser.SendMessageAsync("", false, embed.Build());
                file.ticketStatus = AppFile.TicketStatus.Accepted;
            }
            file.Save(file.ticketNumber);
        }
        [Command("staffappdeny"), RequireBotPermission(GuildPermission.Administrator), RequireUserPermission(GuildPermission.Administrator)]
        public async Task DenyApplicationAsync(string ticketnumber, [Remainder]string reason = default(string))
        {
            var file = new AppFile().Load(ticketnumber);
            file.ticketStatus = AppFile.TicketStatus.Rejected;
            EmbedBuilder embed = new EmbedBuilder();
            embed.Color = Color.DarkRed;
            embed.Title = "***SORRY***";
            embed.Description = "You have been not been approved for the for a role see details below.";
            if(!string.IsNullOrEmpty(reason)) embed.AddField("MessageFrom the Staff", reason);
            await file.GetUser.SendMessageAsync("", false, embed.Build());
            file.Save(ticketnumber);
        }
        [Command("staffappclear"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task ClearApplicationsAsync()
        {
            int numfiles = 0;
            Directory.CreateDirectory(path);
            if (Directory.Exists(path))
            {
                numfiles = Directory.GetFiles(path).Count();
                Directory.Delete(path, true);
                Directory.CreateDirectory(path);
                await ReplyAsync($"{numfiles} Applications Cleared");
            }
        }
    }

    [Serializable]
    public class AppFile
    {
        public enum TicketStatus { Open, Accepted, Rejected }
        public enum Role { None, Helper, Builder, Moderator, Admin }

        public string date;
        public string time;
        public string ticketNumber;
        public TicketStatus ticketStatus;
        public ulong id;
        public string username;
        public int age;
        public Role role;
        public string about;
        public string experiance;
        public string reason;
        
        public SocketUser GetUser
        {
            get
            {
                var user = TCProgram.client.GetGuild(Settings.Default.TCGuild).GetUser(id);
                return user ?? null;
            }
        }

        public AppFile Process(string ticketnumber)
        {
            var lines = File.ReadLines(Staff.path + $"#{ticketnumber}.tcapp").ToList();
            date = lines[1].Split(new string[] {": "}, StringSplitOptions.None)[1];
            time = lines[2].Split(new string[] { ": " }, StringSplitOptions.None)[1];
            ticketNumber = lines[3].Split(new string[] { ": " }, StringSplitOptions.None)[1];
            ticketStatus = (TicketStatus)Enum.Parse(typeof(TicketStatus), lines[4].Split(new string[] { ": " }, StringSplitOptions.None)[1]);
            username = lines[5].Split(new string[] { ": " }, StringSplitOptions.None)[1];
            age = int.Parse(lines[6].Split(new string[] { ": " }, StringSplitOptions.None)[1]);
            role = (Role)Enum.Parse(typeof(Role), lines[7].Split(new string[] { ": " }, StringSplitOptions.None)[1]);
            about = lines[8].Split(new string[] { ": " }, StringSplitOptions.None)[1];
            experiance = lines[9].Split(new string[] { ": " }, StringSplitOptions.None)[1];
            reason = lines[10].Split(new string[] { ": " }, StringSplitOptions.None)[1];
            return this;
        }

        public void Save(string ticketnumber)
        {
            foreach (var file in Directory.GetFiles(Staff.path))
            {
                AppFile af = new AppFile().Load(new FileInfo(file).Name.Replace("#", string.Empty).Replace(new FileInfo(file).Extension, string.Empty));
                if (af.username == username && af.ticketStatus == TicketStatus.Open)
                {
                    GetUser.SendMessageAsync("You already have an application in processing at the moment please wat for it to finish then try again.");
                    if (File.Exists(Staff.path + $"#{ticketnumber}.tcapp")) File.Delete(Staff.path + $"#{ticketnumber}.tcapp");
                    return;
                }
            }

            XmlSerializer xml = new XmlSerializer(typeof(AppFile));
            if (File.Exists(Staff.path + $"#{ticketnumber}.tcapp")) File.Delete(Staff.path + $"#{ticketnumber}.tcapp");
            var fs = new FileStream(Staff.path + $"#{ticketnumber}.tcapp", FileMode.Create);
            xml.Serialize(fs, this);
            fs.Close();
        }

        public AppFile Load(string ticketnumber)
        {
            foreach(var file in Directory.GetFiles(Staff.path))
            {
                if(new FileInfo(file).Name.StartsWith($"#{ticketnumber}.tcapp"))
                {
                    XmlSerializer xml = new XmlSerializer(typeof(AppFile));
                    var fs = new FileStream(file, FileMode.Open);
                    AppFile af = (AppFile)xml.Deserialize(fs);
                    if (af == null) return null;
                    time = af.time;
                    date = af.date;
                    id = af.id;
                    ticketNumber = af.ticketNumber;
                    ticketStatus = af.ticketStatus;
                    username = af.username;
                    age = af.age;
                    role = af.role;
                    about = af.about;
                    experiance = af.experiance;
                    reason = af.reason;
                    fs.Close();
                    return this;
                }
            }
            return null;           
        }
        
        public Embed ToString()
        {
            EmbedBuilder builder = new EmbedBuilder();
            var footer = new EmbedFooterBuilder();
            footer.IconUrl = Settings.Default.StaffApplicationIcon;
            builder.Footer = footer;
            switch(ticketStatus)
            {
                case TicketStatus.Open: builder.Color = Color.Gold; break;
                case TicketStatus.Accepted: builder.Color = Color.Green; break;
                case TicketStatus.Rejected: builder.Color = Color.Red; break;
            }
            builder.Title = $"STAFF APPLICATION | {time} {date}";
            builder.Description = $"Ticket Number: {ticketNumber}\n" +
                $"Ticket Status: {ticketStatus}";
            builder.AddField("User/Age", $"{GetUser.Mention} : {age}");
            builder.AddField("Role", role);
            builder.AddField("About", about);
            builder.AddField("Experiance", experiance, true);
            builder.AddField("Reason", reason, true);
            return builder.Build();
        }
    }
}