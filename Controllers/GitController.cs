using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using TCAdmin.GameHosting.SDK.Objects;
using TCAdmin.Interfaces.Logging;
using TCAdmin.SDK.Web.MVC.Controllers;
using TCAdmin.Web.MVC;
using TCAdminGitClone.HttpResponses;

namespace TCAdminGitClone.Controllers
{
    public class GitController : BaseServiceController
    {
        [HttpPost]
        [ParentAction("Service", "Home")]
        public ActionResult Clone(int id, string target, string gitUrl, bool extract)
        {
            Console.WriteLine(1);
            this.EnforceFeaturePermission("FileManager");
            var uriValid = Uri.TryCreate(gitUrl, UriKind.Absolute, out var gitUri) && gitUri != null &&
                           (gitUri.Scheme == Uri.UriSchemeHttp ||
                            gitUri.Scheme == Uri.UriSchemeHttps);
            if (!uriValid)
            {
                return new JsonHttpStatusResult(new
                {
                    responseText = "Invalid URL provided."
                }, HttpStatusCode.BadRequest);
            }
            Console.WriteLine(2);

            var repoName = gitUri.Segments.LastOrDefault() + ".zip";
            var service = Service.GetSelectedService();
            var game = TCAdmin.GameHosting.SDK.Objects.Game.GetSelectedGame();
            var server = TCAdmin.SDK.Objects.Server.GetSelectedServer();
            var fileSystem = server.FileSystemService;
            var gitCloneUrl = GetGitDownloadUrl(gitUrl);
            var saveTo = "";
            if (!string.IsNullOrEmpty(game.Paths.RelativeUserFiles) && TCAdmin.SDK.Web.Session.GetCurrentUser().UserType == TCAdmin.SDK.Objects.UserType.User)
            {
                saveTo = TCAdmin.SDK.Misc.FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory, game.Paths.RelativeUserFiles, target, repoName).TrimEnd('/', '\\'); ;
            }
            else
            {
                saveTo = TCAdmin.SDK.Misc.FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory, target, repoName).TrimEnd('/', '\\'); ;
            }
            Console.WriteLine(3);
            Console.WriteLine($"SaveTo: {saveTo}");
            fileSystem.DownloadFile(saveTo, gitCloneUrl);
            Console.WriteLine(4);

            if (extract)
            {
                try
                {
                    var serializedDirectorySecurity =
                        TCAdmin.SDK.Misc.ObjectXml.ObjectToXml(service.GetDirectorySecurityForCurrentUser());
                    fileSystem.Extract(saveTo, Path.GetDirectoryName(saveTo), serializedDirectorySecurity);
                }
                catch (Exception e)
                {
                    return new JsonHttpStatusResult(new
                    {
                        Message = "Error when extracting: " + e.Message
                    }, HttpStatusCode.InternalServerError);
                }
            }

            return new JsonHttpStatusResult(new
            {
                Message = $"Successfully cloned <strong>{gitUrl}</strong>"
            }, HttpStatusCode.OK);
        }

        private static string GetGitDownloadUrl(string gitUrl)
        {
            if (gitUrl.EndsWith(".zip") || gitUrl.EndsWith(".pk3") || gitUrl.EndsWith(".bsp"))
            {
                return gitUrl;
            }

            gitUrl = gitUrl.Replace(".git", "");
            gitUrl += "/archive/master.zip";
            return gitUrl;
        }
    }
}