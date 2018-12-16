using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MCBot.Properties;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MCBot
{
    public class TCProgram
    {
        public static readonly string path = $"{AppDomain.CurrentDomain.BaseDirectory}StaffApplications\\";

        static void Main(string[] args) => new TCProgram().RunAsync().GetAwaiter().GetResult();

        public static DiscordSocketClient client;
        public static CommandService service;
        public static IServiceProvider provider;

        private async Task RunAsync()
        {
            Build();

            client = new DiscordSocketClient();
            service = new CommandService();
            provider = new ServiceCollection()
            .AddSingleton(client)
            .AddSingleton(service)
            .BuildServiceProvider();
            var botToken = "NTIyODY4NjIxMzI5MDM5MzY5.DvWqBw.fndExPvur2hmq37YLQkUmTyKEQY";

            // Event Subscription
            client.Log += TCEvents.Log;
            client.UserJoined += TCEvents.AnnounceUserJoinedAsync;
            client.MessageReceived += TCEvents.HandleCommandAsync;

            try
            {
                await service.AddModulesAsync(Assembly.GetEntryAssembly());
            }
            catch (Exception ex) { Console.WriteLine(ex); } 
            await client.LoginAsync(TokenType.Bot, botToken);
            await client.StartAsync();
            await Task.Delay(-1);
        }

        public static void Build()
        {
            Directory.CreateDirectory(path);
            foreach (var file in Directory.GetFiles(path))
            {
                var token = new FileInfo(file).Name.Replace("#", string.Empty).Replace(".tcapp", string.Empty);

                try
                {
                    XDocument xd1 = new XDocument();
                    xd1 = XDocument.Load(file);
                }
                catch 
                {
                    var app = new AppFile().Process(token);
                    if (app != null) app.Save(token);
                    else File.Delete(file);
                }

            }
        }
    }

    public static class TCEvents
    {
        public static async Task AnnounceUserJoinedAsync(SocketGuildUser user)
        {
            var guild = user.Guild;
            var channel = guild.DefaultChannel;
            await channel.SendMessageAsync($"Welcome, {user.Mention}");

        }
        public static async Task HandleCommandAsync(SocketMessage arg)
        {
            var msg = (SocketUserMessage)arg;
            if (msg == null || msg.Author.IsBot) return;

            int argPos = 0;
            if (msg.HasCharPrefix('!', ref argPos) /*|| msg.HasMentionPrefix(TCProgram.client.CurrentUser, ref argPos) */ )
            {
                var contex = new SocketCommandContext(TCProgram.client, msg);

                var result = await TCProgram.service.ExecuteAsync(contex, argPos, TCProgram.provider);
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    await contex.Channel.SendMessageAsync(result.ErrorReason);
                    Console.WriteLine($"{contex.User.Username} snet an invalid command, {result.ErrorReason}.");
                }

            }
        }
        public static async Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            await Task.CompletedTask;
        }
    }

    [Serializable] public class AppFile
    {
        public enum TicketStatus { Open, Accepted, Rejected, Revoked, }
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
            var lines = File.ReadLines(TCProgram.path + $"#{ticketnumber}.tcapp").ToList();
            if (lines.Count != 11) return null;
            date = lines[1].Split(new string[] { ": " }, StringSplitOptions.None)[1];
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
            //foreach (var file in Directory.GetFiles(Staff.path))
            //{
            //    AppFile af = new AppFile().Load(new FileInfo(file).Name.Replace("#", string.Empty).Replace(new FileInfo(file).Extension, string.Empty));
            //    if (af.username == username && af.ticketStatus == TicketStatus.Open)
            //    {
            //        GetUser.SendMessageAsync("You already have an application in processing at the moment please wat for it to finish then try again.");
            //        if (File.Exists(Staff.path + $"#{ticketnumber}.tcapp")) File.Delete(Staff.path + $"#{ticketnumber}.tcapp");
            //        return;
            //    }
            //}

            XmlSerializer xml = new XmlSerializer(typeof(AppFile));
            if (File.Exists(TCProgram.path + $"#{ticketnumber}.tcapp")) File.Delete(TCProgram.path + $"#{ticketnumber}.tcapp");
            var fs = new FileStream(TCProgram.path + $"#{ticketnumber}.tcapp", FileMode.Create);
            xml.Serialize(fs, this);
            fs.Close();
        }

        public AppFile Load(string ticketnumber)
        {
            foreach (var file in Directory.GetFiles(TCProgram.path))
            {
                if (new FileInfo(file).Name.StartsWith($"#{ticketnumber}.tcapp"))
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
            switch (ticketStatus)
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

    #region Modules
    [RequireUserPermission(GuildPermission.Administrator)]
    public class AdminModules : ModuleBase<SocketCommandContext>
    {
        [Command("?!")]
        public async Task ShowAllCommands()
        {
            var embed = new EmbedBuilder();
            var footer = new EmbedFooterBuilder();

            footer.Text = Settings.Default.StaffApplicationFooter;
            footer.IconUrl = Settings.Default.StaffApplicationIcon;

            embed.Footer = footer;
            embed.Color = Color.LighterGrey;
            embed.Title = "*** Commands ***";

            embed.AddField("TEACRAFT", "*** STAFF ***");
            embed.AddField("!staffapply", "This is use to declare that you want to apply to a role it must be at " +
                "the top of your application. for the template do !staffapply");
            embed.AddField("!staffappreview (Admin Only)", "This is use to declare that you want view all applications.");
            embed.AddField("!staffappreview {Ticket Number} (Admin Only)", "This is use to declare that you want view the application for the given ticket number.");
            embed.AddField("!staffappaccept {Ticket Number} (Admin Only)", "This is use to declare that you want to accept the application for the given ticket number.");
            embed.AddField("!staffapprevoke {Ticket Number} (Admin Only)", "This is use to declare that you want to revoke role defined in the application for the given user.");
            embed.AddField("!staffappdeny {Ticket Number} (Admin Only)", "This is use to declare that you want to deny the application for the given ticket number.");
            embed.AddField("!staffappdeny {Ticket Number} {reason} (Admin Only)", "This is use to declare that you want to deny the application for the given ticket number. " +
                "and you can reply to the user via DM telling them why you could not accept the application.");
            embed.AddField("!staffappclear (Admin Only)", "This is use to declare that you want to clear all applications.");
            embed.AddField("TEACRAFT", "*** SETTINGS ***");
            embed.AddField("!settings (Admin Only)", "This is use to declare that you want view all settings.");
            embed.AddField("!settings {name} {value} (Admin Only)", "This is use to declare that you want change the give setting with the new value.");
            await ReplyAsync("", false, embed.Build());
        }
        [Command("staffappreview")]
        public async Task ReviewApplication([Remainder] string ticketnumber = default(string))
        {
            var unserializefiles = new List<string>();
            foreach (string file in Directory.GetFiles(TCProgram.path))
            {
                var f = file;
                var af = new AppFile();
                try
                {
                }
                catch
                {

                    var afile = new AppFile().Process(new FileInfo(f).Name.Replace("#", string.Empty).Replace(".tcapp", string.Empty));
                    afile.id = Context.User.Id;
                    afile.Save(ticketnumber);
                    af.Load(f);
                }
            }


            Directory.CreateDirectory(TCProgram.path);
            if (!string.IsNullOrEmpty(ticketnumber))
            {
                await ReplyAsync("", false, new AppFile().Load(ticketnumber).ToString());
            }
            else
            {
                var files = Directory.GetFiles(TCProgram.path);
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
        [Command("staffappaccept")]
        public async Task AcceptApplicationAsync(string ticketnumber)
        {
            var file = new AppFile().Load(ticketnumber);
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == file.role.ToString());
            var guilduser = TCProgram.client.GetGuild(Settings.Default.TCGuild).GetUser(file.id);
            if (!guilduser.Roles.Contains(role))
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
            else
            {
                EmbedBuilder embed = new EmbedBuilder();
                embed.Color = Color.Green;
                embed.Title = "***Thank you***";
                embed.Description = "Role Already assigned.";
                embed.AddField("MessageFrom the Staff",
                $"Okay so it seam like you alrady have the { file.role } role and so we will keep that on file " +
                $"no need to worry, if you have any question you can ask them in the { TCProgram.client.GetChannel(Settings.Default.StaffChannelID) }.");

                await file.GetUser.SendMessageAsync("", false, embed.Build());
                file.ticketStatus = AppFile.TicketStatus.Accepted;
            }
            file.Save(file.ticketNumber);
        }
        [Command("staffapprevoke")]
        public async Task RevokeApplicationAsync(string ticketnumber)
        {
            var file = new AppFile().Load(ticketnumber);
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name == file.role.ToString());
            var guilduser = TCProgram.client.GetGuild(Settings.Default.TCGuild).GetUser(file.id);
            if (guilduser.Roles.Contains(role))
            {
                await guilduser.RemoveRoleAsync(role);
                EmbedBuilder embed = new EmbedBuilder();
                embed.Color = Color.DarkRed;
                embed.Title = "***SORRY***";
                embed.Description = "You role has been revoked.";
                embed.AddField("MessageFrom the Staff",
                $"Sorry  { file.GetUser.Mention } your role {file.role}  has been taken away." +
                $"will take effect immodestly if you have any question you can ask one of the admins genaral chat.");

                await file.GetUser.SendMessageAsync("", false, embed.Build());
                file.ticketStatus = AppFile.TicketStatus.Revoked;
            }
            else
            {
                EmbedBuilder embed = new EmbedBuilder();
                embed.Color = Color.DarkRed;
                embed.Title = "***SORRY***";
                embed.Description = "Role has been revoked.";
                embed.AddField("MessageFrom the Staff",
                $"Sorry but that user { file.GetUser.Mention } doesn't have {file.role} role.");

                await Context.User.SendMessageAsync("", false, embed.Build());
                file.ticketStatus = AppFile.TicketStatus.Revoked;
            }
            file.Save(file.ticketNumber);
        }
        [Command("staffappdeny")]
        public async Task DenyApplicationAsync(string ticketnumber, [Remainder]string reason = default(string))
        {
            var file = new AppFile().Load(ticketnumber);
            file.ticketStatus = AppFile.TicketStatus.Rejected;
            EmbedBuilder embed = new EmbedBuilder();
            embed.Color = Color.DarkRed;
            embed.Title = "***SORRY***";
            embed.Description = "You have been not been approved for the for a role see details below.";
            if (!string.IsNullOrEmpty(reason)) embed.AddField("MessageFrom the Staff", reason);
            await file.GetUser.SendMessageAsync("", false, embed.Build());
            file.Save(ticketnumber);
        }
        [Command("staffappclear")]
        public async Task ClearApplicationsAsync()
        {
            int numfiles = 0;
            Directory.CreateDirectory(TCProgram.path);
            if (Directory.Exists(TCProgram.path))
            {
                numfiles = Directory.GetFiles(TCProgram.path).Count();
                Directory.Delete(TCProgram.path, true);
                Directory.CreateDirectory(TCProgram.path);
                await ReplyAsync($"{numfiles} Applications Cleared");
            }
        }

        [Command("settings"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task ViewSettings()
        {
            var embed = new EmbedBuilder();
            embed.Title = $"{Settings.Default.StaffApplicationTitle} Settings";
            embed.Color = Color.Teal;
            var footer = new EmbedFooterBuilder();
            footer.IconUrl = Settings.Default.StaffApplicationIcon;
            footer.Text = Settings.Default.StaffApplicationFooter;
            embed.Footer = footer;
            embed.AddField("** GENERAL **", "Settings");
            embed.AddField("MC Server IP", Settings.Default.ip);
            embed.AddField("MC Server URL", Settings.Default.url);
            embed.AddField("MC Server Port", Settings.Default.port);
            embed.AddField("MC Server URL/IP", Settings.Default.useurl ? "URL Mode" : "IP Mode");
            embed.AddField("** STAFF **", "Settings");
            embed.AddField("Staff Application Title", Settings.Default.StaffApplicationTitle);
            embed.AddField("Staff Application Desc", Settings.Default.StaffApplicationDescription);
            embed.AddField("Staff Application Footer", Settings.Default.StaffApplicationFooter);
            embed.AddField("Staff Application Icon URL", Settings.Default.StaffApplicationIcon);
            embed.AddField("Staff Guild ID", $"{TCProgram.client.GetGuild(Settings.Default.TCGuild).Name}");
            embed.AddField("Staff Channel ID", $"{Context.Guild.GetChannel(Settings.Default.StaffChannelID).ToString()}");
            await ReplyAsync("", false, embed.Build());
        }
        [Command("settingschange"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task ChangeSettings(string name, [Remainder] string value)
        {
            var builder = new EmbedBuilder();
            builder.Title = "TeaCraf Settings";
            builder.Color = Color.Teal;

            try
            {
                EmbedFooterBuilder footer = new EmbedFooterBuilder();
                footer.Text = Settings.Default.StaffApplicationFooter;
                footer.IconUrl = Settings.Default.StaffApplicationIcon;
                builder.Footer = footer;
            }
            catch { }
            switch (name)
            {
                case "mcip":
                    try
                    {
                        Settings.Default.ip = value;
                        Settings.Default.Save();
                        builder.AddField($"Application Setting `MC Server IP` has been changed",
                       "All settings has been saveed.");
                    }
                    catch
                    {
                        builder.Color = Color.Red;
                        builder.AddField($"Application Setting `MC Server IP` could not be changed",
                      "Ite seam the the type you entered does not match the type of the field");
                    }
                    await ReplyAsync("", false, builder.Build());
                    break;
                case "mcurl":
                    try
                    {
                        Settings.Default.url = value;
                        Settings.Default.Save();
                        builder.AddField($"Application Setting `MC Server URL` has been changed",
                       "All settings has been saveed.");
                    }
                    catch
                    {
                        builder.Color = Color.Red;
                        builder.AddField($"Application Setting `MC Server URL` could not be changed",
                      "Ite seam the the type you entered does not match the type of the field");
                    }
                    await ReplyAsync("", false, builder.Build());
                    break;
                case "mcport":
                    try
                    {
                        Settings.Default.port = int.Parse(value);
                        Settings.Default.Save();
                        builder.AddField($"Application Setting `MC Server Port` has been changed",
                       "All settings has been saveed.");
                    }
                    catch
                    {
                        builder.Color = Color.Red;
                        builder.AddField($"Application Setting `MC Server Port` could not be changed",
                      "Ite seam the the type you entered does not match the type of the field");
                    }
                    await ReplyAsync("", false, builder.Build());
                    break;
                case "mcuseurl":
                    try
                    {
                        Settings.Default.useurl = bool.Parse(value);
                        Settings.Default.Save();
                        builder.AddField($"Application Setting `MC Server URL/IP` has been changed",
                       "All settings has been saveed.");
                    }
                    catch
                    {
                        builder.Color = Color.Red;
                        builder.AddField($"Application Setting `MC Server URL/IP` could not be changed",
                      "Ite seam the the type you entered does not match the type of the field");
                    }
                    await ReplyAsync("", false, builder.Build());
                    break;
                case "sadesc":
                    try
                    {
                        Settings.Default.StaffApplicationDescription = value;
                        Settings.Default.Save();
                        builder.AddField($"Application Setting `Staff Application Description` has been changed",
                       "All settings has been saveed.");
                    }
                    catch
                    {
                        builder.Color = Color.Red;
                        builder.AddField($"Application Setting `Staff Application Description` could not be changed",
                      "Ite seam the the type you entered does not match the type of the field");
                    }
                    await ReplyAsync("", false, builder.Build());
                    break;
                case "safoot":
                    try
                    {
                        Settings.Default.StaffApplicationFooter = value;
                        Settings.Default.Save();
                        builder.AddField($"Application Setting `Staff Application Footer` has been changed",
                       "All settings has been saveed.");
                    }
                    catch
                    {
                        builder.Color = Color.Red;
                        builder.AddField($"Application Setting `Staff Application Footer` could not be changed",
                      "Ite seam the the type you entered does not match the type of the field");
                    }
                    await ReplyAsync("", false, builder.Build());
                    break;
                case "saiurl":
                    try
                    {
                        Settings.Default.StaffApplicationIcon = value;
                        Settings.Default.Save();
                        builder.AddField($"Application Setting `Staff Application Icon URL` has been changed",
                       "All settings has been saveed.");
                    }
                    catch
                    {
                        builder.Color = Color.Red;
                        builder.AddField($"Application Setting `Staff Application Icon URL` could not be changed",
                      "Ite seam the the type you entered does not match the type of the field");
                    }
                    await ReplyAsync("", false, builder.Build());
                    break;
                case "satitle":
                    try
                    {
                        Settings.Default.StaffApplicationTitle = value;
                        Settings.Default.Save();
                        builder.AddField($"Application Setting `Staff Application Title` has been changed",
                       "All settings has been saveed.");
                    }
                    catch
                    {
                        builder.Color = Color.Red;
                        builder.AddField($"Application Setting `Staff Application Title` could not be changed",
                      "Ite seam the the type you entered does not match the type of the field");
                    }
                    await ReplyAsync("", false, builder.Build());
                    break;
                case "sacid":
                    try
                    {
                        Settings.Default.StaffChannelID = ulong.Parse(value);
                        Settings.Default.Save();
                        builder.AddField($"Application Setting `Staff Application Channel ID` has been changed",
                       "All settings has been saveed.");
                    }
                    catch
                    {
                        builder.Color = Color.Red;
                        builder.AddField($"Application Setting `Staff Application Channel ID` could not be changed",
                        "Ite seam the the type you entered does not match the type of the field");

                    }
                    await ReplyAsync("", false, builder.Build());
                    break;
                case "gid":
                    try
                    {
                        Settings.Default.TCGuild = ulong.Parse(value);
                        Settings.Default.Save();
                        builder.AddField($"Application Setting `Guild ID` has been changed",
                       "All settings has been saveed.");
                    }
                    catch
                    {
                        builder.Color = Color.Red;
                        builder.AddField($"Application Setting `Guild ID` could not be changed",
                      "Ite seam the the type you entered does not match the type of the field");
                    }
                    await ReplyAsync("", false, builder.Build());
                    break;
                default:
                    builder.Color = Color.Red;
                    builder.AddField($"Application Setting `{name}` could not be found",
                        "please make sure that you are applying the right type and the settings name is correct.");
                    await ReplyAsync("", false, builder.Build());
                    return;
            }
        }
    }
    public class UserModules : ModuleBase<SocketCommandContext>
    {
        [Command("?")]
        public async Task ShowAllCommands()
        {
            var embed = new EmbedBuilder();
            var footer = new EmbedFooterBuilder();

            footer.Text = Settings.Default.StaffApplicationFooter;
            footer.IconUrl = Settings.Default.StaffApplicationIcon;

            embed.Footer = footer;
            embed.Color = Color.LighterGrey;
            embed.Title = "*** Commands ***";

            embed.AddField("TEACRAFT", "*** STAFF ***");
            embed.AddField("!staffapply", "This is use to declare that you want to apply to a role it must be at " +
                "the top of your application. for the template do !staffapply");
            await ReplyAsync("", false, embed.Build());
        }
        [Command("staffapply")]
        public async Task ApplyForStaffAsync([Remainder] string app = default(string))
        {
            Directory.CreateDirectory(TCProgram.path);
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
                    $"!staffapply\n" +
                    $"Name: {demoname}\n" +
                    $"Age: {demoage}\n" +
                    $"Role: {demorole}\n" +
                    $"About: {demoabout}\n" +
                    $"Experiance: {demoexperiance}\n" +
                    $"Reason: {demoreason}\n" +
                    $"```\n" +
                    $"***Note***\n" +
                    $"MAKE SURE YOU DO !staffapp on the first line ALSO EVERYTHING MUST BE ON IT'S OWN LINE\n" +
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

                if (!Directory.Exists(TCProgram.path)) Directory.CreateDirectory(TCProgram.path);
                StreamWriter sw = new StreamWriter(TCProgram.path + $"#{ticketnumber}.tcapp", false);
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
    }
    #endregion
}
